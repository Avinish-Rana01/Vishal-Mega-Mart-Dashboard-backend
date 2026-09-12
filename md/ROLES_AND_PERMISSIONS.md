# 🛡️ Database Roles, Users & RBAC Architecture

> Complete reference of all local database accounts, roles in `Role_Master`, and Row-Level Security (RLS) rules discovered from stored procedures.  
> **Database**: `VMM_RFID_RETAIL_SOLUTION`  
> **Source Tables**: `User_Registration`, `Role_Master`, `tbl_Store_Master`  
> **Core Procedures**: `SP_Master`, `SP_New_Dashboard`, `SP_NEW_REPORT`

---

## Table of Contents
1. [👥 Database Accounts Summary](#-database-accounts-summary)
2. [🔑 Key Accounts for Dashboard Access](#-key-accounts-for-dashboard-access)
3. [📋 Full User Directory (54 Accounts)](#-full-user-directory)
4. [🎭 Defined Roles in `Role_Master`](#-defined-roles-in-role_master)
5. [🔒 Row-Level Security & Store Filtering (SQL Engine)](#-row-level-security--store-filtering)
6. [🚀 Tomorrow's Implementation Blueprint (RBAC)](#-tomorrows-implementation-blueprint)

---

## 👥 Database Accounts Summary

* **Total Registered Users**: **54**
* **Default Password**: Almost all accounts in this database use plaintext **`123`** *(with only one exception: `HH151` uses `1233`)*.
* **Status**: All 54 accounts are marked `Is_Status = 1` (Active).

> [!NOTE]
> `AuthController` currently allows **Super Admin**, **Store Admin**, and **Manager** roles to log in, but contains hardcoded checks blocking users with `User_Type` of `"Store"` or `"Warehouse"`.

---

## 🔑 Key Accounts for Dashboard Access

### 👑 Super Admin (Unrestricted Access to All Stores & Pollers)
| User ID | Username | Password | Role / User Type | Notes |
|:---:|---|:---:|---|---|
| **26** | `hareesh` | `123` | Super Admin | Primary admin ID used by `LiveStockPollerService` & `CacheWarmerService` |
| **30** | `Admin` | `123` | Super Admin | System master account |
| **53** | `gagan` | `123` | Super Admin | Regional/Operations Super Admin |
| **54** | `kishan` | `123` | Super Admin | Regional/Operations Super Admin |
| **55** | `karthik` | `123` | Super Admin | Regional/Operations Super Admin |

---

### 🏪 Store Admins (Store-Level Admin Access)
| User ID | Username | Password | Role / User Type | Store ID |
|:---:|---|:---:|---|:---:|
| **1** | `HD55` | `123` | Store Admin | 1 |
| **29** | `HH15` | `123` | Store Admin | 5 |
| **36** | `6060677` | `123` | Store Admin | 5 |
| **39** | `6026710` | `123` | Store Admin | 6 |
| **40** | `HD44-UT2` | `123` | Store Admin | 6 |

---

### 📦 Warehouse Admins & Specialty Roles
| User ID | Username | Password | Role / User Type | Assigned Scope |
|:---:|---|:---:|---|---|
| **33** | `1001004` | `123` | Warehouse Admin | Central Distribution Hub |
| **42** | `REPORT` | `123` | Warehouse Admin | Reporting & Analytics |
| **43** | `WA1` | `123` | Warehouse Admin | Warehouse Admin Operations |
| **45** | `d_admin` | `123` | Dispatch Admin | Gate pass, Outward DC Loading |
| **48** | `t_admin` | `123` | Tag Admin | RFID Encoding & Lifecycle |
| **49** | `jitendra` | `123` | Area Manager | Regional stores assigned to `AM_ID` |
| **50** | `sandeep` | `123` | ZFM | Regional stores assigned to `ZFM_ID` |
| **51** | `pawan` | `123` | LP | Stores assigned to `LP_ID` |
| **52** | `neeraj` | `123` | Area Manager | Regional stores assigned to `AM_ID` |
| **56** | `yash` | `123` | ZFM | Regional stores assigned to `ZFM_ID` |

---

## 📋 Full User Directory

<details open>
<summary><b>Click to expand / collapse all 54 database accounts</b></summary>

| User ID | Username | Password | User Type | Store ID | Status |
|:---:|---|:---:|---|:---:|:---:|
| 1 | `HD55` | `123` | Store Admin | 1 | Active |
| 2 | `D1` | `123` | Store | 1 | Active |
| 3 | `D2` | `123` | Store | 1 | Active |
| 4 | `D3` | `123` | Store | 1 | Active |
| 6 | `D4` | `123` | Store | 1 | Active |
| 7 | `VMM1` | `123` | Warehouse | - | Active |
| 8 | `VMM2` | `123` | Warehouse | - | Active |
| 9 | `VMM3` | `123` | Warehouse | - | Active |
| 10 | `VMM4` | `123` | Warehouse | - | Active |
| 12 | `VMM5` | `123` | Warehouse | - | Active |
| 13 | `VMM6` | `123` | Warehouse | - | Active |
| 14 | `VMM7` | `123` | Warehouse | - | Active |
| 15 | `VMM8` | `123` | Warehouse | - | Active |
| 16 | `VMM9` | `123` | Warehouse | - | Active |
| 17 | `VMM10` | `123` | Warehouse | - | Active |
| 18 | `D5` | `123` | Store | 1 | Active |
| 19 | `3000080` | `123` | Store | 1 | Active |
| 20 | `6002360` | `123` | Store | 1 | Active |
| 21 | `6080993` | `123` | Store | 1 | Active |
| 22 | `6045531` | `123` | Store | 1 | Active |
| 23 | `6080215` | `123` | Store | 1 | Active |
| 24 | `6004123` | `123` | Store | 1 | Active |
| 26 | `hareesh` | `123` | Super Admin | - | Active |
| 27 | `VMM11` | `123` | Warehouse | - | Active |
| 28 | `VMM12` | `123` | Warehouse | - | Active |
| 29 | `HH15` | `123` | Store Admin | 5 | Active |
| 30 | `Admin` | `123` | Super Admin | - | Active |
| 31 | `D6` | `123` | Store | 5 | Active |
| 32 | `HD44` | `123` | Store | 6 | Active |
| 33 | `1001004` | `123` | Warehouse Admin | - | Active |
| 34 | `D7` | `123` | Store | 6 | Active |
| 35 | `6063816` | `123` | Store | 1 | Active |
| 36 | `6060677` | `123` | Store Admin | 5 | Active |
| 37 | `6113062` | `123` | Store | 5 | Active |
| 38 | `D8` | `123` | Store | 6 | Active |
| 39 | `6026710` | `123` | Store Admin | 6 | Active |
| 40 | `HD44-UT2` | `123` | Store Admin | 6 | Active |
| 41 | `D9` | `123` | Store | 5 | Active |
| 42 | `REPORT` | `123` | Warehouse Admin | - | Active |
| 43 | `WA1` | `123` | Warehouse Admin | - | Active |
| 44 | `PRADEEP` | `123` | Warehouse | - | Active |
| 45 | `d_admin` | `123` | Dispatch Admin | - | Active |
| 46 | `6028665` | `123` | Store | 5 | Active |
| 47 | `VMM13` | `123` | Warehouse | - | Active |
| 48 | `t_admin` | `123` | Tag Admin | - | Active |
| 49 | `jitendra` | `123` | Area Manager | - | Active |
| 50 | `sandeep` | `123` | ZFM | - | Active |
| 51 | `pawan` | `123` | LP | - | Active |
| 52 | `neeraj` | `123` | Area Manager | - | Active |
| 53 | `gagan` | `123` | Super Admin | - | Active |
| 54 | `kishan` | `123` | Super Admin | - | Active |
| 55 | `karthik` | `123` | Super Admin | - | Active |
| 56 | `yash` | `123` | ZFM | - | Active |
| 57 | `HH151` | `1233` | Store | 5 | Active |

</details>

---

## 🎭 Defined Roles in `Role_Master`

The database has a dedicated lookup table `[dbo].[Role_Master]` defining **10 roles**:

```sql
SELECT Role_ID, Role_Name FROM dbo.Role_Master WHERE Is_Status = '1';
```

| `Role_ID` | Role Name | Associated Employee Column in `tbl_Store_Master` |
|:---:|---|---|
| **1** | Area Manager | `AM_ID` |
| **2** | ZFM (Zonal Facility Manager) | `ZFM_ID` |
| **3** | LP (Loss Prevention) | `LP_ID` |
| **4** | Super Admin | *N/A (Bypasses all store filters)* |
| **5** | Store Admin | `SM_ID` |
| **6** | Store | *N/A (Floor / POS)* |
| **7** | Warehouse | *N/A (DC Floor)* |
| **8** | Warehouse Admin | *N/A (Bypasses all store filters)* |
| **9** | Tag Admin | *N/A (RFID Encoding)* |
| **10** | Dispatch Admin | *N/A (DC Outward Dispatches)* |

---

## 🔒 Row-Level Security & Store Filtering

### 1. Database-Level Security (Already Implemented in SQL!)

Both `SP_New_Dashboard` and `SP_NEW_REPORT` already have **native Row-Level Security**. Whenever the backend sends `@USER_ID`, the database dynamically resolves the user's role and applies store filtering:

```sql
-- Step 1: Resolve Role and Employee ID from User_Registration
SELECT @Role_id = UR.Role_ID, @EmpID = UR.Emp_ID 
FROM dbo.User_Registration UR 
WHERE UR.User_ID = @USER_ID AND UR.IS_Status = '1';

-- Step 2: Filter stores based on role hierarchy
SELECT S.Store_Code, S.Store_Name
FROM dbo.tbl_Store_Master S
WHERE S.Is_Status = '1'
  AND (
       @Role_id IN (4, 8)                             -- Super Admin & Warehouse Admin: See ALL stores across India
    OR (@Role_id = 5 AND S.SM_ID = @EmpID)            -- Store Admin: Only see their single assigned store
    OR (@Role_id = 1 AND S.AM_ID = @EmpID)            -- Area Manager: Only see stores in their area
    OR (@Role_id = 2 AND S.ZFM_ID = @EmpID)           -- ZFM: Only see stores in their zone
    OR (@Role_id = 3 AND S.LP_ID = @EmpID)            -- Loss Prevention: Only see stores under audit
  )
ORDER BY S.Store_Code;
```

### 2. Store Manager Mapping Columns in `tbl_Store_Master`

Every store in `tbl_Store_Master` has dedicated manager references:
* **`SM_ID`**: Store Manager ID
* **`AM_ID`**: Area Manager ID
* **`ZFM_ID`**: Zonal Facility Manager ID
* **`LP_ID`**: Loss Prevention Officer ID

> [!TIP]
> **No SQL rewrite is needed for store-level filtering!** As long as the backend passes the authenticated user's `@User_ID`, SQL Server automatically restricts rows to only the stores that user is authorized to see.

---

## 🚀 Tomorrow's Implementation Blueprint

### Step 1: JWT Authentication (`AuthController.cs` & `AuthService.cs`)
When `POST /api/auth/login` is called:
1. `SP_Master` with `@Status='SP_Login'` executes and returns:
   - `User_ID`, `User_Name`, `Role_ID`, `User_Type`, `Store_ID`, `Store_Code`
2. Generate and return a signed **JWT token** containing claims:
   ```json
   {
     "nameid": "26",
     "unique_name": "hareesh",
     "role": "Super Admin",
     "roleId": "4",
     "storeCode": "1001",
     "storeId": "1"
   }
   ```

### Step 2: Parameter Interception (Row-Level Security)
In the backend services (such as `MainDashboardService.cs`):
* Read `UserId`, `Role`, and `StoreCode` from `IHttpContextAccessor.HttpContext.User`.
* **If Role == "Super Admin"**: Allow any requested `storeCode` or empty (all stores).
* **If Role == "Store Admin"**: Force override `request.StoreCode = userStoreCode`.
* Pass the authenticated user's `UserId` to the stored procedures to let SQL's native RLS filter the dataset.

### Step 3: Frontend Menu / Tab Visibility (React)
Since there is no menu-rights table in SQL, page navigation in the React UI should be conditioned on `user.role`:
* **Super Admin**: Access to all tabs (Live Stock, Cycle Count, Void, Tag Management, DC Encoding, etc.).
* **Store Admin**: Live Stock, Cycle Count, Store Discrepancy.
* **Warehouse Admin**: DC Encoding, Tag Verification, Dispatch Loading.
