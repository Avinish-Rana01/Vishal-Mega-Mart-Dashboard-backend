# Vishal-Mega-Mart-Dashboard-backend 🚀

Welcome to the **VS Mart Backend** repository! This is a modern, high-performance .NET 8 Web API built to power the POS Web Application Dashboard. It serves as a direct, optimized replacement for the legacy ASP.NET WebForms architecture.

## 🏗️ Architecture

This project follows a clean N-Tier architecture (Controller-Service-Model) to separate concerns:

- **`Controllers/` (The Waiters):** Handles incoming HTTP requests, route mapping, and returns JSON responses.
- **`Models/` (The DTOs):** Contains Data Transfer Objects defining the strict Request and Response schemas.
- **`Services/` (The Kitchen):** Contains the core business logic and Database communication (ADO.NET). 
- **`appsettings.json`:** Stores environment variables and Database connection strings.

---

## ⚡ Advanced Caching: "Stale-While-Revalidate" (SWR)

To completely eliminate the "2-Second Gap" (Cache Stampede) that occurs during heavy dashboard usage, we have implemented a custom **Stale-While-Revalidate** caching engine inside `LiveStockService.cs`.

**How it works:**
1. The cache has a hard expiration of 2 minutes, but a "Stale Threshold" of **1 minute 40 seconds**.
2. If a user hits the API and the cache is older than 1m 40s, the API instantly returns the slightly stale data to the user with **0ms wait time**.
3. A background `Task.Run` is immediately fired to query the SQL Database for fresh data and silently updates the cache in the background.

*Never use simple `_cache.Set()` for high-traffic endpoints; always use the `GetOrCreateWithSWRAsync` wrapper.*

---

## ⚠️ Critical Legacy Rules for Future Developers

When building new APIs or migrating old WebForms Stored Procedures, you **MUST** adhere to the following rule:

- **The Empty String Rule**: When mapping missing or null string parameters to ADO.NET `SqlCommand`, you must pass `""` (Empty String) instead of `DBNull.Value`. 
  - *Why?* The legacy Stored Procedures (like `SP_NEW_REPORT` and `SP_New_Dashboard`) expect empty strings. Passing `NULL` will cause the `WHERE` clauses to fail and return `0` rows!

---

## 🗺️ API Documentation (21 Total Endpoints)

### 1. GRC Report APIs (`GrcReportController.cs`)
- `GET /api/grc-report/hu-numbers/search`: HU Number Autocomplete (Pass `storeCode` to filter by store).
- `GET /api/grc-report/details`: Main Grid Data.
- `GET /api/grc-report/modal-details`: Drill-down data (returns Qty, MaterialCount, ActualQty).

### 2. General Report APIs (`ModernReportController.cs`)
- `GET /api/report/stores`: List of stores.
- `GET /api/report/articles/search`: Article autocomplete.
- `GET /api/report/live-stock`: Core live stock report data.

### 3. Core Dashboard APIs (`StockController.cs`)
*These endpoints are powered by `LiveStockService.cs` and utilize the SWR Caching Engine.*
- `GET /api/Stock/live-details`
- `GET /api/Stock/report`
- `GET /api/Stock/tag-cycle-count`
- `GET /api/Stock/store-dashboard`
- `GET /api/Stock/sale-dashboard`
- `GET /api/Stock/return-dashboard`
- `GET /api/Stock/void-dashboard`
- `GET /api/Stock/dc-validate-dashboard`
- `GET /api/Stock/cycle-count-report`
- `GET /api/Stock/cycle-count-dashboard`
- `GET /api/Stock/vendor-hu-discrepancy`
- `GET /api/Stock/tag-management-location`
- `GET /api/Stock/warehouse-encoding`
- `GET /api/Stock/cache-status`
- `POST /api/Stock/toggle-cache`

---

## 🚀 How to Run Locally

1. Ensure you have the .NET 8 SDK installed.
2. Open your terminal in the `VS Mart Backend` directory.
3. Run the following command:
   ```bash
   dotnet run
   ```
4. The API will be available at `http://localhost:5000`.
