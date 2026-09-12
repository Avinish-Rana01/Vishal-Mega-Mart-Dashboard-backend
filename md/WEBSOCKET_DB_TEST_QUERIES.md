# ⚡ Real-Time WebSocket & Database Trigger Queries Guide

This guide contains the exact SQL queries, background poller mechanisms, and WebSocket event channels for testing real-time updates across all sections of the VMM Retail RFID Dashboard.

When any of these queries are executed in SQL Server (SSMS, sqlcmd, or PowerShell), the backend background pollers (`LiveStockPollerService` every **2s**, `DashboardSectionsPollerService` every **4s**) automatically compute in-memory delta snapshots and broadcast live SignalR WebSocket patches to all connected browser clients.

---

## 🧭 Overview of Real-Time WebSocket Channels

| # | Dashboard Section | Role Visibility | SignalR Event | Primary Table | Poller Cycle |
| :---: | :--- | :--- | :--- | :--- | :---: |
| **1** | **Live Stock** | Super Admin, Store Admin | `ReceiveLiveStockPatch` | `dbo.tbl_Encoding_Dtl` | 2 sec |
| **2** | **Store Validation** | Super Admin, Store Admin | `ReceiveStoreValidationPatch` | `dbo.tbl_GRC_DETAILS` | 4 sec |
| **3** | **Cycle Count** | Super Admin, Store Admin | `ReceiveCycleCountPatch` | `dbo.tbl_Cycle_count_Dtl` | 4 sec |
| **4** | **DC Validation (Parked)** | Super Admin, Warehouse Admin | `ReceiveDcValidationPatch` | `dbo.tbl_SAP_DC_Outward_Dtl` | 4 sec |
| **5** | **Vendor Discrepancy** | Super Admin, Warehouse Admin | `ReceiveVendorDiscrepancyPatch` | `dbo.tbl_WH_Val_Box_HU_Park_Dtl` | 4 sec |
| **6** | **Tag Management** | Super Admin, Warehouse Admin | `ReceiveTagManagementPatch` | `dbo.tbl_tagmanagement_history` | 4 sec |
| **7** | **DC Encoding (Hourly)** | Super Admin, Warehouse Admin | `ReceiveDcEncodingPatch` | `dbo.tbl_Encoding_Dtl` | 4 sec |

---

## 🌟 Master All-In-One Scripts (All Sections Combined)

For comprehensive end-to-end testing, use these master transactions to trigger and restore all 7 dashboard sections simultaneously.

### 🔴 1. Master Script: TRIGGER CHANGES (All Sections Combined)
Copy and execute this script in SSMS connected to `VMM_RFID_RETAIL_SOLUTION`. Within 2 to 4 seconds, every dashboard section will receive a live WebSocket delta patch with green/red ticker animations in the browser.

```sql
USE [VMM_RFID_RETAIL_SOLUTION];
GO
PRINT '=======================================================';
PRINT '  TRIGGERING REAL-TIME WEBSOCKET DELTAS (ALL SECTIONS) ';
PRINT '=======================================================';
BEGIN TRANSACTION;

-- 1. LIVE STOCK DASHBOARD (Simulate 5 tags checked out / sold from HD44)
UPDATE TOP (5) dbo.tbl_Encoding_Dtl 
SET Is_Status = '0' 
WHERE STORE_ID = 6 AND Is_Status = '1';
PRINT '>>> 1. Live Stock: 5 tags subtracted from Store HD44.';

-- 2. STORE VALIDATION (De-validate HU 4000827011 for Store HD55)
UPDATE dbo.tbl_GRC_DETAILS 
SET Is_Status = 0 
WHERE STORE_CODE = 'HD55' AND HU = '4000827011';
PRINT '>>> 2. Store Validation: HU 4000827011 marked as Unvalidated for HD55.';

-- 3. CYCLE COUNT (Add +5 to physical scanned quantity on active audit)
UPDATE TOP (1) dbo.tbl_Cycle_count_Dtl 
SET Scanned_Qty = Scanned_Qty + 5 
WHERE Ref_ID = '20260909024154' AND Site_Code = 'HD55';
PRINT '>>> 3. Cycle Count: +5 Scanned Qty added to audit Ref 20260909024154.';

-- 4. DC VALIDATION (Mark outward HU 1229042922 for Plant HD55 as Unprocessed)
UPDATE dbo.tbl_SAP_DC_Outward_Dtl 
SET Is_Complete_Status_Flg = '0' 
WHERE Reciving_Plant = 'HD55' AND HU_No = '1229042922';
PRINT '>>> 4. DC Validation: HU 1229042922 marked Unprocessed for HD55.';

-- 5. VENDOR HU DISCREPANCY (Simulate +10 scanned discrepancy items)
UPDATE TOP (1) dbo.tbl_WH_Val_Box_HU_Park_Dtl 
SET Park_Qty = Park_Qty + 10 
WHERE Park_No = 1 AND Article_Code = '1133019672009';
PRINT '>>> 5. Vendor Discrepancy: +10 Parked Qty added to Article 1133019672009.';

-- 6. TAG MANAGEMENT (Shift tag location from Store to Warehouse)
UPDATE dbo.tbl_tagmanagement_history 
SET TAG_LOCATION = 'Warehouse' 
WHERE TAG_ID = 'E2801170200014116AB40B64';
PRINT '>>> 6. Tag Management: Tag E2801170200014116AB40B64 moved to Warehouse.';

-- 7. DC ENCODING (Shift timestamp of record 2006662 into the current hour today)
UPDATE dbo.tbl_Encoding_Dtl 
SET Encode_Date = GETUTCDATE() 
WHERE Encode_Trans_ID = 2006662;
PRINT '>>> 7. DC Encoding: Trans 2006662 shifted to current UTC hour.';

COMMIT TRANSACTION;
PRINT '=======================================================';
PRINT '  ALL 7 SECTIONS MODIFIED SUCCESSFULLY!                ';
PRINT '  Watch your web browser for live SignalR updates.     ';
PRINT '=======================================================';
GO
```

