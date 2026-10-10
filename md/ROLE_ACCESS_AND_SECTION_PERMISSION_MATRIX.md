# 🛡️ Role Access & Section Permission Matrix

> **Authoritative Reference**: Single source of truth for all role-based section and menu visibility across the VMM POS Solution.  
> **Rule**: Whenever any role access or section permissions are modified in backend (`AuthService.cs`, `ControlCenterService.cs`) or frontend (`AuthContext.jsx`, `Sidebar.jsx`, `DashboardPage.jsx`), **this document must be updated immediately**.

---

## 📋 1. Master Role vs. Section Permission Matrix (Application & UI Layer)

This matrix defines what sections, navigation tabs, and API endpoints are exposed or permitted at the **Application / Frontend & API Gateway Layer** (`AuthService.cs`, `AuthContext.jsx`, `Sidebar.jsx`).

| Section Key | Section / Sidebar Item | Super Admin | Store Admin | Store User | WH Admin | WH User | Tag Admin | Dispatch Admin |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `live_stock` | Live Stock Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `cycle_count` | Tag Cycle Count Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `store_validation` | Store Discrepancy Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `sale` | Sale Dashboard Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `void` | Void Dashboard Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `return` | Return Dashboard Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `dc_validation` | DC Validation Tab | ✅ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| `dc_encoding` | DC Encoding Tab | ✅ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| `tag_management` | Tag Management Tab | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `vendor_discrepancy` | Vendor Discrepancy Tab | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `store_counter_status` | Store Counter Status | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `tag_cleaning` | Tag Cleaning Report | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| `get_sap_stock_take` | SAP Stock Take Report | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ |
| `dispatch_tracking` | Dispatch Tracking | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| `picklist_creation` | Picklist Creation | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| `user_registration` | User Registration | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `store_registration` | Store Registration | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `warehouse_registration` | Warehouse Registration | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `floor_registration` | Floor Registration | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

---

## 👥 2. Role Descriptions & Scopes

### 1. Super Admin
* **Scope**: Unrestricted platform-wide access across India.
* **Sections**: All 10 Dashboard tabs, Store reports, Tag hygiene reports, and all 4 Master Registrations.
* **Exclusions**: `dispatch_tracking` and `picklist_creation` are kept strictly exclusive to Dispatch Admin.

### 2. Store Admin
* **Scope**: Single assigned store (`Store_ID` / `Store_Code`) or roving store administrator.
* **Sections**: All Store Dashboard tabs (`live_stock`, `cycle_count`, `store_validation`, `sale`, `void`, `return`), Store Counter Status, SAP Stock Take, User Registration (for managing store-level users), and Floor Registration (for configuring store layout/floors).

### 3. Store User / Store
* **Scope**: Single assigned store (`Store_ID` / `Store_Code`).
* **Sections**: All Store Dashboard tabs and Store Counter Status. Read/operate POS floor data.

### 4. Warehouse Admin / WH Admin
* **Scope**: Central Distribution Hub operations.
* **Sections**: All DC tabs (`dc_validation`, `dc_encoding`, `tag_management`, `vendor_discrepancy`), and User Registration.

### 5. Warehouse User / Warehouse
* **Scope**: DC floor operations.
* **Sections**: Scanning and encoding only (`dc_validation`, `dc_encoding`).

### 6. Dispatch Admin
* **Scope**: Dedicated logistics and outward dispatch tracking.
* **Sections**: Exclusively `dispatch_tracking` and `picklist_creation`. Isolated from retail POS and warehouse floors.

### 7. Tag Admin
* **Scope**: RFID tag lifecycle and audit reports.
* **Sections**: Exclusively `tag_cleaning` and `get_sap_stock_take`.

---

## 🗄️ 3. Database Query Level Allowed Data Matrix (SQL Server Stored Procedures)

