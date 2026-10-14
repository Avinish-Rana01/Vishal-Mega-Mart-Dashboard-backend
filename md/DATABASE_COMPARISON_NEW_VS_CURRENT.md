# 📊 Database Comparison Report: Current Active DB vs. Fresh Backup

> **Active Working Database**: `VMM_RFID_RETAIL_SOLUTION`  
> **Freshly Restored Backup Database**: `VMM_RFID_RETAIL_SOLUTION_NEW` (from `C:\DB backuo\VMM_RFID_RETAIL_SOLUTION_14102026_233.bak`)  
> **Analysis Date**: October 14, 2026  
> **Environment**: Microsoft SQL Server 2022 (`MSSQL17.MSSQLSERVER`)

---

## 1. Executive Summary

The fresh 11.6 GB backup was successfully restored side-by-side as `VMM_RFID_RETAIL_SOLUTION_NEW` in 6 seconds without impacting running services.

A checksum and metadata audit of all database objects revealed:
1. **Stored Procedures**:
   - Out of 59+ stored procedures, **only 3 procedures have code differences** (`SP_Master`, `SP_New_Dashboard`, `SP_NEW_REPORT`).
   - **1 new procedure** was introduced in the backup: `SP_WH_TagInward_InsFromEncDtl`.
   - **Critical Warning on `SP_Master`**: The fresh backup contains an old version of `SP_Master` from **May 26, 2026** (14,275 characters with 23 parameters). The current active database has the modernized version (33,439 characters with 33 parameters). If switched without patching, **User Registration and Store Registration will immediately fail with SQL Error 8144 (Too many arguments specified)**.
2. **Tables & Schema Drift**:
   - The backup is missing **3 tables** created in the current working database: `tbl_State_Mst`, `tbl_City_Mst`, and `tbl_Employee_Mst`.
   - The backup is missing **16 columns** across `tbl_Store_Master`, `User_Registration`, and `tbl_Store_Floor_Mst`.
3. **Data & Row Counts**:
   - Core tables (`Role_Master`, `tbl_Store_Master`, `tbl_Store_Floor_Mst`) have identical row counts.
   - `User_Registration`: 54 users in current vs. 53 users in the fresh backup (1 user added recently).

---

## 2. Stored Procedures Comparison Matrix

| Stored Procedure Name | Comparison Status | Current DB Version | Fresh Backup Version | Impact & Details |
| :--- | :---: | :---: | :---: | :--- |
| **`SP_Master`** | **CRITICAL DIFFERENCE** | **33,439 chars**<br>*(2026-10-14 14:54)* | **14,275 chars**<br>*(2026-05-26 23:12)* | **High**: Backup only accepts 23 parameters. Missing `State`, `City`, `Store_Manager`, `Area_Manager`, `ZFM`, `LP`, and `Email`. Missing dynamic `Role_Master` resolution and `IS_Status = '1'` fix. |
| **`SP_New_Dashboard`** | **MODIFIED** | **51,718 chars**<br>*(2026-10-06 15:09)* | **51,095 chars**<br>*(2026-09-25 13:52)* | **Medium**: Current has SignalR output parameters (`@QTY`, `@HU_VALIDATED_QTY`), cycle count diff metrics, and delta sync logic. |
| **`SP_NEW_REPORT`** | **MODIFIED** | **263,687 chars**<br>*(2026-10-14 11:49)* | **268,665 chars**<br>*(2026-10-07 17:53)* | **Medium**: Current contains lifetime full-date range exports (`DC_ENCODING_SUMMARY`) and `CYCLE_COUNT_REPORT_VIEW` row matching. |
| **`SP_WH_TagInward_InsFromEncDtl`** | **NEW IN BACKUP** | *Does not exist* | **516 chars**<br>*(2026-10-09 11:00)* | **Low (Safe)**: New warehouse tag inward helper procedure added in office DB. Retain this. |
| **All Other Procedures (56)** | **IDENTICAL (100%)** | Matches | Matches | Zero code differences. |

---

## 3. Schema Drift: Missing Tables & Columns in Fresh Backup

If we switch to `VMM_RFID_RETAIL_SOLUTION_NEW`, the following columns and tables MUST be added to prevent runtime exceptions:

### 3.1 Missing Tables
1. `dbo.tbl_State_Mst` (State directory master)
2. `dbo.tbl_City_Mst` (City directory master)
3. `dbo.tbl_Employee_Mst` (Employee master directory - 59 rows in active DB)

### 3.2 Missing Columns in Existing Tables

#### A. `dbo.tbl_Store_Master` (Missing 13 columns)
- `State` (`NVARCHAR(100)`)
- `City` (`NVARCHAR(100)`)
- `Store_Manager` (`NVARCHAR(100)`)
- `Area_Manager` (`NVARCHAR(100)`)
- `ZFM` (`NVARCHAR(100)`)
- `LP` (`NVARCHAR(100)`)
- `State_ID`, `City_ID`, `SM_ID`, `AM_ID`, `ZFM_ID`, `LP_ID` (`INT`)
- `SM_MAIL_ID` (`NVARCHAR(300)`)

