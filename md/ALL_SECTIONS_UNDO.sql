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