This matrix defines what SQL Server stored procedures (`SP_New_Dashboard`, `SP_NEW_REPORT`, `SP_Master`) **actually return from the database** based on hardcoded SQL `WHERE`, `HAVING`, and `IF (@User_Type = ...)` blocks.

### Legend:
* **All Stores**: Database query returns data for all stores nationwide without store-code filtering.
* **Assigned Store**: Query filters strictly to `dbo.tbl_Store_Master.Store_Code = @Store_code` or `Store_ID = @Store_ID`.
* **Assigned WH**: Query filters strictly to `dbo.tbl_Warehouse_Mst.WH_ID = @WH_ID`.
* **Personal Only**: Query filters strictly to rows where `Encode_By = @User_ID`.
* **0 Rows (Blocked)**: The SQL procedure's hardcoded role condition evaluates to `FALSE` (e.g., role not listed in `IN ('Super Admin', 'Store Admin')`), resulting in an empty result set `[]`.

| Section Key | Stored Procedure & Status | Super Admin | Store Admin | Store User (`Store`) | WH Admin | WH User (`Warehouse`) | Tag Admin | Dispatch Admin |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `live_stock` | `SP_New_Dashboard`<br>`@Status='LIVE_STOCK_DASHBOARD'` | All Stores | Assigned Store | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ |
| `cycle_count` | `SP_New_Dashboard`<br>`@Status='CYCLE_COUNT_DASHBOARD'` | All Stores | Assigned Store | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ |
| `store_validation` | `SP_New_Dashboard`<br>`@Status='STORE_DASHBOARD'` | All Stores | Assigned Store | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ |
| `sale` | `SP_New_Dashboard`<br>`@Status='SALE_DASHBOARD'` | All Stores | Assigned Store | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ |
| `void` | `SP_New_Dashboard`<br>`@Status='VOID_DASHBOARD'` | All Stores | Assigned Store | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ |
| `return` | `SP_New_Dashboard`<br>`@Status='RETURN_DASHBOARD'` | All Stores | Assigned Store | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ |
| `dc_validation` | `SP_New_Dashboard`<br>`@Status='DC_VALIDATE_DASHBOARD'` | All Stores | Assigned Store | 0 Rows (Blocked) ⛔ | All Stores | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ |
| `dc_encoding` | `SP_NEW_REPORT`<br>`@Status='SHOW_WAREHOUSE_ENCODE_DATA'` | All WH Ops | 0 Rows (No WH_ID) | 0 Rows (No WH_ID) | All WH Ops | Personal Only (`@User_ID`) | 0 Rows ⛔ | 0 Rows ⛔ |
| `tag_management` | `SP_NEW_REPORT`<br>`@Status='TAG_MANAGEMENT_LOCATION'` | All WH | 0 Rows (No WH_ID) | 0 Rows (No WH_ID) | All WH | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ |
| `vendor_discrepancy` | `SP_New_Dashboard`<br>`@Status='HU_DISCREPANCY_VENDOR_DASHBOARD'` | All Vendors | All Vendors | All Vendors | All Vendors | All Vendors | All Vendors | All Vendors |
| `store_counter_status` | `SP_Master`<br>`@Status='STORENAME_FOR_COUNTER_STATUS'` | All Stores | Assigned Store | Assigned Store | 0 Rows (No Store_ID) | 0 Rows (No Store_ID) | 0 Rows ⛔ | 0 Rows ⛔ |
| `tag_cleaning` | `SP_NEW_REPORT`<br>`@Status='TAG_CLEANING_REPORT'` | All Tags | All Tags | All Tags | All Tags | All Tags | All Tags | All Tags |
| `get_sap_stock_take` | `SP_NEW_REPORT`<br>`@Status='VIEW_STOCK_TAKE_REPORT'` | All Stores | Assigned Store | Assigned Store | All Stores | 0 Rows ⛔ | All Stores | 0 Rows ⛔ |
| `dispatch_tracking` | Dedicated Tables / Outward SP | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | Outward Lots ✅ |
| `picklist_creation` | Dedicated Tables / Picklist SP | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | Picklists ✅ |
| `user_registration` | `SP_Master`<br>`@Status='SP_Bind_User_Master'` | All Users | Assigned Store (Store & Store Admin only) | 0 Rows (Blocked) ⛔ | Assigned WH (WH & WH Admin only) | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ | 0 Rows (Blocked) ⛔ |
| `store_registration` | `SP_Master`<br>`@Status='SP_DDL_StoreID'` | All Stores | Assigned Store | Assigned Store | Assigned Store | Assigned Store | 0 Rows ⛔ | 0 Rows ⛔ |
| `warehouse_registration` | `SP_Master`<br>`@Status='SP_DDL_WarehouseID'` | All WH | 0 Rows ⛔ | 0 Rows ⛔ | Assigned WH | Assigned WH | 0 Rows ⛔ | 0 Rows ⛔ |
| `floor_registration` | `SP_Master`<br>`@Status='SP_Bind_FloorMaster'` | All Floors | Assigned Store | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ | 0 Rows ⛔ |

