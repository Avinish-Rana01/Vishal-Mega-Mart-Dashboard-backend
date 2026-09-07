# Vishal-Mega-Mart-Dashboard-backend 🚀

Welcome to the **VS Mart Backend** repository! This is a modern, high-performance .NET 8 Web API built to power the POS Web Application Dashboard. It serves as a direct, optimized replacement for the legacy ASP.NET WebForms architecture.

---

## 🏗️ Folder Structure & Architecture (For Beginners)

This project follows a **Feature-Based Architecture**. Instead of separating all controllers into one folder and all services into another (which gets messy as the project grows), we group everything by its **Feature**. 

If you need to fix a bug in the "Sale Dashboard", you only need to look inside the `Features/SaleDashboard` folder!

Here is the exact layout of the `Features/` folder and what you will find inside:

### 1. `Features/Base/`
- **`BaseDashboardService.cs`**: The parent class that every other service inherits from. It handles the Database Connection (`_connectionString`) and the central SWR Caching logic (`GetOrCreateWithSWRAsync`).

### 2. `Features/CycleCountReport/`
Handles Cycle Count specific reports.
- `[HttpGet("/api/Stock/cycle-count-report")]`
- `[HttpGet("/api/Stock/cycle-count-details")]`

### 3. `Features/DcDashboard/`
Handles Distribution Center (DC) details.
- `[HttpGet("api/Stock/GetDCDetails")]`

### 4. `Features/LiveStockReport/`
Handles the core Live Stock data and filtering utilities.
- `[HttpGet("api/report/stores")]`
- `[HttpGet("api/report/articles/search")]`
- `[HttpGet("api/report/live-stock")]`

### 5. `Features/MainDashboard/`
This is the master controller for the primary high-level dashboard summaries.
- `[HttpGet("api/Stock/live-details")]`
- `[HttpGet("api/Stock/tag-cycle-count")]`
- `[HttpGet("api/Stock/store-dashboard")]`
- `[HttpGet("api/Stock/sale-dashboard")]`
- `[HttpGet("api/Stock/return-dashboard")]`
- `[HttpGet("api/Stock/dc-validate-dashboard")]`
- `[HttpGet("api/Stock/cycle-count-dashboard")]`
- `[HttpGet("api/Stock/vendor-hu-discrepancy")]`
- `[HttpGet("api/Stock/tag-management-location")]`
- `[HttpGet("api/Stock/warehouse-encoding")]`

### 6. `Features/ReturnDashboard/`
Handles Return tracking and reconciliation.
- `[HttpGet("api/Stock/dashboard/return-details")]`
- `[HttpGet("api/Stock/void/return-reconciliation")]`

### 7. `Features/SaleDashboard/`
Handles Sale performance, mapping, and POS counter data.
- `[HttpGet("/api/Stock/store-sale-report")]`
- `[HttpGet("/api/Stock/sale/pos-counters")]`
- `[HttpGet("/api/Stock/sale/articles")]`
- `[HttpGet("/api/Stock/sale/eans")]`
- `[HttpGet("/api/Stock/sale-data")]`

### 8. `Features/StoreGrcReport/`
Handles Goods Receipt (GRC) and Handling Unit (HU) logic.
- `[HttpGet("api/grc-report/hu-numbers/search")]`
- `[HttpGet("api/grc-report/details")]`
- `[HttpGet("api/grc-report/modal-details")]`
- `[HttpGet("/api/stock/store-grc-report")]`
- `[HttpGet("/api/stock/Hu-details")]`

### 9. `Features/SystemUtility/`
Handles Authentication and global application caching mechanisms.
- `[HttpPost("/api/Auth/login")]`
- `[HttpGet("/api/Stock/cache-status")]`
- `[HttpPost("/api/Stock/toggle-cache")]`
- `[HttpGet("/api/Stock/GetEncodingStoreData")]`