### 🟢 2. Master Script: UNDO / RESTORE (All Sections Combined)
Execute this script to revert all 7 sections back to their original baseline state.

```sql
USE [VMM_RFID_RETAIL_SOLUTION];
GO
PRINT '=======================================================';
PRINT '  REVERTING ALL DASHBOARD CHANGES TO PRISTINE BASELINE ';
PRINT '=======================================================';
BEGIN TRANSACTION;

-- 1. RESTORE LIVE STOCK (Re-enable the 5 tags for HD44)
UPDATE TOP (5) dbo.tbl_Encoding_Dtl 
SET Is_Status = '1' 
WHERE STORE_ID = 6 AND Is_Status = '0';
PRINT '>>> 1. Live Stock: 5 tags restored for Store HD44.';

-- 2. RESTORE STORE VALIDATION (Re-validate HU 4000827011 for Store HD55)
UPDATE dbo.tbl_GRC_DETAILS 
SET Is_Status = 1 
WHERE STORE_CODE = 'HD55' AND HU = '4000827011';
PRINT '>>> 2. Store Validation: HU 4000827011 re-validated for HD55.';

-- 3. RESTORE CYCLE COUNT (Subtract the 5 test scanned items)
UPDATE TOP (1) dbo.tbl_Cycle_count_Dtl 
SET Scanned_Qty = Scanned_Qty - 5 
WHERE Ref_ID = '20260909024154' AND Site_Code = 'HD55';
PRINT '>>> 3. Cycle Count: Scanned Qty restored for audit Ref 20260909024154.';

-- 4. RESTORE DC VALIDATION (Mark outward HU 1229042922 back to Processed)
UPDATE dbo.tbl_SAP_DC_Outward_Dtl 
SET Is_Complete_Status_Flg = '1' 
WHERE Reciving_Plant = 'HD55' AND HU_No = '1229042922';
PRINT '>>> 4. DC Validation: HU 1229042922 restored to Processed for HD55.';

-- 5. RESTORE VENDOR HU DISCREPANCY (Subtract the 10 test discrepancy items)
UPDATE TOP (1) dbo.tbl_WH_Val_Box_HU_Park_Dtl 
SET Park_Qty = Park_Qty - 10 
WHERE Park_No = 1 AND Article_Code = '1133019672009';
PRINT '>>> 5. Vendor Discrepancy: Parked Qty restored for Article 1133019672009.';

-- 6. RESTORE TAG MANAGEMENT (Shift tag location back to Store)
UPDATE dbo.tbl_tagmanagement_history 
SET TAG_LOCATION = 'Store' 
WHERE TAG_ID = 'E2801170200014116AB40B64';
PRINT '>>> 6. Tag Management: Tag E2801170200014116AB40B64 restored to Store.';

-- 7. RESTORE DC ENCODING (Restore original historical timestamp)
UPDATE dbo.tbl_Encoding_Dtl 
SET Encode_Date = '2026-09-11 08:15:00.000' 
WHERE Encode_Trans_ID = 2006662;
PRINT '>>> 7. DC Encoding: Trans 2006662 restored to original timestamp.';

COMMIT TRANSACTION;
PRINT '=======================================================';
PRINT '  ALL 7 SECTIONS RESTORED SUCCESSFULLY!                ';
PRINT '=======================================================';
GO
```