---

### 🔍 Exact SQL Predicate Analysis by Procedure

#### 1. `SP_New_Dashboard` (Stored Procedure Filter Logic)
* **User Type Resolution** (Lines 65–67):
  ```sql
  SELECT @User_Type = User_Type FROM dbo.[User_Registration] WHERE User_ID = @User_ID;
  SELECT @Store_code = dbo.[tbl_Store_Master].Store_code 
  FROM dbo.[tbl_Store_Master] 
  JOIN dbo.[User_Registration] ON dbo.[tbl_Store_Master].Store_ID = dbo.[User_Registration].Store_ID 
  WHERE User_ID = @User_ID;
  ```
* **Store POS Dashboards (`LIVE_STOCK`, `CYCLE_COUNT`, `SALE`, `VOID`, `RETURN`, `STORE_DASHBOARD`)**:
  ```sql
  -- Example: Sale Dashboard (Line 421)
  WHERE SM.Is_Status = '1' AND (@User_Type = 'Super Admin' OR (@User_Type = 'Store Admin' AND p.Store_Code = @Store_code))
  
  -- Example: Void Dashboard (Line 212)
  HAVING (@User_Type = 'Super Admin' OR (@User_Type = 'Store Admin' AND SM.Store_Code = @Store_code))
  
  -- Example: Return Dashboard (Line 151)
  HAVING (@User_Type = 'Super Admin' OR (@User_Type = 'Store Admin' AND V.Store_Code = @Store_code))
  ```
  > ⚠️ **Key Database Invariant**: `User_Type = 'Store'` (Store User) is **omitted** from the SQL check. If called with `@User_ID` of a Store User, SQL Server evaluates `(@User_Type = 'Super Admin' OR (@User_Type = 'Store Admin' AND ...))` as `FALSE`, returning 0 rows!

* **DC Validation Dashboard** (Line 90):
  ```sql
  WHERE (@User_Type IN ('Super Admin','Warehouse Admin') OR (@User_Type = 'Store Admin' AND dbo.tbl_Store_Master.Store_Code = @Store_code))
  ```
  > ⚠️ `Warehouse` (Warehouse User) is omitted from `IN ('Super Admin', 'Warehouse Admin')`.

* **Vendor Discrepancy Dashboard** (Line 947):
  No `@User_Type` filter exists inside `HU_DISCREPANCY_VENDOR_DASHBOARD`. If called, it returns vendor summary calculations regardless of the caller's role.

---