#### B. `dbo.User_Registration` (Missing 5 columns)
- `Email_ID` (`NVARCHAR(300)`)
- `Is_Email_Required` (`BIT DEFAULT 0`)
- `User_Email_ID` (`NVARCHAR(300)`)
- `Emp_ID` (`INT`)
- `Email_Required_Flag` (`BIT DEFAULT 0`)

#### C. `dbo.tbl_Store_Floor_Mst` (Missing 3 columns)
- `Is_Status` (`BIT DEFAULT 1`)
- `U_By` (`INT`)
- `U_Dt` (`DATETIME`)

---

## 4. Row Counts & Master Data Comparison

| Table Name | Current Working DB | Fresh Backup DB | Delta | Notes |
| :--- | :---: | :---: | :---: | :--- |
| `Role_Master` | **10** | **10** | 0 | Identical master roles |
| `tbl_Store_Master` | **6** | **6** | 0 | Identical store records |
| `tbl_Store_Floor_Mst` | **16** | **16** | 0 | Identical 16 floor records |
| `User_Registration` | **54** | **53** | +1 | 1 user provisioned recently in local DB |
| `tbl_Employee_Mst` | **59** | *Missing Table* | +59 | Reference table created locally |
| `tbl_State_Mst` | **2** | *Missing Table* | +2 | Reference table created locally |
| `tbl_City_Mst` | **3** | *Missing Table* | +3 | Reference table created locally |

---

## 5. Ready-to-Run Migration Script for the Fresh Backup

To upgrade `VMM_RFID_RETAIL_SOLUTION_NEW` so it contains all fixes, run the following SQL script:

```sql
USE [VMM_RFID_RETAIL_SOLUTION_NEW];
GO

-- 1. ADD MISSING COLUMNS TO tbl_Store_Master
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Store_Master') AND name = 'State')
    ALTER TABLE dbo.tbl_Store_Master ADD 
        [State] NVARCHAR(100) NULL, 
        [City] NVARCHAR(100) NULL,
        [Store_Manager] NVARCHAR(100) NULL, 
        [Area_Manager] NVARCHAR(100) NULL, 
        [ZFM] NVARCHAR(100) NULL, 
        [LP] NVARCHAR(100) NULL,
        [State_ID] INT NULL,
        [City_ID] INT NULL,
        [SM_ID] INT NULL,
        [AM_ID] INT NULL,
        [ZFM_ID] INT NULL,
        [LP_ID] INT NULL,
        [SM_MAIL_ID] NVARCHAR(300) NULL;
GO

-- 2. ADD MISSING COLUMNS TO User_Registration
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.User_Registration') AND name = 'Email_ID')
    ALTER TABLE dbo.User_Registration ADD 
        [Email_ID] NVARCHAR(100) NULL, 
        [Is_Email_Required] BIT NULL DEFAULT 0,
        [User_Email_ID] NVARCHAR(300) NULL,
        [Emp_ID] INT NULL,
        [Email_Required_Flag] BIT NULL DEFAULT 0;
GO

-- 3. ADD MISSING COLUMNS TO tbl_Store_Floor_Mst
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Store_Floor_Mst') AND name = 'Is_Status')
    ALTER TABLE dbo.tbl_Store_Floor_Mst ADD 
        [Is_Status] BIT DEFAULT 1, 
        [U_By] INT NULL, 
        [U_Dt] DATETIME NULL;
GO

-- 4. COPY MISSING MASTER TABLES FROM WORKING DATABASE
IF OBJECT_ID('dbo.tbl_State_Mst') IS NULL
    SELECT * INTO [VMM_RFID_RETAIL_SOLUTION_NEW].dbo.tbl_State_Mst 
    FROM [VMM_RFID_RETAIL_SOLUTION].dbo.tbl_State_Mst;
GO

IF OBJECT_ID('dbo.tbl_City_Mst') IS NULL
    SELECT * INTO [VMM_RFID_RETAIL_SOLUTION_NEW].dbo.tbl_City_Mst 
    FROM [VMM_RFID_RETAIL_SOLUTION].dbo.tbl_City_Mst;
GO

IF OBJECT_ID('dbo.tbl_Employee_Mst') IS NULL
    SELECT * INTO [VMM_RFID_RETAIL_SOLUTION_NEW].dbo.tbl_Employee_Mst 
    FROM [VMM_RFID_RETAIL_SOLUTION].dbo.tbl_Employee_Mst;
GO
```

---

## 6. Stored Procedures Synchronizer

Once the schema migration is applied, the 3 modernized stored procedures (`SP_Master`, `SP_New_Dashboard`, `SP_NEW_REPORT`) from `VMM_RFID_RETAIL_SOLUTION` should be scripted into `VMM_RFID_RETAIL_SOLUTION_NEW`.

