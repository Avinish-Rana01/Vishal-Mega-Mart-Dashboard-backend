        ### WebForms to .NET Core Migration
- **Legacy SQL Parameters**: When migrating legacy ASP.NET Web Forms ADO.NET code to modern .NET Core, always map missing or null string parameters to `""` (Empty String) instead of `DBNull.Value`. Legacy Stored Procedures in this codebase often expect empty strings, and sending `NULL` will cause `WHERE` clauses to fail and return zero records.

### Project Structure
- **Backend Directory**: Always create new C# backend code (e.g., API Controllers, Models, Services) inside the active `VS Mart Backend` project directory (e.g., `c:\Users\MARKSS\OneDrive\Documents\VS mart Backend\VS Mart Backend\VS Mart Backend\`). Never create project files in temporary or learning directories unless explicitly requested.
# Project Knowledge & Architecture Context (For New Developers & Agents)

## 1. Architectural Patterns
- **Controller-Service Pattern**: All business logic and SQL execution must live in the `Services` layer (e.g., `LiveStockService.cs`). Controllers must remain thin, handling only HTTP routing, input validation, and calling the injected service.
- **Dependency Injection**: All new Services must be registered as Scoped or Singleton in `Program.cs` (`builder.Services.AddScoped<...>`).
- **Caching & Background Workers**: We use an aggressive caching strategy. `CacheWarmerService.cs` runs as an `IHostedService` to pre-warm the cache. In the Services, we use a custom **Stale-While-Revalidate (SWR)** pattern (`GetOrCreateWithSWRAsync`) using `IMemoryCache`. NEVER bypass the caching layer for dashboard data unless explicitly required.

## 2. Database & ADO.NET Rules
- **No Entity Framework**: This project uses raw ADO.NET (`SqlConnection`, `SqlCommand`, `SqlDataReader`) to execute legacy Stored Procedures. Do not attempt to introduce Entity Framework.
- **Connection Management**: Always use `using` blocks for `SqlConnection`, `SqlCommand`, and `SqlDataReader` to prevent connection pool exhaustion.
- **SQL Server Casing Bugs**: The C# ADO.NET mapping MUST use `Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)`. This is CRITICAL because the legacy SQL stored procedures (like `SP_NEW_DASHBOARD`) often return mixed-case columns (e.g., `Store_Name` instead of `STORE_NAME`). If you instantiate case-sensitive dictionaries, lookups for `row["STORE_NAME"]` will crash or return null.

## 3. Frontend Integration Context
- **Metadata Fallbacks**: The React UI heavily relies on the `Summary` block in API responses. If 0 rows are returned, the UI expects `Summary.StoreName` and `Summary.Date` to be populated so the info bar can render. The C# backend handles this by safely extracting the metadata from `request.StoreName` if the data grid is empty.
- **JSON Serialization**: C# PascalCase properties (like `StoreName`) are automatically serialized to camelCase (`storeName`) by ASP.NET Core defaults. Do not manually add `[JsonPropertyName]` unless overriding this default behavior.

## 4. Planned Features: RBAC (Role-Based Access Control)
- **Current Auth State**: `AuthController.cs` (`POST /api/auth/login`) validates users against `SP_Master` but **DOES NOT** issue a JWT token. Additionally, it contains hardcoded logic (`Forbid()`) that blocks any user with `User_Type` of `"Store"` or `"Warehouse"`.
- **The RBAC Plan**: We are migrating to JWT-based authentication. The next major step is to update `AuthController.cs` to generate and return a JWT containing `Role` and `StoreCode` claims.
- **Data Filtering Strategy (Row-Level Security)**: Instead of duplicating our 20 APIs for different roles, we will use **Parameter Interception**. Services (like `LiveStockService.cs`) will use `IHttpContextAccessor` to read the JWT. If the user is a Biller or Manager, the backend will completely ignore their requested `StoreCode` and force-override the SQL parameter with the `StoreCode` from their token. Admins will bypass this override. Do not create new APIs for different user roles; use this interception strategy!

## 5. Legacy API Migration Constraints
When migrating or referencing legacy ASP.NET `[WebMethod]` functions to modern .NET Web APIs:
1. **Strict Parameter Mapping**: You MUST extract every single argument from the legacy `[WebMethod]` signature and ensure they are included in the modern DTO (e.g., `ref_no`, `store_code`, `fromDate`, `toDate`).
2. **Exact Stored Procedure Statuses**: You MUST use the exact `@status` flag passed to the stored procedure in the legacy code (e.g., `CYCLE_COUNT_REPORT`). Do not invent new statuses (e.g., `CYCLE_COUNT_DASHBOARD`) unless explicitly requested by the user.
3. **Output Variables**: You MUST map all `ParameterDirection.Output` variables used in the legacy `SqlCommand` to the summary DTO of the modern API.
        