#### 2. `SP_Master` (Stored Procedure Filter Logic)
* **User Management (`SP_Bind_User_Master`)** (Lines 284–290):
  ```sql
  WHERE (
      (@User_Type = 'Store Admin' AND UR.User_Type IN ('Store', 'Store Admin') AND UR.Store_ID = @Store_ID)
      OR
      (@User_Type = 'Warehouse Admin' AND UR.User_Type IN ('Warehouse', 'Warehouse Admin') AND UR.WH_ID = @WH_ID)
      OR
      (@User_Type = 'Super Admin')
  );
  ```
  > ✅ Super Admin sees all users. Store Admin only sees users within their store. Warehouse Admin only sees users within their warehouse. Junior users (`Store`, `Warehouse`, `Tag Admin`) match none of these branches and get 0 rows.

* **Store Dropdown (`SP_DDL_StoreID`)** (Lines 296–305):
  ```sql
  IF (@User_Type = 'Super Admin')
  BEGIN
      SELECT Distinct Store_Name, Store_ID FROM tbl_Store_Master WHERE IS_STATUS = 1;
  END
  ELSE
  BEGIN
      SELECT ISNULL(Store_Name, 'NA') AS 'Store_Name', SM.Store_ID 
      FROM dbo.User_Registration UR 
      LEFT JOIN tbl_Store_Master SM ON UR.Store_ID = SM.Store_ID 
      WHERE UR.User_ID = @User_ID AND SM.Is_Status = 1;
  END
  ```

* **Counter Status (`STORENAME_FOR_COUNTER_STATUS`)** (Lines 326–340):
  ```sql
  IF (@ReqUserType = 'Super Admin' OR @User_ID = 0)
      SELECT Distinct Store_ID, Store_Name, Store_Code FROM dbo.tbl_Store_Master WHERE Is_Status = '1';
  ELSE
      SELECT Distinct Store_ID, Store_Name, Store_Code FROM dbo.tbl_Store_Master WHERE Store_ID = @ReqStoreId AND Is_Status = '1';
  ```

---

#### 3. `SP_NEW_REPORT` (Stored Procedure Filter Logic)
* **Store Dropdown (`BIND_STORE_FOR_ALL`)** (Lines 70–75):
  ```sql
  SELECT DISTINCT S.Store_Code AS 'STORE_CODE', S.Store_Name AS 'STORE_NAME' 
  FROM dbo.tbl_Store_Master S 
  LEFT JOIN dbo.User_Registration U ON S.Store_ID = U.Store_ID
  WHERE U.User_ID = @USER_ID  
     OR EXISTS (SELECT 1 FROM dbo.User_Registration WHERE User_ID = @USER_ID AND User_Type IN ('Super Admin','Warehouse Admin'))
  ORDER BY S.Store_Code DESC;
  ```
  > `Warehouse Admin` receives all stores, while `Tag Admin` receives 0 stores if no `Store_ID` is attached to their login!

---

## ⚖️ 4. Side-by-Side Comparison: Application Layer vs. Database Query Layer

This section highlights the direct differences and discrepancies between what the **Application / UI Layer** intends to show versus what the **Database Query Layer** natively permits.

### Status Indicators:
* 🟢 **Aligned**: Both application layer and database query enforce the identical scope.
* 🟡 **RAM-Bridged**: The application layer allows access, but the legacy database query would return 0 rows if invoked directly. The backend currently bridges this by passing `@User_ID = 0` or a master ID and slicing the result in C# memory.
* 🔴 **Query Blocked**: If an administrator enables this UI section for the role, the database stored procedure will return an empty set (`0 rows`) unless the database query is updated with role permissions.