---

## Detailed Breakdown by Section

## 1. Live Stock Dashboard

- **SignalR Event**: `ReceiveLiveStockPatch`
- **Target Table**: `dbo.tbl_Encoding_Dtl`
- **Target Store**: Store `HD44` (`STORE_ID = 6`) or Store `HD55` (`STORE_ID = 1`)
- **Impacted Columns**: `RFID_STOCK`, `DIFFERENCE`, `PERCENTAGE` + Top Summary Totals

### 🔴 Step A: Trigger Live Stock Change (Simulate Tag Sale / Checkout)
```sql
-- Subtract 5 RFID tags from Store HD44 (setting Is_Status = '0')
UPDATE TOP (5) dbo.tbl_Encoding_Dtl 
SET Is_Status = '0' 
WHERE STORE_ID = 6 AND Is_Status = '1';
```
*Expected Result in UI*: Store `HD44` row shows a red flashing indicator (`Δ -5`), `RFID_STOCK` drops by 5, and header `Total RFID Qty` updates immediately without page refresh.

### 🟢 Step B: Restore Live Stock (Simulate Tag Return / Inward)
```sql
-- Restore 5 RFID tags back to Store HD44 (setting Is_Status = '1')
UPDATE TOP (5) dbo.tbl_Encoding_Dtl 
SET Is_Status = '1' 
WHERE STORE_ID = 6 AND Is_Status = '0';
```
*Expected Result in UI*: Store `HD44` row shows a green flashing indicator (`Δ +5`), restoring original stock.

---

## 2. Store Validation (GRC Received vs Validated)

- **SignalR Event**: `ReceiveStoreValidationPatch`
- **Target Table**: `dbo.tbl_GRC_DETAILS`
- **Target Store**: Store `HD55`
- **Impacted Columns**: `HU_VALIDATED_QTY`, `HU_WRONG_QTY`, `PENDING_QTY` + Summary Delta

### 🔴 Step A: Trigger Store Validation Change (De-validate an HU)
```sql
-- Mark HU '4000827011' as Unvalidated (Is_Status = 0)
UPDATE dbo.tbl_GRC_DETAILS 
SET Is_Status = 0 
WHERE STORE_CODE = 'HD55' AND HU = '4000827011';
```
*Expected Result in UI*: Store `HD55` row updates `HU Validated` (-1), `HU Wrong` (+1), and shows red ticker animation.

### 🟢 Step B: Restore Store Validation (Re-validate the HU)
```sql
-- Mark HU '4000827011' as Validated (Is_Status = 1)
UPDATE dbo.tbl_GRC_DETAILS 
SET Is_Status = 1 
WHERE STORE_CODE = 'HD55' AND HU = '4000827011';
```
*Expected Result in UI*: Store `HD55` row flashes green (`Δ +1 Validated`), restoring validated quantity.

---

## 3. Cycle Count Audits

- **SignalR Event**: `ReceiveCycleCountPatch`
- **Target Table**: `dbo.tbl_Cycle_count_Dtl`
- **Target Audit Ref**: `Ref_ID = '20260909024154'`, Site `HD55`
- **Impacted Columns**: `SCANNED_QTY`, `NET_DIFF`, `EXCESS_QTY`, `SHORT_QTY`

### 🔴 Step A: Trigger Cycle Count Audit Delta (Simulate Tags Counted)
```sql
-- Add +5 to Scanned Qty on active audit reference
UPDATE TOP (1) dbo.tbl_Cycle_count_Dtl 
SET Scanned_Qty = Scanned_Qty + 5 
WHERE Ref_ID = '20260909024154' AND Site_Code = 'HD55';
```
*Expected Result in UI*: Audit `20260909024154` shows green pulse (`Δ +5`), `SCANNED_QTY` increases by 5, and `NET_DIFF` adjusts in real-time.

### 🟢 Step B: Restore Cycle Count
```sql
-- Subtract 5 to restore baseline count
UPDATE TOP (1) dbo.tbl_Cycle_count_Dtl 
SET Scanned_Qty = Scanned_Qty - 5 
WHERE Ref_ID = '20260909024154' AND Site_Code = 'HD55';
```

---

## 4. DC Validation (Outward Validation / Parked)

