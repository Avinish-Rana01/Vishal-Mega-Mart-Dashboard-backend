# Document 02: Database Redesign Guide
## Rebuilding the SQL Server Data Layer from Scratch

> **Purpose:** Transform monolithic, fragile stored procedures into modular, high-performance, strictly typed data services with zero-downtime execution and rock-solid sorting.

---

## 1. Core Architectural Principles for the Data Layer

When rebuilding the VMM retail data layer from scratch, adhere to these five non-negotiable rules:

1. **One Report, One Procedure:** Never combine multiple distinct reports into a single monolithic stored procedure. Each report gets a dedicated, single-purpose stored procedure (e.g., `usp_Report_VoidDetails`, `usp_Report_StoreGrcSummary`).
2. **Zero Spaces in Column Identifiers:** Never use spaces or special characters in column names (e.g., `[VOID QTY]` or `[ENCODED VS VOID (QTY)]`). Use standard `snake_case` (`void_qty`, `encoded_qty`) or `PascalCase` (`VoidQty`, `EncodedQty`).
3. **Fail-Fast Dynamic Sorting:** Dynamic sorting must validate against a strict whitelist. If an unhandled column is passed, return a descriptive error or clear fallback rather than silently masking the defect.
4. **Modern ANSI Paging (`OFFSET / FETCH NEXT`):** Replace legacy `ROW_NUMBER() OVER (...)` subqueries with native SQL Server `OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY` for better plan caching and reduced I/O.
5. **Covering Indexes for Date-Range Queries:** Create composite indexes aligned with retail query patterns: `(store_code, transaction_date) INCLUDE (...)`.

---

## 2. Standardized Stored Procedure Template

Below is the production-grade reference template for any report stored procedure:

```sql
CREATE OR ALTER PROCEDURE dbo.usp_Report_VoidDetails
    @StoreCode      VARCHAR(50),
    @FromDate       DATE,
    @ToDate         DATE,
    @SearchTerm     NVARCHAR(100) = NULL,
    @SortColumn     VARCHAR(50)   = 'transaction_date',
    @SortDirection  VARCHAR(4)    = 'DESC',
    @PageIndex      INT           = 1,
    @PageSize       INT           = 10,
    @TotalCount     INT OUTPUT,
    @TotalVoidQty   INT OUTPUT,
    @TotalEncodeQty INT OUTPUT,
    @TotalDiffQty   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; -- Prevent blocking on analytical reads

    -- 1. Parameter Normalization
    SET @PageIndex = ISNULL(@PageIndex, 1);
    SET @PageSize  = ISNULL(@PageSize, 10);
    IF @PageIndex < 1 SET @PageIndex = 1;
    IF @PageSize < 1 OR @PageSize > 500 SET @PageSize = 10;
    
    SET @SortDirection = UPPER(ISNULL(@SortDirection, 'DESC'));
    IF @SortDirection NOT IN ('ASC', 'DESC') SET @SortDirection = 'DESC';

    -- 2. Sort Column Whitelisting (Fail-Safe Validation)
    SET @SortColumn = LOWER(ISNULL(NULLIF(LTRIM(RTRIM(@SortColumn)), ''), 'transaction_date'));
    
    IF @SortColumn NOT IN ('transaction_date', 'void_qty', 'encoded_qty', 'pending_qty', 'store_code', 'store_name')
    BEGIN
        -- Default to transaction_date if column is not recognized
        SET @SortColumn = 'transaction_date';
    END

    DECLARE @Offset INT = (@PageIndex - 1) * @PageSize;

    -- 3. Calculate Aggregates / Totals in a Single Fast Scan
    SELECT 
        @TotalCount     = COUNT(1),
        @TotalVoidQty   = ISNULL(SUM(v.void_qty), 0),
        @TotalEncodeQty = ISNULL(SUM(v.encoded_qty), 0),
        @TotalDiffQty   = ISNULL(SUM(v.void_qty - v.encoded_qty), 0)
    FROM dbo.tbl_Void_Transactions v WITH (NOLOCK)
    INNER JOIN dbo.tbl_Stores s WITH (NOLOCK) ON s.store_code = v.store_code
    WHERE (@StoreCode = '' OR v.store_code = @StoreCode)
      AND v.transaction_date >= @FromDate 
      AND v.transaction_date <= @ToDate
      AND (@SearchTerm IS NULL OR v.store_code LIKE '%' + @SearchTerm + '%' OR s.store_name LIKE '%' + @SearchTerm + '%');

    -- 4. Paginated Data Retrieval using Modern ANSI SQL
    SELECT 
        v.transaction_date     AS TransactionDate,
        v.store_code           AS StoreCode,
        s.store_name           AS StoreName,
        v.void_qty             AS VoidQty,
        v.encoded_qty          AS EncodedQty,
        (v.void_qty - v.encoded_qty) AS PendingQty
    FROM dbo.tbl_Void_Transactions v WITH (NOLOCK)
    INNER JOIN dbo.tbl_Stores s WITH (NOLOCK) ON s.store_code = v.store_code
    WHERE (@StoreCode = '' OR v.store_code = @StoreCode)
      AND v.transaction_date >= @FromDate 
      AND v.transaction_date <= @ToDate
      AND (@SearchTerm IS NULL OR v.store_code LIKE '%' + @SearchTerm + '%' OR s.store_name LIKE '%' + @SearchTerm + '%')
    ORDER BY 
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'transaction_date' THEN v.transaction_date END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'transaction_date' THEN v.transaction_date END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'void_qty'         THEN v.void_qty END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'void_qty'         THEN v.void_qty END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'encoded_qty'      THEN v.encoded_qty END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'encoded_qty'      THEN v.encoded_qty END DESC,
        CASE WHEN @SortDirection = 'ASC'  AND @SortColumn = 'pending_qty'      THEN (v.void_qty - v.encoded_qty) END ASC,
        CASE WHEN @SortDirection = 'DESC' AND @SortColumn = 'pending_qty'      THEN (v.void_qty - v.encoded_qty) END DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
```