### 10. `Features/VoidDashboard/`
Handles Voided transaction data and EAN searches.
- `[HttpGet("/api/Stock/void-dashboard")]` (Wait, MainDashboard also has a void-dashboard summary, this one is for detailed views)
- `[HttpGet("/api/Stock/GetVoidDetails")]`
- `[HttpGet("/api/Stock/GetVoidReconciliationData")]`
- `[HttpGet("/api/Stock/void/pos-counters")]`
- `[HttpGet("/api/Stock/void-SearchEAN")]`

---

## 🌊 Project Flow: How Data Moves (For New Developers)

If you are new to the project, the easiest way to understand the backend is to follow a request from start to finish. Here is exactly what happens when a user clicks a button on the dashboard:

### 1. The Request (The Customer Orders)
The React frontend makes an HTTP GET request to our server. For example: `http://localhost:5000/api/Stock/sale-dashboard?userId=26&pageIndex=1`.

### 2. The Controller (The Waiter)
The request first hits the **Controller** (e.g., `MainDashboardController.cs`). The Controller's *only* job is to listen for requests. It takes the messy URL parameters and maps them into a clean, strictly typed C# object called a **QueryRequest DTO** (Data Transfer Object). Once the data is organized, the Controller hands it off to the Service layer. *Controllers do not write SQL or do math!*

### 3. The Service (The Chef)
The **Service** (e.g., `MainDashboardService.cs`) is where all the heavy lifting happens. It looks at the QueryRequest and executes the business logic:
- **Cache Check:** It first calls `GetOrCreateWithSWRAsync()` (from `BaseDashboardService`) to check if we recently asked for this exact data (using our SWR Cache Engine). If we have it in memory, it instantly returns it!
- **Database Query:** If the cache is empty or stale, it connects to the SQL Server. We use **Dapper** (`connection.QueryAsync<dynamic>`) to execute the legacy Stored Procedures (like `SP_New_Dashboard`).

### 4. Data Processing (Boxing the Food)
The Service reads the raw rows coming back from Dapper. Instead of passing ugly SQL data directly to the user, the Service neatly organizes this raw data into a pristine **Response DTO**. Summary metrics (from SQL `OUTPUT` parameters) and list items are bundled together.

### 5. The Delivery
The Service passes the clean Response DTO back to the Controller. The Controller simply wraps it in an HTTP 200 Success code (`Ok(response)`) and the ASP.NET framework automatically translates it into JSON format before sending it across the internet back to the user's browser.

---

## ⚡ Advanced Caching: "Stale-While-Revalidate" (SWR)

To completely eliminate the "2-Second Gap" (Cache Stampede) that occurs during heavy dashboard usage, we have implemented a custom **Stale-While-Revalidate** caching engine inside `BaseDashboardService.cs`.

**How it works:**
1. The cache has a hard expiration of 90 seconds, but a "Stale Threshold" of **20 seconds**.
2. If a user hits the API and the cache is older than 20s, the API instantly returns the slightly stale data to the user with **0ms wait time**.
3. A background `Task.Run` is immediately fired to query the SQL Database for fresh data and silently updates the cache in the background.

*Never use simple `_cache.Set()` for high-traffic endpoints; always use the `GetOrCreateWithSWRAsync` wrapper.*

---

## ⚠️ Critical Legacy Rules

When migrating old WebForms Stored Procedures, you **MUST** adhere to the following rule:
- **The Empty String Rule**: When mapping missing or null string parameters to Dapper `DynamicParameters`, you must pass `""` (Empty String) instead of `DBNull.Value`. 
  - *Why?* The legacy Stored Procedures (like `SP_NEW_REPORT` and `SP_New_Dashboard`) expect empty strings. Passing `NULL` will cause the `WHERE` clauses to fail and return `0` rows!

---

## 🚀 How to Run Locally

1. Ensure you have the .NET 8 SDK installed.
2. Open your terminal in the `VS Mart Backend` directory.
3. Run the following command:
   ```bash
   dotnet run
   ```
4. The API will be available at `http://localhost:5000`.