| Section Key | Section Name | App Layer Allowed Roles | DB Query Allowed Roles | Alignment Status | Discrepancy & Technical Impact |
| :--- | :--- | :--- | :--- | :---: | :--- |
| `live_stock` | Live Stock Tab | Super Admin, Store Admin, **Store User** | Super Admin, Store Admin | 🟡 **RAM-Bridged** | **Store User Gap**: App allows Store User, but `SP_New_Dashboard` checks `@User_Type = 'Store Admin'`. Backend currently queries company-wide and filters in RAM (`MatchStore`). |
| `cycle_count` | Cycle Count Tab | Super Admin, Store Admin, **Store User** | Super Admin, Store Admin | 🟡 **RAM-Bridged** | Same as Live Stock. Store User is blocked at SQL level; backend RAM bridge serves data. |
| `store_validation`| Store Discrepancy | Super Admin, Store Admin, **Store User** | Super Admin, Store Admin | 🟡 **RAM-Bridged** | Store User blocked by `WHERE (@User_Type = 'Super Admin' OR (@User_Type = 'Store Admin' AND ...))`. |
| `sale` | Sale Dashboard | Super Admin, Store Admin, **Store User** | Super Admin, Store Admin | 🟡 **RAM-Bridged** | Store User blocked at SQL level; served via backend master cache slice. |
| `void` | Void Dashboard | Super Admin, Store Admin, **Store User** | Super Admin, Store Admin | 🟡 **RAM-Bridged** | Store User blocked at SQL level; served via backend master cache slice. |
| `return` | Return Dashboard | Super Admin, Store Admin, **Store User** | Super Admin, Store Admin | 🟡 **RAM-Bridged** | Store User blocked at SQL level; served via backend master cache slice. |
| `dc_validation` | DC Validation Tab | Super Admin, WH Admin, **WH User** | Super Admin, WH Admin, **Store Admin** | 🔴 **Query Blocked** / 🟡 **Leakage** | **WH User Blocked**: `SP_New_Dashboard` line 90 only allows `Super Admin` and `Warehouse Admin`. WH User gets 0 rows from SQL.<br>**Store Admin Leakage**: Line 90 allows Store Admin to see their receiving plant, but App layer disables the tab for Store Admin! |
| `dc_encoding` | DC Encoding Tab | Super Admin, WH Admin, WH User | Super Admin, WH Admin, WH User | 🟢 **Aligned** | SQL filters by `@User_ID` for individual operators or returns all if admin. |
| `tag_management` | Tag Management | Super Admin, WH Admin | Super Admin, WH Admin | 🟢 **Aligned** | Open warehouse-level report; both layers restrict to WH Admin / Super Admin. |
| `vendor_discrepancy`| Vendor Discrepancy | Super Admin, WH Admin | **All Roles** (Unfiltered in SQL) | 🟡 **App Restricted Only** | SQL Server has no role check in `HU_DISCREPANCY_VENDOR_DASHBOARD`. Security relies 100% on Application Layer (`AuthService.cs`). |
| `store_counter_status`| Counter Status | Super Admin, Store Admin, Store User | Super Admin, Store Admin, Store User | 🟢 **Aligned** | `STORENAME_FOR_COUNTER_STATUS` natively supports `@ReqStoreId` for both Store Admin and Store User. |
| `tag_cleaning` | Tag Cleaning | Super Admin, Tag Admin | **All Roles** (Unfiltered in SQL) | 🟢 **App Restricted** | Database query has no `@User_Type` filter; app layer limits to Tag Admin and Super Admin. |
| `get_sap_stock_take`| SAP Stock Take | Super Admin, Store Admin, Tag Admin | Super Admin, Store Admin, Tag Admin | 🟡 **Dropdown Blocked** | `VIEW_STOCK_TAKE_REPORT` accepts `@Store_code`, but `BIND_STORE_FOR_ALL` dropdown only returns all stores for Super Admin and WH Admin. Tag Admin gets 0 stores in dropdown if no `Store_ID` assigned. |
| `dispatch_tracking`| Dispatch Tracking | Dispatch Admin | Dispatch Admin | 🟢 **Aligned** | Dedicated tables and outward logistics stored procedures. Isolated from POS and DC floors. |
| `picklist_creation`| Picklist Creation | Dispatch Admin | Dispatch Admin | 🟢 **Aligned** | Dedicated tables (`tbl_Picklist_Mst`). Isolated from POS and DC floors. |
| `user_registration`| User Registration | Super Admin, Store Admin, WH Admin | Super Admin, Store Admin, WH Admin | 🟢 **Aligned** | Both layers enforce scoped user management: Store Admin can only view store users, WH Admin can only view warehouse users. |
| `store_registration`| Store Registration | Super Admin | Super Admin | 🟢 **Aligned** | Master management restricted strictly to Super Admin across both layers. |
| `warehouse_registration`| Warehouse Reg. | Super Admin | Super Admin | 🟢 **Aligned** | Master management restricted strictly to Super Admin across both layers. |
| `floor_registration`| Floor Registration | Super Admin, Store Admin | Super Admin, Store Admin | 🟢 **Aligned** | Floor layout configuration scoped to assigned `Store_ID` in both layers. |