---

## 3. High-Performance Indexing Strategy

In retail operations, queries filter by **Date Range** and **Store Code** 95% of the time. Without covering indexes, SQL Server performs expensive Index Scans or Clustered Index Scans across millions of RFID scan rows.

### Recommended Indexes for VMM Reporting:

```sql
-- 1. Void Transactions Covering Index
CREATE NONCLUSTERED INDEX IX_VoidTrans_Store_Date_Covering
ON dbo.tbl_Void_Transactions (store_code, transaction_date)
INCLUDE (void_qty, encoded_qty);

-- 2. Store GRC Summary Covering Index
CREATE NONCLUSTERED INDEX IX_StoreGRC_Store_Date_Covering
ON dbo.tbl_Store_GRC (store_code, grc_date)
INCLUDE (hu_received_qty, hu_validated_qty, hht_validate_qty, wrong_hu_qty);

-- 3. Cycle Count Batch Tracking Index
CREATE NONCLUSTERED INDEX IX_CycleCount_Store_Status_Date
ON dbo.tbl_Cycle_Count_Header (store_code, batch_status, start_date)
INCLUDE (batch_name, total_ean, counted_ean, difference_ean, percentage);

-- 4. Tag Lifecycle Tracking Index
CREATE NONCLUSTERED INDEX IX_TagInventory_Tid_Location
ON dbo.tbl_Tag_Inventory (tid, location_code)
INCLUDE (cycle_count, last_seen_date, tag_status);
```

---

## 4. Database Schema Migration Plan

| Legacy Monolithic Stored Proc | New Single-Responsibility Procedure | Primary Tables |
| :--- | :--- | :--- |
| `SP_NEW_REPORT (@Status = 'LAST7DAY_VOID_DASHBOARD')` | `usp_Report_VoidDetails` | `tbl_Void_Transactions`, `tbl_Stores` |
| `SP_NEW_REPORT (@Status = 'LAST7DAY_RETURN_DASHBOARD')` | `usp_Report_ReturnDetails` | `tbl_Return_Transactions`, `tbl_Stores` |
| `SP_STORE_GRC_API (@status = 'STORE_GRC_REPORT')` | `usp_Report_StoreGrcSummary` | `tbl_Store_GRC`, `tbl_Stores` |
| `SP_NEW_REPORT (@Status = 'SHOW_DATA_FOR_STORE_ENCODING')`| `usp_Report_AllocatedStoreEncoding` | `tbl_Store_Encoding`, `tbl_Articles` |
| `SP_NEW_REPORT (@Status = 'CYCLE_COUNT_REPORT')` | `usp_Report_CycleCountVariance` | `tbl_Cycle_Count_Header`, `tbl_Stores` |
| `SP_NEW_REPORT (@Status = 'VIEW_PARK_HU_VENDOR_REPORT')` | `usp_Report_VendorDiscrepancy` | `tbl_HU_Vendor_Discrepancy`, `tbl_Vendors` |
| `SP_Online_Inventory (@Status = 'TAG_MANAGEMENT_LOCATION')`| `usp_Report_TagInventoryLocation` | `tbl_Tag_Inventory`, `tbl_Locations` |

By separating these procedures:
- Query plans remain cached cleanly and execute in single-digit milliseconds.
- Any procedure can be updated or tested without touching the rest of the application.
- Naming is consistent across the entire database.