- **SignalR Event**: `ReceiveDcValidationPatch`
- **Target Table**: `dbo.tbl_SAP_DC_Outward_Dtl`
- **Target Plant**: Plant `HD55` (`HU_No = '1229042922'`)
- **Impacted Columns**: `PROCESSED_HU`, `UNPROCESSED_HU`, `PROCESSED_ARTICLE_QTY`

### 🔴 Step A: Trigger DC Outward Validation Change (Mark HU as Unprocessed)
```sql
-- Toggle HU to Unprocessed ('0')
UPDATE dbo.tbl_SAP_DC_Outward_Dtl 
SET Is_Complete_Status_Flg = '0' 
WHERE Reciving_Plant = 'HD55' AND HU_No = '1229042922';
```
*Expected Result in UI*: Plant `HD55` row updates `Processed HU` (-1) and `Unprocessed HU` (+1) with red visual highlight.

### 🟢 Step B: Restore DC Outward Validation (Mark HU as Processed)
```sql
-- Toggle HU back to Processed ('1')
UPDATE dbo.tbl_SAP_DC_Outward_Dtl 
SET Is_Complete_Status_Flg = '1' 
WHERE Reciving_Plant = 'HD55' AND HU_No = '1229042922';
```
*Expected Result in UI*: Plant `HD55` row flashes green (`Δ +1 Processed HU`).

---

## 5. Vendor Discrepancy (HU Discrepancies)

- **SignalR Event**: `ReceiveVendorDiscrepancyPatch`
- **Target Table**: `dbo.tbl_WH_Val_Box_HU_Park_Dtl`
- **Target Vendor**: Article `1133019672009` under `Park_No = 1`
- **Impacted Columns**: `SCANNED_QTY`, `DIFF_QTY`, `DIFF_TILL_DATE`

### 🔴 Step A: Trigger Vendor Discrepancy Delta (Simulate New Discrepant Tags Scanned)
```sql
-- Add +10 to Parked (Scanned) Qty
UPDATE TOP (1) dbo.tbl_WH_Val_Box_HU_Park_Dtl 
SET Park_Qty = Park_Qty + 10 
WHERE Park_No = 1 AND Article_Code = '1133019672009';
```
*Expected Result in UI*: Corresponding vendor row updates with `Δ +10` scanned and diff recalculates dynamically.

### 🟢 Step B: Restore Vendor Discrepancy
```sql
-- Subtract 10 to restore original quantity
UPDATE TOP (1) dbo.tbl_WH_Val_Box_HU_Park_Dtl 
SET Park_Qty = Park_Qty - 10 
WHERE Park_No = 1 AND Article_Code = '1133019672009';
```

---

## 6. Tag Management (Location Shift)

- **SignalR Event**: `ReceiveTagManagementPatch`
- **Target Table**: `dbo.tbl_tagmanagement_history`
- **Target Tag**: `TAG_ID = 'E2801170200014116AB40B64'`
- **Impacted Columns**: `StoreCount`, `WarehouseCount`, `DeltaStoreCount`, `DeltaWarehouseCount`

### 🔴 Step A: Trigger Tag Location Change (Shift Tag from Store to Warehouse)
```sql
-- Move tag from Store to Warehouse
UPDATE dbo.tbl_tagmanagement_history 
SET TAG_LOCATION = 'Warehouse' 
WHERE TAG_ID = 'E2801170200014116AB40B64';
```
*Expected Result in UI*: Header metrics update with `Store: -1` and `Warehouse: +1`.

### 🟢 Step B: Restore Tag Location (Shift Tag back to Store)
```sql
-- Move tag back to Store
UPDATE dbo.tbl_tagmanagement_history 
SET TAG_LOCATION = 'Store' 
WHERE TAG_ID = 'E2801170200014116AB40B64';
```
*Expected Result in UI*: Header metrics update with `Store: +1` and `Warehouse: -1`.

---

## 7. DC Encoding (Hourly Encoding Production)

- **SignalR Event**: `ReceiveDcEncodingPatch`
- **Target Table**: `dbo.tbl_Encoding_Dtl`
- **Target Record**: `Encode_Trans_ID = 2006662` (`WH_ID = 1`)
- **Impacted Columns**: Current Hour Block (`08-09`, `09-10`, `10-11`, etc.) + `TotalCount`

### 🔴 Step A: Trigger DC Encoding Hourly Delta (Encode Tag in Current Hour)
```sql
-- Shift an encoded record timestamp to current UTC hour today
UPDATE dbo.tbl_Encoding_Dtl 
SET Encode_Date = GETUTCDATE() 
WHERE Encode_Trans_ID = 2006662;
```
*Expected Result in UI*: The active hour bar/card increments by +1 with green ticker animation.

