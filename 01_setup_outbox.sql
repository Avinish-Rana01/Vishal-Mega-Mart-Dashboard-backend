-- =============================================
-- Outbox Change Notification Setup
-- Run ONCE on VMM_RFID_RETAIL_SOLUTION
-- =============================================

-- 1. Create outbox table
IF NOT EXISTS (
    SELECT 1 FROM sys.tables 
    WHERE name = 'ChangeNotifications' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE dbo.ChangeNotifications (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Section     NVARCHAR(50)  NOT NULL,
        ChangedAt   DATETIME2     NOT NULL DEFAULT GETDATE(),
        IsProcessed BIT           NOT NULL DEFAULT 0
    );
    -- Index makes HasPendingAsync check < 1ms even with thousands of rows
    CREATE INDEX IX_ChangeNotifications_Section_Processed
        ON dbo.ChangeNotifications (Section, IsProcessed);
    PRINT 'ChangeNotifications table created.';
END
ELSE
    PRINT 'ChangeNotifications table already exists — skipping create.';
GO

-- =============================================
-- 2. LIVE_STOCK  ->  tbl_ZCURR_STOCK
-- =============================================
IF OBJECT_ID('dbo.trg_ZCURR_STOCK_Notify', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_ZCURR_STOCK_Notify;
GO
CREATE TRIGGER dbo.trg_ZCURR_STOCK_Notify
ON dbo.tbl_ZCURR_STOCK
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    -- Only insert a new notification if one is already pending (de-duplicate)
    IF NOT EXISTS (
        SELECT 1 FROM dbo.ChangeNotifications 
        WHERE Section = 'LIVE_STOCK' AND IsProcessed = 0
    )
        INSERT INTO dbo.ChangeNotifications (Section) VALUES ('LIVE_STOCK');
END
GO

-- =============================================
-- 3. CYCLE_COUNT  ->  tbl_Cycle_count_Dtl
-- =============================================
IF OBJECT_ID('dbo.trg_CycleCountDtl_Notify', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_CycleCountDtl_Notify;
GO
CREATE TRIGGER dbo.trg_CycleCountDtl_Notify
ON dbo.tbl_Cycle_count_Dtl
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.ChangeNotifications 
        WHERE Section = 'CYCLE_COUNT' AND IsProcessed = 0
    )
        INSERT INTO dbo.ChangeNotifications (Section) VALUES ('CYCLE_COUNT');
END
GO

-- =============================================
-- 4. STORE_VALIDATION  ->  tbl_WH_Val_Box_HU_Park_Dtl
-- =============================================
IF OBJECT_ID('dbo.trg_WHValHU_Notify', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_WHValHU_Notify;
GO
CREATE TRIGGER dbo.trg_WHValHU_Notify
ON dbo.tbl_WH_Val_Box_HU_Park_Dtl
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.ChangeNotifications 
        WHERE Section = 'STORE_VALIDATION' AND IsProcessed = 0
    )
        INSERT INTO dbo.ChangeNotifications (Section) VALUES ('STORE_VALIDATION');
END
GO

-- =============================================
-- 5. DC_ENCODING  ->  tbl_Encoding_Dtl
-- =============================================
IF OBJECT_ID('dbo.trg_EncodingDtl_Notify', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_EncodingDtl_Notify;
GO
CREATE TRIGGER dbo.trg_EncodingDtl_Notify
ON dbo.tbl_Encoding_Dtl
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.ChangeNotifications 
        WHERE Section = 'DC_ENCODING' AND IsProcessed = 0
    )
        INSERT INTO dbo.ChangeNotifications (Section) VALUES ('DC_ENCODING');
END
GO

-- =============================================
-- 6. TAG_MANAGEMENT  ->  VMM_TAG_MST
-- =============================================
IF OBJECT_ID('dbo.trg_VmmTagMst_Notify', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_VmmTagMst_Notify;
GO
CREATE TRIGGER dbo.trg_VmmTagMst_Notify
ON dbo.VMM_TAG_MST
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.ChangeNotifications 
        WHERE Section = 'TAG_MANAGEMENT' AND IsProcessed = 0
    )
        INSERT INTO dbo.ChangeNotifications (Section) VALUES ('TAG_MANAGEMENT');
END
GO

-- =============================================
-- 7. VENDOR_DISCREPANCY  ->  tbl_GRC_DETAILS
-- =============================================
IF OBJECT_ID('dbo.trg_GrcDetails_Notify', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_GrcDetails_Notify;
GO
CREATE TRIGGER dbo.trg_GrcDetails_Notify
ON dbo.tbl_GRC_DETAILS
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.ChangeNotifications 
        WHERE Section = 'VENDOR_DISCREPANCY' AND IsProcessed = 0
    )
        INSERT INTO dbo.ChangeNotifications (Section) VALUES ('VENDOR_DISCREPANCY');
END
GO

-- =============================================
-- 8. DC_VALIDATION  ->  tbl_SAP_DC_Outward_Dtl
-- =============================================
IF OBJECT_ID('dbo.trg_SAPDCOutward_Notify', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_SAPDCOutward_Notify;
GO
CREATE TRIGGER dbo.trg_SAPDCOutward_Notify
ON dbo.tbl_SAP_DC_Outward_Dtl
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.ChangeNotifications 
        WHERE Section = 'DC_VALIDATION' AND IsProcessed = 0
    )
        INSERT INTO dbo.ChangeNotifications (Section) VALUES ('DC_VALIDATION');
END
GO

PRINT 'All 7 triggers created successfully.';
PRINT 'Outbox setup complete. Backend pollers will now only fire SPs on real data changes.';
GO
