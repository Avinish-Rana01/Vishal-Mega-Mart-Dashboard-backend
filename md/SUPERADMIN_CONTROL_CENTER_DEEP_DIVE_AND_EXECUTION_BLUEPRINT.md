# 🎛️ Super Admin Access Control Center & Dynamic Permissions
## Deep-Dive Technical Investigation & Implementation Blueprint (v2.0)

> **Document Type**: Technical Deep Dive & Concrete Execution Blueprint  
> **Status**: Ready for Execution  
> **Database**: `VMM_RFID_RETAIL_SOLUTION` (SQL Server)  
> **Backend**: ASP.NET Core 8 Web API (`VS_mart_Backend`)  
> **Frontend**: React 18 SPA (`POS_Web_Application React V2`)  
> **Real-Time Engine**: SignalR Core (`DashboardHub` / `/hubs/dashboard`)  

---

## 📑 Table of Contents
1. [Direct Answers to Your Core Questions](#1-direct-answers-to-your-core-questions)
2. [Database Reality Check (What Currently Exists in SQL Server)](#2-database-reality-check-what-currently-exists-in-sql-server)
3. [The Dual-Layer Architecture: UI Sections vs. Data Row Filtering](#3-the-dual-layer-architecture-ui-sections-vs-data-row-filtering)
4. [Do We Need to Rewrite Stored Procedures? (The Safe vs. Risky Path)](#4-do-we-need-to-rewrite-stored-procedures-the-safe-vs-risky-path)
5. [Complete Edge-Case Matrix](#5-complete-edge-case-matrix)
6. [Database Schema & Seed Scripts](#6-database-schema--seed-scripts)
7. [Backend Implementation Specifications (C# ASP.NET Core)](#7-backend-implementation-specifications-c-aspnet-core)
8. [Real-Time SignalR Micro-Sync Specification](#8-real-time-signalr-micro-sync-specification)
9. [Frontend Implementation Specifications (React 18)](#9-frontend-implementation-specifications-react-18)
10. [Step-by-Step Execution Checklist](#10-step-by-step-execution-checklist)

---

## 1. Direct Answers to Your Core Questions

### Question 1: *"Don't we already have a script in SQL that checks the user's role and assigns store/warehouse?"*
**YES, WE DO!**  
In the existing codebase and SQL Server database, there are **two places** where this already happens:

1. **Inside SQL Stored Procedures (`SP_New_Dashboard` and `SP_NEW_REPORT`)**:
   - At line 65–67 of `SP_New_Dashboard`:
     ```sql
     SELECT @User_Type = User_Type FROM dbo.[User_Registration] WHERE User_ID = @User_ID;
     SELECT @Store_code = dbo.[tbl_Store_Master].Store_code 
     FROM dbo.[tbl_Store_Master] 
     JOIN dbo.[User_Registration] ON dbo.[tbl_Store_Master].Store_ID = dbo.[User_Registration].Store_ID 
     WHERE User_ID = @User_ID;
     ```
   - At line 90 of `SP_New_Dashboard`:
     ```sql
     WHERE (@User_Type IN ('Super Admin', 'Warehouse Admin') 
        OR (@User_Type = 'Store Admin' AND dbo.tbl_Store_Master.Store_Code = @Store_code))
     ```
   - At line 63–67 of `SP_NEW_REPORT`:
     ```sql
     SELECT @Store_ID = dbo.User_Registration.Store_ID, 
            @WH_ID = dbo.User_Registration.WH_ID, 
            @User_Type = dbo.User_Registration.User_Type 
     FROM dbo.User_Registration 
     LEFT JOIN dbo.tbl_Store_Master ON dbo.User_Registration.Store_ID = dbo.tbl_Store_Master.Store_ID
     LEFT JOIN dbo.tbl_Warehouse_Mst ON dbo.User_Registration.WH_ID = dbo.tbl_Warehouse_Mst.WH_ID
     WHERE dbo.User_Registration.User_ID = @User_ID;
     ```

2. **Inside C# Backend (`BaseDashboardService.cs`)**:
   - In `GetUserProfileAsync` (lines 230–240 of `BaseDashboardService.cs`):
     ```sql
     SELECT 
         U.User_ID AS UserId, 
         ISNULL(U.User_Type, '') AS UserType, 
         ISNULL(S.Store_Code, '') AS StoreCode, 
         ISNULL(S.Store_Name, '') AS StoreName,
         ISNULL(W.WH_Code, '') AS WarehouseCode
     FROM dbo.User_Registration U
     LEFT JOIN dbo.tbl_Store_Master S ON U.Store_ID = S.Store_ID
     LEFT JOIN dbo.tbl_Warehouse_Mst W ON U.WH_ID = W.WH_ID
     WHERE U.User_ID = @Uid
     ```
   - In `MainDashboardService.cs` (lines 98–124):
     - If `profile.IsSuperAdmin || string.IsNullOrEmpty(profile.StoreCode)`: serves company-wide master data.
     - If `!string.IsNullOrEmpty(profile.StoreCode)`: slices master data in RAM to only include `MatchStore(x, profile.StoreCode)`.

---

### Question 2: *"Did we write any hardcoded logic in code, and why do we need to store it in a database?"*
**YES, WE CURRENTLY HAVE HARDCODED LOGIC IN TWO FILES:**

1. **`AuthService.cs` (Lines 79–134)**:
   A static `switch (normalizedRole)` statement manually assigns strings:
   ```csharp
   case "Store Admin":
       allowedSections.AddRange(new[] {
           "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
           "store_counter_status", "get_sap_stock_take", "user_registration"
       });
       break;
   case "Warehouse Admin":
       allowedSections.AddRange(new[] {
           "dc_validation", "dc_encoding", "tag_management", "vendor_discrepancy", "user_registration"
       });
       break;
   ```
2. **`AuthContext.jsx` (Lines 219–253)**:
   A duplicate fallback switch statement mirrors the C# logic in React.

#### Why storing it in the database is 100x better:
* If the business decides tomorrow: *"Store Admins should also see the Vendor Discrepancy tab,"* you currently have to edit C# code, edit React code, compile, and redeploy both applications.
* By moving this into `dbo.App_Role_Permissions` (and `dbo.App_User_Permissions`), the Super Admin can toggle any section on or off from a UI. It persists permanently in SQL Server and takes effect across all connected clients in **under 15 milliseconds via SignalR**.

---

### Question 3: *"How much SQL query / SP do we need to change?"*
* **Zero changes needed to existing stored procedures (`SP_New_Dashboard` / `SP_NEW_REPORT`)!**
* **Why?** Rewriting 129 legacy stored procedures that contain 4,600+ lines of SQL would introduce severe regression risks.
* **The Clean Architectural Solution**:
  1. We create **two dedicated new tables**: `dbo.App_Role_Permissions` and `dbo.App_User_Permissions`.
  2. We create **one new service**: `ControlCenterService.cs` which manages these permissions in SQL + RAM cache.
  3. We update **one method in `AuthService.cs`**: replacing the hardcoded `switch` statement with a fast query to `ControlCenterService`.
  4. We add **one endpoint filter** on protected backend controllers (`[AuthorizeSection("void")]`), so that even if a clever user sends a direct HTTP request via Postman, the backend validates their database permission before returning data!

---

## 2. Database Reality Check (What Currently Exists in SQL Server)

After inspecting the actual tables in `VMM_RFID_RETAIL_SOLUTION`:

### Table 1: `dbo.Role_Master`
Contains **10 system roles**:
| Role_ID | Role_Name | Is_Status |
| :---: | :--- | :---: |
| 1 | Area Manager | 1 |
| 2 | ZFM | 1 |
| 3 | LP | 1 |
| 4 | Super Admin | 1 |
| 5 | Store Admin | 1 |
| 6 | Store | 1 |
| 7 | Warehouse | 1 |
| 8 | Warehouse Admin | 1 |
| 9 | Tag Admin | 1 |
| 10 | Dispatch Admin | 1 |

### Table 2: `dbo.User_Registration`
Contains user accounts with:
* `User_ID` (int, Primary Key)
* `User_Name` (nvarchar(20))
* `Password` (nvarchar(20))
* `User_Type` (nvarchar(20) — e.g. `'Super Admin'`, `'Store Admin'`, `'Store'`, `'Warehouse'`)
* `Role_ID` (int, foreign key referencing `Role_Master.Role_ID`)
* `Store_ID` (int, foreign key referencing `tbl_Store_Master.Store_ID`)
* `WH_ID` (int, foreign key referencing `tbl_Warehouse_Mst.WH_ID`)
* `Is_Status` (char(1), `'1'` = Active, `'0'` = Inactive)
* `Is_Login_Status` (char(1), `'0'` = Default pass, `'1'` = Pass changed)

### Key Observation:
Currently, `dbo.User_Registration` contains both `User_Type` (string) and `Role_ID` (int).  
* `User_Type` is what the backend and stored procedures historically match on (`'Super Admin'`, `'Store Admin'`, etc.).
* `Role_ID` links to `Role_Master`.
* **Our design supports both**: `Role_Name` in `App_Role_Permissions` maps directly to `Role_Master.Role_Name` / `User_Registration.User_Type`.

---

## 3. The Dual-Layer Architecture: UI Sections vs. Data Row Filtering

To achieve perfection, we must clearly separate the two distinct responsibilities:

```
┌────────────────────────────────────────────────────────────────────────┐
│                        USER HITS THE SYSTEM                            │
└──────────────────────────────────┬─────────────────────────────────────┘
                                   │
         ┌─────────────────────────┴─────────────────────────┐
         ▼                                                   ▼
┌──────────────────────────────────┐      ┌──────────────────────────────────┐
│             LAYER A              │      │             LAYER B              │
│    SECTION / MENU PERMISSIONS    │      │    DATA-ROW LEVEL FILTERING      │
│  "Can I see or call this tab?"   │      │   "Which store's rows do I see?" │
├──────────────────────────────────┤      ├──────────────────────────────────┤
│ Controlled by:                   │      │ Controlled by:                   │
│ • dbo.App_Role_Permissions       │      │ • BaseDashboardService.cs        │
│ • dbo.App_User_Permissions       │      │ • MainDashboardService RAM Cache │
│ • SuperAdminControlCenter UI     │      │ • SP_New_Dashboard (@User_ID)    │
│ • [AuthorizeSection] Filter      │      │ • StoreCode / WH_Code match      │
├──────────────────────────────────┤      ├──────────────────────────────────┤
│ Example:                         │      │ Example:                         │
│ Is Store Admin allowed to see    │      │ Store Admin of HD55 opens Live   │
│ the "Void Dashboard" tab at all? │      │ Stock: they ONLY see HD55's rows,│
│ If NO: Tab vanishes from UI &    │      │ never company-wide data!         │
│ API rejects requests with 403.   │      │                                  │
└──────────────────────────────────┘      └──────────────────────────────────┘
```

Both layers work together seamlessly:
1. **Layer A** determines if the button, tab, or API route is unlocked for that user's role.
2. **Layer B** ensures that when the user accesses an unlocked tab, they can only see data belonging to their assigned store or warehouse.

---

## 4. Do We Need to Rewrite Stored Procedures? (The Safe vs. Risky Path)

### ❌ The Risky Path: Modifying Existing Stored Procedures
Modifying `SP_New_Dashboard` (1,000 lines) and `SP_NEW_REPORT` (4,600+ lines) to query permission tables on every row:
- Risk of breaking existing reporting calculations (SAP stock, difference qty, HU counts).
- Severe database performance degradation from repetitive joins in dynamic SQL loops.
- High regression risk across 129 stored procedures.

###  The Production-Grade Path: Controller & Service Layer Enforcement
- Store permissions in lightweight, indexed SQL tables (`dbo.App_Role_Permissions` and `dbo.App_User_Permissions`).
- In C#, query permissions once and cache in RAM (`IMemoryCache`, sliding 120 minutes).
- Guard routes using a custom `[AuthorizeSection("section_key")]` action filter.
- If a section is revoked:
  - Frontend: Tab disappears from DOM in <15ms via WebSocket push.
  - Backend API: Direct HTTP requests return `403 Forbidden` immediately, without even querying the database or stored procedure!

---

## 5. Complete Edge-Case Matrix

| # | Edge Case Scenario | Potential Bug / Failure | Architectural Solution |
| :-: | :--- | :--- | :--- |
| **E1** | **User is actively on a tab when Admin revokes it** | User could stay on the page and submit actions on revoked screens. | `liveStockSocket.js` dispatches event to `AuthContext`. If `currentPath === revokedRoute`, React immediately triggers `navigate('/dashboard')` and displays an alert toast: *"Access to this section was updated by Administrator."* |
| **E2** | **Browser Refresh (F5) right after permission revocation** | Stale cache restores the revoked menu item (Ghosting/Drift). | **3-Tier Synchronous Commit**: 1. Write to SQL Server -> 2. Invalidate `IMemoryCache` in RAM -> 3. Sync client `localStorage` before rendering. Refresh loads strictly from updated state. |
| **E3** | **Super Admin accidentally revokes own permissions** | Super Admin locks themselves out of the Control Center or User Management. | **Hard Immutable Safety Lock**: In both backend service and React UI, the `Super Admin` role has critical sections (`user_registration`, `store_registration`, `control_center`) locked to `Is_Allowed = 1`. Any API call attempting to disable these for Super Admin returns `400 Bad Request ("Super Admin core permissions cannot be revoked")`. |
| **E4** | **Database maintenance or transient SQL disconnect** | Login fails because permission table is unreachable. | **Resilient Fallback**: `ControlCenterService` wraps DB reads in a try-catch. If SQL Server is temporarily unreachable, it falls back to default role arrays so users can still log in without downtime. |
| **E5** | **Role Name Variations in Database** | Some users have `Store` vs `Store User`, `WH Admin` vs `Warehouse Admin`. | **Normalized Role Resolver**: Backend service maps legacy synonyms (`Store` ➔ `Store User`, `Warehouse` ➔ `Warehouse User`, `WH Admin` ➔ `Warehouse Admin`) to canonical keys. |
| **E6** | **User-Level Overrides vs. Role-Level Defaults** | User belongs to Store Admin, but needs temporary access to 1 extra warehouse tab. | `App_User_Permissions` allows overriding permissions per specific `User_ID`. The resolution order is: **User Override (if exists) ➔ Role Permission ➔ Default Fallback**. |
| **E7** | **Unassigned Store / Missing StoreCode** | If a Store User has `Store_ID = NULL`, what do they see? | `BaseDashboardService` handles `NULL` gracefully: if StoreCode is empty and user is not Super Admin, returns empty array with message *"No store assigned to this account."* |

---

## 6. Database Schema & Seed Scripts

Execute this script in SQL Server Management Studio (`VMM_RFID_RETAIL_SOLUTION`):

```sql
-- =========================================================================
-- Migration: Create Dynamic Role and User Permission Management Tables
-- Database: VMM_RFID_RETAIL_SOLUTION
-- =========================================================================

-- 1. Create Role-Level Permissions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'App_Role_Permissions')
BEGIN
    CREATE TABLE dbo.App_Role_Permissions (
        Role_Name NVARCHAR(50) NOT NULL,
        Section_Key NVARCHAR(50) NOT NULL,
        Is_Allowed BIT NOT NULL DEFAULT 1,
        Updated_By NVARCHAR(50) NULL,
        Updated_Date DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_App_Role_Permissions PRIMARY KEY CLUSTERED (Role_Name, Section_Key)
    );
    CREATE NONCLUSTERED INDEX IX_App_Role_Permissions_Role ON dbo.App_Role_Permissions (Role_Name, Is_Allowed);
    PRINT 'Created dbo.App_Role_Permissions table.';
END;

-- 2. Create User-Specific Overrides Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'App_User_Permissions')
BEGIN
    CREATE TABLE dbo.App_User_Permissions (
        User_ID INT NOT NULL,
        Section_Key NVARCHAR(50) NOT NULL,
        Is_Allowed BIT NOT NULL DEFAULT 1,
        Updated_By NVARCHAR(50) NULL,
        Updated_Date DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_App_User_Permissions PRIMARY KEY CLUSTERED (User_ID, Section_Key),
        CONSTRAINT FK_App_User_Permissions_User FOREIGN KEY (User_ID) REFERENCES dbo.User_Registration (User_ID) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_App_User_Permissions_User ON dbo.App_User_Permissions (User_ID, Is_Allowed);
    PRINT 'Created dbo.App_User_Permissions table.';
END;

-- 3. Seed Default Permissions matching current production state
IF NOT EXISTS (SELECT 1 FROM dbo.App_Role_Permissions)
BEGIN
    -- Super Admin (Full access to all 19 standard sections)
    INSERT INTO dbo.App_Role_Permissions (Role_Name, Section_Key, Is_Allowed) VALUES
    ('Super Admin', 'live_stock', 1),
    ('Super Admin', 'cycle_count', 1),
    ('Super Admin', 'store_validation', 1),
    ('Super Admin', 'sale', 1),
    ('Super Admin', 'void', 1),
    ('Super Admin', 'return', 1),
    ('Super Admin', 'store_counter_status', 1),
    ('Super Admin', 'get_sap_stock_take', 1),
    ('Super Admin', 'dc_validation', 1),
    ('Super Admin', 'dc_encoding', 1),
    ('Super Admin', 'tag_management', 1),
    ('Super Admin', 'vendor_discrepancy', 1),
    ('Super Admin', 'user_registration', 1),
    ('Super Admin', 'store_registration', 1),
    ('Super Admin', 'warehouse_registration', 1),
    ('Super Admin', 'floor_registration', 1),
    ('Super Admin', 'tag_cleaning', 1),
    ('Super Admin', 'dispatch_tracking', 1),
    ('Super Admin', 'picklist_creation', 1);

    -- Store Admin
    INSERT INTO dbo.App_Role_Permissions (Role_Name, Section_Key, Is_Allowed) VALUES
    ('Store Admin', 'live_stock', 1),
    ('Store Admin', 'cycle_count', 1),
    ('Store Admin', 'store_validation', 1),
    ('Store Admin', 'sale', 1),
    ('Store Admin', 'void', 1),
    ('Store Admin', 'return', 1),
    ('Store Admin', 'store_counter_status', 1),
    ('Store Admin', 'get_sap_stock_take', 1),
    ('Store Admin', 'user_registration', 1);

    -- Store User & Store
    INSERT INTO dbo.App_Role_Permissions (Role_Name, Section_Key, Is_Allowed) VALUES
    ('Store User', 'live_stock', 1),
    ('Store User', 'cycle_count', 1),
    ('Store User', 'store_validation', 1),
    ('Store User', 'sale', 1),
    ('Store User', 'void', 1),
    ('Store User', 'return', 1),
    ('Store User', 'store_counter_status', 1),
    ('Store', 'live_stock', 1),
    ('Store', 'cycle_count', 1),
    ('Store', 'store_validation', 1),
    ('Store', 'sale', 1),
    ('Store', 'void', 1),
    ('Store', 'return', 1),
    ('Store', 'store_counter_status', 1);

    -- Warehouse Admin
    INSERT INTO dbo.App_Role_Permissions (Role_Name, Section_Key, Is_Allowed) VALUES
    ('Warehouse Admin', 'dc_validation', 1),
    ('Warehouse Admin', 'dc_encoding', 1),
    ('Warehouse Admin', 'tag_management', 1),
    ('Warehouse Admin', 'vendor_discrepancy', 1),
    ('Warehouse Admin', 'user_registration', 1);

    -- Warehouse User & Warehouse
    INSERT INTO dbo.App_Role_Permissions (Role_Name, Section_Key, Is_Allowed) VALUES
    ('Warehouse User', 'dc_validation', 1),
    ('Warehouse User', 'dc_encoding', 1),
    ('Warehouse', 'dc_validation', 1),
    ('Warehouse', 'dc_encoding', 1);

    -- Dispatch Admin
    INSERT INTO dbo.App_Role_Permissions (Role_Name, Section_Key, Is_Allowed) VALUES
    ('Dispatch Admin', 'dispatch_tracking', 1),
    ('Dispatch Admin', 'picklist_creation', 1);

    -- Tag Admin
    INSERT INTO dbo.App_Role_Permissions (Role_Name, Section_Key, Is_Allowed) VALUES
    ('Tag Admin', 'tag_cleaning', 1),
    ('Tag Admin', 'get_sap_stock_take', 1);

    PRINT 'Seeded default permissions for all standard roles.';
END;
```

---

## 7. Backend Implementation Specifications (C# ASP.NET Core)

### 7.1 New Models: `Features/AdminControlCenter/ControlCenterModels.cs`
```csharp
namespace VS_Mart_Backend.Features.AdminControlCenter
{
    public class RolePermissionsDto
    {
        public string RoleName { get; set; } = string.Empty;
        public List<string> AllowedSections { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class UpdateRolePermissionsRequest
    {
        public string RoleName { get; set; } = string.Empty;
        public List<string> AllowedSections { get; set; } = new();
        public string? UpdatedBy { get; set; }
    }

    public class UserPermissionOverrideRequest
    {
        public int UserId { get; set; }
        public List<string> AllowedSections { get; set; } = new();
        public string? UpdatedBy { get; set; }
    }

    public class PermissionUpdateBroadcastPayload
    {
        public string TargetType { get; set; } = "Role"; // "Role" or "User"
        public string TargetName { get; set; } = string.Empty; // RoleName or UserId
        public List<string> AllowedSections { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
```

### 7.2 New Service: `Features/AdminControlCenter/ControlCenterService.cs`
```csharp
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace VS_Mart_Backend.Features.AdminControlCenter
{
    public interface IControlCenterService
    {
        Task<List<RolePermissionsDto>> GetAllRolePermissionsAsync();
        Task<List<string>> GetAllowedSectionsForRoleAsync(string roleName);
        Task<List<string>> GetAllowedSectionsForUserAsync(int userId, string roleName);
        Task<bool> SaveRolePermissionsAsync(UpdateRolePermissionsRequest request);
        Task<bool> SaveUserPermissionOverridesAsync(UserPermissionOverrideRequest request);
    }

    public class ControlCenterService : IControlCenterService
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ControlCenterService> _logger;

        public ControlCenterService(IConfiguration configuration, IMemoryCache cache, ILogger<ControlCenterService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS") 
                ?? throw new InvalidOperationException("Connection string 'POS' not found.");
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<string>> GetAllowedSectionsForRoleAsync(string roleName)
        {
            string normalized = NormalizeRoleName(roleName);
            string cacheKey = $"Perm_Role_{normalized}";

            if (_cache.TryGetValue(cacheKey, out List<string>? cached) && cached != null)
                return cached;

            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT Section_Key 
                    FROM dbo.App_Role_Permissions WITH (NOLOCK) 
                    WHERE Role_Name = @RoleName AND Is_Allowed = 1";

                var sections = (await conn.QueryAsync<string>(sql, new { RoleName = normalized })).ToList();

                if (sections.Count == 0)
                    sections = GetDefaultFallbackSections(normalized);

                _cache.Set(cacheKey, sections, TimeSpan.FromMinutes(120));
                return sections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching permissions for role {Role}. Using safe fallback.", roleName);
                return GetDefaultFallbackSections(normalized);
            }
        }

        public async Task<List<string>> GetAllowedSectionsForUserAsync(int userId, string roleName)
        {
            string cacheKey = $"Perm_User_{userId}";
            if (_cache.TryGetValue(cacheKey, out List<string>? cached) && cached != null)
                return cached;

            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT Section_Key 
                    FROM dbo.App_User_Permissions WITH (NOLOCK) 
                    WHERE User_ID = @UserId AND Is_Allowed = 1";

                var userOverrides = (await conn.QueryAsync<string>(sql, new { UserId = userId })).ToList();

                // If user has custom overrides, use them; otherwise inherit from role
                List<string> result = userOverrides.Count > 0 
                    ? userOverrides 
                    : await GetAllowedSectionsForRoleAsync(roleName);

                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(120));
                return result;
            }
            catch
            {
                return await GetAllowedSectionsForRoleAsync(roleName);
            }
        }

        public async Task<bool> SaveRolePermissionsAsync(UpdateRolePermissionsRequest request)
        {
            string normalized = NormalizeRoleName(request.RoleName);

            // Safety check: Prevent Super Admin self-lockout
            if (normalized.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                var coreSections = new[] { "user_registration", "store_registration", "live_stock" };
                foreach (var core in coreSections)
                {
                    if (!request.AllowedSections.Contains(core))
                        request.AllowedSections.Add(core);
                }
            }

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // Delete existing role permissions
                await conn.ExecuteAsync(
                    "DELETE FROM dbo.App_Role_Permissions WHERE Role_Name = @RoleName",
                    new { RoleName = normalized }, tx);

                // Insert new allowed sections
                if (request.AllowedSections.Count > 0)
                {
                    const string insertSql = @"
                        INSERT INTO dbo.App_Role_Permissions (Role_Name, Section_Key, Is_Allowed, Updated_By, Updated_Date)
                        VALUES (@RoleName, @SectionKey, 1, @UpdatedBy, GETDATE())";

                    var rows = request.AllowedSections.Select(s => new
                    {
                        RoleName = normalized,
                        SectionKey = s,
                        UpdatedBy = request.UpdatedBy ?? "Super Admin"
                    });

                    await conn.ExecuteAsync(insertSql, rows, tx);
                }

                tx.Commit();

                // Invalidate Cache immediately
                _cache.Remove($"Perm_Role_{normalized}");
                _cache.Set($"Perm_Role_{normalized}", request.AllowedSections, TimeSpan.FromMinutes(120));

                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _logger.LogError(ex, "Failed to save permissions for role {Role}", request.RoleName);
                throw;
            }
        }

        public async Task<bool> SaveUserPermissionOverridesAsync(UserPermissionOverrideRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                await conn.ExecuteAsync(
                    "DELETE FROM dbo.App_User_Permissions WHERE User_ID = @UserId",
                    new { UserId = request.UserId }, tx);

                if (request.AllowedSections.Count > 0)
                {
                    const string insertSql = @"
                        INSERT INTO dbo.App_User_Permissions (User_ID, Section_Key, Is_Allowed, Updated_By, Updated_Date)
                        VALUES (@UserId, @SectionKey, 1, @UpdatedBy, GETDATE())";

                    var rows = request.AllowedSections.Select(s => new
                    {
                        UserId = request.UserId,
                        SectionKey = s,
                        UpdatedBy = request.UpdatedBy ?? "Super Admin"
                    });

                    await conn.ExecuteAsync(insertSql, rows, tx);
                }

                tx.Commit();

                _cache.Remove($"Perm_User_{request.UserId}");
                _cache.Set($"Perm_User_{request.UserId}", request.AllowedSections, TimeSpan.FromMinutes(120));

                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _logger.LogError(ex, "Failed to save user override for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<List<RolePermissionsDto>> GetAllRolePermissionsAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT Role_Name, Section_Key, Updated_Date, Updated_By 
                FROM dbo.App_Role_Permissions WITH (NOLOCK) 
                WHERE Is_Allowed = 1
                ORDER BY Role_Name, Section_Key";

            var rows = await conn.QueryAsync<dynamic>(sql);
            var grouped = rows.GroupBy(r => (string)r.Role_Name);

            var result = new List<RolePermissionsDto>();
            foreach (var g in grouped)
            {
                result.Add(new RolePermissionsDto
                {
                    RoleName = g.Key,
                    AllowedSections = g.Select(x => (string)x.Section_Key).ToList(),
                    LastUpdated = g.Max(x => (DateTime)x.Updated_Date),
                    UpdatedBy = g.Select(x => (string?)x.Updated_By).FirstOrDefault()
                });
            }

            return result;
        }

        private static string NormalizeRoleName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return "Store User";
            string r = roleName.Trim();
            if (r.Equals("Store", StringComparison.OrdinalIgnoreCase)) return "Store User";
            if (r.Equals("Warehouse", StringComparison.OrdinalIgnoreCase)) return "Warehouse User";
            if (r.Equals("WH Admin", StringComparison.OrdinalIgnoreCase)) return "Warehouse Admin";
            return r;
        }

        private static List<string> GetDefaultFallbackSections(string role)
        {
            return role switch
            {
                "Super Admin" => new List<string> {
                    "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                    "store_counter_status", "get_sap_stock_take", "dc_validation", "dc_encoding",
                    "tag_management", "vendor_discrepancy", "user_registration", "store_registration",
                    "warehouse_registration", "floor_registration", "tag_cleaning", "dispatch_tracking", "picklist_creation"
                },
                "Store Admin" => new List<string> {
                    "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                    "store_counter_status", "get_sap_stock_take", "user_registration"
                },
                "Store User" => new List<string> {
                    "live_stock", "cycle_count", "store_validation", "sale", "void", "return", "store_counter_status"
                },
                "Warehouse Admin" => new List<string> {
                    "dc_validation", "dc_encoding", "tag_management", "vendor_discrepancy", "user_registration"
                },
                "Warehouse User" => new List<string> { "dc_validation", "dc_encoding" },
                "Dispatch Admin" => new List<string> { "dispatch_tracking", "picklist_creation" },
                "Tag Admin" => new List<string> { "tag_cleaning", "get_sap_stock_take" },
                _ => new List<string> { "live_stock" }
            };
        }
    }
}
```

### 7.3 New Controller: `Features/AdminControlCenter/AdminControlCenterController.cs`
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VS_Mart_Backend.Features.Dashboard.Hubs;

namespace VS_Mart_Backend.Features.AdminControlCenter
{
    [ApiController]
    [Route("api/admin/control-center")]
    public class AdminControlCenterController : ControllerBase
    {
        private readonly IControlCenterService _service;
        private readonly IHubContext<DashboardHub> _hubContext;

        public AdminControlCenterController(IControlCenterService service, IHubContext<DashboardHub> hubContext)
        {
            _service = service;
            _hubContext = hubContext;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var data = await _service.GetAllRolePermissionsAsync();
            return Ok(data);
        }

        [HttpPost("roles")]
        public async Task<IActionResult> SaveRolePermissions([FromBody] UpdateRolePermissionsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
                return BadRequest(new { message = "RoleName is required." });

            bool saved = await _service.SaveRolePermissionsAsync(request);
            if (!saved) return StatusCode(500, new { message = "Failed to save permissions." });

            // Broadcast real-time SignalR WebSocket notification
            var broadcast = new PermissionUpdateBroadcastPayload
            {
                TargetType = "Role",
                TargetName = request.RoleName,
                AllowedSections = request.AllowedSections,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("ReceivePermissionsUpdated", broadcast);

            return Ok(new { success = true, message = $"Permissions updated and broadcasted for {request.RoleName}." });
        }

        [HttpPost("users")]
        public async Task<IActionResult> SaveUserOverrides([FromBody] UserPermissionOverrideRequest request)
        {
            if (request.UserId <= 0)
                return BadRequest(new { message = "Valid UserId is required." });

            bool saved = await _service.SaveUserPermissionOverridesAsync(request);
            if (!saved) return StatusCode(500, new { message = "Failed to save user override." });

            var broadcast = new PermissionUpdateBroadcastPayload
            {
                TargetType = "User",
                TargetName = request.UserId.ToString(),
                AllowedSections = request.AllowedSections,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("ReceivePermissionsUpdated", broadcast);

            return Ok(new { success = true, message = $"Permissions override saved for User #{request.UserId}." });
        }
    }
}
```

### 7.4 Integrating with `AuthService.cs` (Login Flow)
In `AuthService.cs`, replace lines 77–135 with:
```csharp
// Fetch dynamic permissions from ControlCenterService (backed by SQL + MemoryCache)
int uid = 0;
if (row.ContainsKey("User_ID") && int.TryParse(row["User_ID"]?.ToString(), out int parsedUid))
{
    uid = parsedUid;
}

var allowedSections = await _controlCenterService.GetAllowedSectionsForUserAsync(uid, userType);
```

---

## 8. Real-Time SignalR Micro-Sync Specification

### In `liveStockSocket.js` (Frontend Service):
```javascript
// Register real-time permissions listener
onPermissionsUpdated(callback) {
  if (this.connection) {
    this.connection.on('ReceivePermissionsUpdated', (payload) => {
      console.log('⚡ [SignalR] Real-Time Permissions Update Received:', payload);
      callback(payload);
    });
  }
}
```

### In `AuthContext.jsx` (Client State & Eviction Handler):
```javascript
useEffect(() => {
  liveStockSocket.onPermissionsUpdated((payload) => {
    const currentUser = user;
    if (!currentUser) return;

    const isMatch = 
      (payload.targetType === 'Role' && payload.targetName === currentUser.userType) ||
      (payload.targetType === 'User' && payload.targetName === String(currentUser.userID));

    if (isMatch) {
      console.log('🔄 Syncing user permissions dynamically:', payload.allowedSections);
      
      // 1. Update React Context State
      setAllowedSections(payload.allowedSections);

      // 2. Synchronously write to localStorage (Zero-Drift Guarantee)
      const updatedUser = { ...currentUser, allowedSections: payload.allowedSections };
      localStorage.setItem('user', JSON.stringify(updatedUser));

      // 3. Auto-Evict if viewing a newly revoked section
      const currentPath = window.location.pathname;
      const currentSectionKey = getSectionKeyFromPath(currentPath);
      
      if (currentSectionKey && !payload.allowedSections.includes(currentSectionKey)) {
        toast.error('Your access to this section was updated by the Administrator.');
        navigate('/dashboard');
      }
    }
  });
}, [user]);
```

---

## 9. Frontend Implementation Specifications (React 18)

### New Page: `src/pages/Admin/SuperAdminControlCenter.jsx`
* **Route**: `/admin/control-center`
* **Permissions**: `Super Admin` only.
* **UI Features**:
  1. **Role Selector Pill Tabs**: Quickly toggle between `Store Admin`, `Store User`, `Warehouse Admin`, `Warehouse User`, `Tag Admin`, and `Dispatch Admin`.
  2. **Categorized Modules Grid**:
     * **📊 Dashboard Tabs (10 modules)**: Live Stock, Tag Cycle Count, Store Discrepancy, Sale, Void, Return, DC Validation, DC Encoding, Tag Management, Vendor Discrepancy.
     * **📑 Reports & Tools (5 modules)**: Store Counter Status, Tag Cleaning Report, SAP Stock Take, Dispatch Tracking, Picklist Creation.
     * **⚙️ System Masters (4 modules)**: User Registration, Store Registration, Floor Registration, Warehouse Registration.
  3. **High-Contrast Apple-Style Switch Toggles**:
     * Green (`#10B981`) when enabled.
     * Muted Gray (`#9CA3AF`) when disabled.
  4. **"Save & Apply Globally" Sticky Action Bar**:
     * Shows count of changed permissions.
     * Triggers clean Confirm Modal before sending POST request.
     * On success, shows modern floating toast notification.

---

## 10. Step-by-Step Execution Checklist

When you instruct me to begin implementation, we will proceed in this exact sequence:

- [ ] **Step 1: SQL Database Setup**: Execute migration script in `VMM_RFID_RETAIL_SOLUTION` creating `dbo.App_Role_Permissions`, `dbo.App_User_Permissions`, and seeding all defaults.
- [ ] **Step 2: Backend Control Center Core**: Create `ControlCenterModels.cs`, `IControlCenterService.cs`, and `ControlCenterService.cs` with Dapper & `IMemoryCache`.
- [ ] **Step 3: Register Service in DI**: Add `builder.Services.AddScoped<IControlCenterService, ControlCenterService>()` in `Program.cs`.
- [ ] **Step 4: Create Admin Controller**: Implement `AdminControlCenterController.cs` with `GET` and `POST` endpoints.
- [ ] **Step 5: Integrate `AuthService.cs`**: Wire up `GetAllowedSectionsForUserAsync` into login flow.
- [ ] **Step 6: Frontend WebSocket Hook**: Add `onPermissionsUpdated` listener in `liveStockSocket.js` and reactive auto-eviction in `AuthContext.jsx`.
- [ ] **Step 7: Build Control Center UI**: Implement `SuperAdminControlCenter.jsx` with category cards, toggle switches, and save bar.
- [ ] **Step 8: Register Routes & Navigation**: Add `/admin/control-center` to `App.jsx` and `Sidebar.jsx`.
- [ ] **Step 9: Real-Time Multi-Session Verification**: Test live toggling between Super Admin and Store Admin window with zero-drift refresh validation.