### 🟢 Step B: Restore DC Encoding Timestamp
```sql
-- Restore original historical date
UPDATE dbo.tbl_Encoding_Dtl 
SET Encode_Date = '2026-09-11 08:15:00.000' 
WHERE Encode_Trans_ID = 2006662;
```

---

## 🚀 One-Click Automated PowerShell Test Scripts

If you want to test each section automatically from the terminal without manual SQL typing, use these ready-to-run PowerShell one-liners:

### Test Live Stock WebSocket
```powershell
powershell -Command "
`$cs = 'Data Source=tcp:127.0.0.1,1433;Initial Catalog=VMM_RFID_RETAIL_SOLUTION;User ID=sa;Password=M@rkss2026;TrustServerCertificate=True;';
`$c = New-Object System.Data.SqlClient.SqlConnection(`$cs); `$c.Open(); `$cmd = `$c.CreateCommand();
Write-Host '>>> Toggling 5 tags in DB for HD44...' -ForegroundColor Cyan;
`$cmd.CommandText = 'UPDATE TOP (5) dbo.tbl_Encoding_Dtl SET Is_Status = ''0'' WHERE STORE_ID = 6 AND Is_Status = ''1''';
`$cmd.ExecuteNonQuery();
Start-Sleep -Seconds 3;
Write-Host '>>> Restoring tags...' -ForegroundColor Green;
`$cmd.CommandText = 'UPDATE TOP (5) dbo.tbl_Encoding_Dtl SET Is_Status = ''1'' WHERE STORE_ID = 6 AND Is_Status = ''0''';
`$cmd.ExecuteNonQuery();
`$c.Close();
Write-Host '>>> Done. Watch your browser dashboard for live ticker animations!' -ForegroundColor Yellow;
"
```

### Test Store Validation WebSocket
```powershell
powershell -Command "
`$cs = 'Data Source=tcp:127.0.0.1,1433;Initial Catalog=VMM_RFID_RETAIL_SOLUTION;User ID=sa;Password=M@rkss2026;TrustServerCertificate=True;';
`$c = New-Object System.Data.SqlClient.SqlConnection(`$cs); `$c.Open(); `$cmd = `$c.CreateCommand();
Write-Host '>>> De-validating HU 4000827011 for HD55...' -ForegroundColor Cyan;
`$cmd.CommandText = 'UPDATE dbo.tbl_GRC_DETAILS SET Is_Status = 0 WHERE STORE_CODE = ''HD55'' AND HU = ''4000827011''';
`$cmd.ExecuteNonQuery();
Start-Sleep -Seconds 4;
Write-Host '>>> Re-validating HU 4000827011 for HD55...' -ForegroundColor Green;
`$cmd.CommandText = 'UPDATE dbo.tbl_GRC_DETAILS SET Is_Status = 1 WHERE STORE_CODE = ''HD55'' AND HU = ''4000827011''';
`$cmd.ExecuteNonQuery();
`$c.Close();
Write-Host '>>> Done. Store Validation WebSocket patch broadcasted!' -ForegroundColor Yellow;
"
```

### Test DC Validation (Parked) WebSocket
```powershell
powershell -Command "
`$cs = 'Data Source=tcp:127.0.0.1,1433;Initial Catalog=VMM_RFID_RETAIL_SOLUTION;User ID=sa;Password=M@rkss2026;TrustServerCertificate=True;';
`$c = New-Object System.Data.SqlClient.SqlConnection(`$cs); `$c.Open(); `$cmd = `$c.CreateCommand();
Write-Host '>>> Toggling HU 1229042922 for HD55 to Unprocessed...' -ForegroundColor Cyan;
`$cmd.CommandText = 'UPDATE dbo.tbl_SAP_DC_Outward_Dtl SET Is_Complete_Status_Flg = ''0'' WHERE Reciving_Plant = ''HD55'' AND HU_No = ''1229042922''';
`$cmd.ExecuteNonQuery();
Start-Sleep -Seconds 4;
Write-Host '>>> Restoring HU 1229042922 back to Processed...' -ForegroundColor Green;
`$cmd.CommandText = 'UPDATE dbo.tbl_SAP_DC_Outward_Dtl SET Is_Complete_Status_Flg = ''1'' WHERE Reciving_Plant = ''HD55'' AND HU_No = ''1229042922''';
`$cmd.ExecuteNonQuery();
`$c.Close();
Write-Host '>>> Done. DC Validation WebSocket patch broadcasted!' -ForegroundColor Yellow;
"
```