---

## 🛠️ 5. Architectural Reconciliation & Dynamic Control Center Design

### The Core Problem:
If an administrator uses the new **Super Admin Control Center** to unlock a section for a user (e.g., granting `dc_validation` to a Store Admin or `live_stock` to a junior Store User):
1. **Application Layer**: React renders the tab, and the API gateway permits the HTTP request.
2. **Database Query Layer**: The legacy stored procedure executes its hardcoded condition `WHERE @User_Type = 'Store Admin'` or `WHERE @User_Type IN ('Super Admin', 'Warehouse Admin')`.
3. **The Consequence**: SQL Server evaluates the condition to `FALSE` and returns **0 rows (`[]`)**! The UI appears blank or broken to the user.

### The Two Possible Solutions & Strategic Decision:

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        HOW TO SERVE DATA FOR DYNAMIC PERMISSIONS                       │
├───────────────────────────────────────────┬────────────────────────────────────────────┤
│   APPROACH 1: BACKEND MEMORY BRIDGING     │    APPROACH 2: DATABASE QUERY PARAMETER    │
│            (RAM OVER-FETCH)               │           (OPTIMIZED & INDEXED)            │
├───────────────────────────────────────────┼────────────────────────────────────────────┤
│ • Backend queries SQL as Super Admin      │ • SP queries `dbo.App_Role_Permissions`    │
│   (fetching 120 KB company-wide payload). │   or accepts `@Allowed_Store` parameter.   │
│ • Slices and filters rows in C# RAM.      │ • SQL Server only reads target store's     │
│ • High RAM, thread, and network waste.    │   indexed rows (~16 KB payload).           │
│ • Vulnerable to memory spikes.            │ • Clean, scalable, zero memory overhead.   │
└───────────────────────────────────────────┴────────────────────────────────────────────┘
```

### Adopted Strategic Implementation (Dual-Shield Protection):
1. **Dynamic Permission Tables**:
   - `dbo.App_Role_Permissions`: Role-level default toggles.
   - `dbo.App_User_Permissions`: User-specific granular overrides.
2. **Database Stored Procedure Enhancement**:
   - Instead of risky complete rewrites, append clean permission existence checks to `WHERE` clauses:
     ```sql
     WHERE (
         @User_Type = 'Super Admin'
         OR (@User_Type IN ('Store Admin', 'Store') AND S.Store_Code = @Store_code)
         OR EXISTS (
             SELECT 1 FROM dbo.App_Role_Permissions P 
             WHERE P.Role_Name = @User_Type 
               AND P.Section_Key = 'live_stock' 
               AND P.Is_Allowed = 1
               AND S.Store_Code = @Store_code
         )
     )
     ```
3. **API Authorization Filter (`[AuthorizeSection]`)**:
   - Every protected controller endpoint checks the caller's database permission before query execution.
   - Rejects unauthorized direct API / Postman attempts with `HTTP 403 Forbidden`.
4. **Real-Time SignalR Synchronization**:
   - When Super Admin toggles any permission in the Control Center UI, SignalR broadcasts `ReceivePermissionsUpdated` in `< 15ms`, updating the client's navigation and state without requiring page reload.

---

## 🔒 6. Mandatory User Identity Enforcement (`UserIdValidationMiddleware`)

To ensure that the backend API gateway and SQL Server always enforce role scoping and never serve anonymous or cross-tenant data:

### 1. Backend Middleware Shield (`UserIdValidationMiddleware`)
* **Scope**: Intercepts **all** incoming HTTP requests starting with `/api/`.
* **Exempt Routes** (Unauthenticated or infrastructure endpoints):
  - `/api/auth/login`
  - `/api/auth/change-password`
  - `/api/auth/reset-password`
  - `/api/system/`
  - `/swagger/`
  - `/hubs/` (SignalR WebSocket handshakes)
* **Identity Detection Priority**:
  1. Query String parameter: `?userId=...`, `?UserId=...`, `?user_id=...`
  2. Request Header: `X-User-Id` or `UserId`
  3. Buffered JSON Request Body: `userId`, `UserId`, `user_id`
* **Enforcement Action**:
  - If `userId` is missing, unparseable, or `<= 0`: Immediately abort the request and return **`HTTP 400 Bad Request`**:
    ```json
    {
      "statusCode": 400,
      "error": "Bad Request",
      "message": "userId is mandatory for this request. Please provide a valid userId query parameter (?userId=...) or X-User-Id header."
    }
    ```
  - If valid: Stores `context.Items["UserId"] = userId` for downstream controllers and services.

### 2. Frontend Dual-Layer Identity Injection
* **Layer 1: Global Axios Request Interceptor** (`stockService.js`):
  - Dynamically extracts the active `userID` from `sessionStorage.getItem('vmm_user')`.
  - Injects `X-User-Id: <id>` header onto every Axios HTTP request across the entire React application.
  - Automatically appends `userId=<id>` query parameter if not already provided.
* **Layer 2: Service-Level Explicit Parameters & Headers**:
  - `stockService.js`, `storeService.js`, `dispatchService.js`, `masterAuthService.js`, and `useStreamingExport.js` explicitly append `userId` query parameter and pass `X-User-Id` header to guarantee that even raw `fetch` calls (e.g. streaming file downloads) comply.

### 3. Stored Procedure `@User_ID` Forwarding Invariant
All backend service layers pass `@USER_ID` into SQL Server Stored Procedures (`SP_NEW_REPORT`, `SP_NEW_DASHBOARD`, `SP_Master`):
* Live Stock Report (`GetStoresAsync`, `SearchArticlesAsync`, `QueryLiveStockDetailsFromDbAsync`)
* Cycle Count Report (`GetCycleCountReportViewAsync`, `QueryCycleCountDetailsFromDbAsync`)
* Sale Dashboard (`GetStoreSaleReportAsync`, `BindPOSCounterAsync`, `SearchArticlesSaleAsync`, `SearchEANSaleAsync`)
* Void Dashboard (`GetStoreVoidReportAsync`, `BindPOSCounterVoidAsync`)
* Return Dashboard (`GetStoreReturnReportAsync`)
* Store GRC Report (`SearchHuNumbersAsync`, `QueryGrcDetailsFromDbAsync`, `QueryGrcModalDetailsFromDbAsync`)
* DC Dashboard (`GetDcDashboardDataAsync`)
* Tag Management (`GetStoresAsync`, `QueryTagManagementFromDbAsync`)
* HU Discrepancy (`GetVendorDiscrepancyReportAsync`)
* Store Counter Status (`GetCounterStatusAsync`)
* Stock Take Report (`GetStockTakeReportsAsync`)
* Warehouse Encoding (`GetOperatorWiseEncodingDataAsync`, `GetDateWiseEncodingDataAsync`)
* Main Dashboard Aggregator (`GetTagCycleCountDataAsync`, `GetTagManagementDataAsync`, `GetWarehouseEncodingDataAsync`)
