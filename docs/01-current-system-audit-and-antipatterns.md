# Document 01: Current System Audit & Anti-Patterns Analysis
## Deep-Dive Technical Audit of the Legacy VMM Dashboard & Backend Architecture

> **Purpose:** Detailed diagnostic of architectural bottlenecks, anti-patterns, and vulnerability points in the existing codebase that cause regressions, broken sorting, and maintenance fatigue.

---

## 1. Database Tier: Stored Procedure Bottlenecks

### 1.1 The "God Stored Procedure" Anti-Pattern (`SP_NEW_REPORT`)
The primary database procedure powering reports (`SP_NEW_REPORT`) exceeds **4,500 lines of T-SQL** and acts as an enormous router via an input parameter:
```sql
IF @Status = 'LAST7DAY_VOID_DASHBOARD'
BEGIN
    ...
END
ELSE IF @Status = 'LAST7DAY_RETURN_DASHBOARD'
BEGIN
    ...
END
ELSE IF @Status = 'SHOW_DATA_FOR_STORE_ENCODING'
BEGIN
    ...
END
-- 25+ more ELSE IF branches
```

#### Why this causes critical issues:
1. **Plan Cache Invalidation & Parameter Sniffing:** SQL Server compiles an execution plan based on the first `@Status` and parameters passed. When another user requests a completely different report using the same procedure, SQL Server reuses an inappropriate execution plan, leading to massive CPU spikes and timeouts.
2. **Merge Conflicts & Blast Radius:** If Developer A fixes a column in `CycleCountReport`, they must edit the same 4,500-line file where Developer B is modifying `StoreGrcReport`. A typo in line 1,200 can break all 25 reports across the entire company.
3. **Dead / Orphaned Code:** Our audit identified large chunks of historical commented-out code (e.g., lines 2760–2940 contain a legacy `CYCLE_COUNT_REPORT` branch that was later reimplemented at line 2958), confusing developers and making maintenance perilous.

---

### 1.2 Fragile Dynamic Sorting via String Matching
In SQL Server, dynamic sorting is implemented via `CASE @SortColumn`:
```sql
ORDER BY 
    CASE WHEN @SortDirection = 'ASC' THEN 
        CASE @SortColumn
            WHEN 'VOID QTY' THEN R.VOID_QTY
            WHEN 'ENCODED VS VOID (QTY)' THEN R.ENCODED_QTY
            WHEN 'Store_Code' THEN R.STORE_CODE
            WHEN 'ARTICLE_DESC' THEN R.DESCRIPTION
            ELSE R.DATE
        END
    END ASC
```

#### The Vulnerabilities Uncovered During Audit:
- **Inconsistent Phrasing & Spaces:** Notice that some columns expect exact spaces (`'VOID QTY'`), some expect camel/pascal casing (`'Store_Code'`), and some expect abbreviations (`'ARTICLE_DESC'`).
- **Silent Fallbacks to `ELSE`:** If the frontend passes `'VOID_QTY'` (with an underscore instead of a space), SQL does **not** throw an error. It silently hits `ELSE R.DATE`. To the end user and developer, clicking the column appears broken or unresponsive because the table sorts by Date instead of Void Quantity.
- **Dual-Purpose Modals:** In `ReconciliationDetailsModal.jsx`, both Void and Return reconciliations share the same UI, but SQL Server expects `'VOID_DATE'` for voids and `'BILL_DATE'` for returns. Without dynamic runtime key resolution, one of the two modes always fails silently.

---

## 2. Backend Tier (.NET 8): Architectural Gaps

### 2.1 "Pass-Through" Controllers with Loose Strings
In `VS Mart Backend`, controllers and services frequently pass raw, unvalidated strings directly from HTTP query parameters into database parameters:

```csharp
// Example from VoidDashboardService.cs
public async Task<VoidDetailsResponse> GetVoidDetailsAsync(VoidDetailsRequest request)
{
    // ...
    parameters.Add("@status", "LAST7DAY_VOID_DASHBOARD", DbType.String, size: 50);
    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn, DbType.String, size: 50);
    parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection, DbType.String, size: 10);
    
    var items = await connection.QueryAsync<dynamic>("SP_NEW_DASHBOARD", parameters, commandType: CommandType.StoredProcedure);
    // ...
}
```

#### Why this causes issues:
- **No Compile-Time Validation:** If the frontend makes a typo (`sortColumn=VOID_QTY`), neither .NET nor the IDE flags it. The request receives a `200 OK` status with data sorted by the wrong column.
- **Dynamic Typing (`QueryAsync<dynamic>`):** Returning dynamic dictionaries strips away all C# type safety. Column renames in SQL return null or empty values without any compiler warning.
- **Swallowed Exceptions:** In several services, broad try-catch blocks silently catch `Exception` and return an empty `new VoidDetailsResponse()`, hiding database connectivity errors, SQL syntax exceptions, and parameter mismatches from logs.

