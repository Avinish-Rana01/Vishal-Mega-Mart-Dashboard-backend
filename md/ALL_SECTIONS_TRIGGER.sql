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