---

### 2.2 Fragmented Architecture: The Dual-Registry Problem
In `VS Mart Backend`, the team recognized the complexity of managing report parameters and created:
- `VS_Mart_Backend.Features.Dashboard.Export.ReportRegistry.cs` (627 lines)

This registry centralizes parameters **only for Excel/CSV streaming exports**. However, the standard data retrieval endpoints (`VoidDashboardService`, `StoreGrcReportService`, `SaleDashboardService`) do **not** use this registry. They duplicate the stored procedure calls, parameter bindings, and default sort values independently, leading to discrepancies between what users see in the grid and what gets exported.

---

## 3. Frontend Tier (React 19): Architectural Gaps

### 3.1 Massive State Duplication Across 16+ Pages
Every report page (`StoreGrcReportPage.jsx`, `DcReportPage.jsx`, `VoidDetailsReportPage.jsx`, `AllocatedStoreReportPage.jsx`, etc.) independently declares and manages 8–12 identical pieces of state:

```javascript
// Duplicated across 16 different files
const [pageIndex, setPageIndex] = useState(1);
const [pageSize, setPageSize] = useState(10);
const [totalRecords, setTotalRecords] = useState(0);
const [sortColumn, setSortColumn] = useState('DATE');
const [sortDirection, setSortDirection] = useState('DESC');
const [searchTerm, setSearchTerm] = useState('');
const [fromDate, setFromDate] = useState(defaultFromDate);
const [toDate, setToDate] = useState(defaultToDate);
const [isExportOpen, setIsExportOpen] = useState(false);
```

#### Why this causes issues:
- **High Maintenance Overhead:** When a bug is fixed in pagination or sorting logic in one page, the same edit must be manually applied across 15 other files.
- **Inconsistent User Experience:** Some pages reset `pageIndex` to 1 on sort change; others forget to reset it, causing users on page 10 to see empty tables after sorting.
- **Inconsistent Props:** Some pages pass `onSortChange`, while others like `AllocatedStoreReportPage.jsx` originally lacked `onSortChange` wiring entirely, rendering sort arrows completely inoperative.

---

### 3.2 Session Storage & Auth Hacks
In `src/services/stockService.js` (lines 12–37), session parsing requires defensive hacks to detect the current user ID:
```javascript
export const getActiveUserId = () => {
  const raw = sessionStorage.getItem('vmm_user');
  if (raw) {
    const user = JSON.parse(raw);
    // Checking 8 different casing variations!
    const direct = user.userID ?? user.userId ?? user.UserID ?? user.User_Id ?? user.USER_ID ?? user.user_id ?? user.id ?? user.Id;
    if (direct !== undefined) return direct;
    for (const key of Object.keys(user)) {
      if (key.toLowerCase() === 'userid') return user[key];
    }
  }
  return 1; // Unsafe fallback to User 1
};
```
This is a direct consequence of not having a unified authentication token (JWT) or an HTTP Request Interceptor, forcing individual API calls to manually format and concatenate query strings.

---

### 3.3 Disconnected URL State (Loss of Context on Refresh)
- In the existing architecture, when a user clicks a card on the Dashboard (e.g., clicking the Void card for store `HD44`), navigation occurs via React Router state: `navigate('/reports/void-details', { state: { storeCode: 'HD44' } })`.
- If the user refreshes the page, sends the link to a coworker, or opens the report in a new tab, the `state` is wiped. The report falls back to default filters or fails to load data.

---

## 4. Summary Matrix of Existing Anti-Patterns

| Layer | Existing Anti-Pattern | Immediate Symptom | Root Cause |
| :--- | :--- | :--- | :--- |
| **Database** | 4,500-line monolithic SP (`SP_NEW_REPORT`) | SQL timeouts, parameter sniffing, risky deployments | All reports bundled into a single procedural switch |
| **Database** | Dynamic SQL `CASE @SortColumn` | Sorting clicks silently fail or sort by fallback column | Inconsistent casing, spaces in column names, silent `ELSE` |
| **Backend** | Loose string query parameters (`string? sortColumn`) | Broken sort keys not caught at compile time | Lack of strongly-typed Request DTOs and Enums |
| **Backend** | Dynamic query results (`connection.QueryAsync<dynamic>`) | Schema changes in DB break silently without build errors | Lack of strongly-typed Response Models |
| **Frontend** | 16 copies of table state and handlers | Maintenance nightmare; inconsistent behavior across reports | Absence of a universal headless table hook (`useServerTable`) |
| **Frontend** | Component-level `useState` instead of URL params | Refreshing browser or bookmarking loses active filters | State stored in memory rather than browser URL searchParams |
| **Testing** | 100% manual testing of 100+ columns | Developer anxiety, high regression risk, slow release cycles | Lack of automated E2E test suites (Playwright) |
