# SQL Bug Report & Fix: Allocated Store Report Drill-Down Modal

## Summary
When clicking on an **EAN** or **Encoding Tags** in the **Allocated Store Report**, the modal box (`GetEncodingReportDetailsModal`) returns `0` records:
```json
{"pageIndex":1,"recordCount":0,"totalCount":0,"data":[]}
```

---

## 1. Stored Procedure & Status Affected
- **Stored Procedure:** `[dbo].[SP_NEW_REPORT]`
- **Block / Status:**
  ```sql
  ELSE If(@status = 'VIEW_ENCODING_SHOW_DATA_FOR_STORE')
  ```
  *(Also check `@status = 'EXPORT_VIEW_ENCODING_SHOW_DATA_FOR_STORE'` if identical)*

---

## 2. Root Cause
In the database table `[dbo].[tbl_Encoding_Dtl]`:
- Retail store-level encodings (e.g., Store `HD44`) have a valid `Store_ID` and `Store_Floor_ID`, but **`WH_ID` is `NULL`** (because they are encoded at the store, not at the warehouse).
- However, the query inside `VIEW_ENCODING_SHOW_DATA_FOR_STORE` has:
  ```sql
  WHERE dbo.[tbl_Encoding_Dtl].WH_ID IS NOT NULL AND dbo.[tbl_Encoding_Dtl].Store_ID IS NOT NULL
  ```
- Because of `WH_ID IS NOT NULL`, all valid store-level encodings are filtered out, causing the modal to always show:
  > *"No matching records found"*

In contrast, the parent report query (`SHOW_DATA_FOR_STORE_ENCODING`) only requires `Store_ID IS NOT NULL AND Store_Floor_ID IS NOT NULL`, which is why rows appear on the main page but disappear when clicking into details.

---

## 3. Failing Query Section

```sql
ELSE If(@status='VIEW_ENCODING_SHOW_DATA_FOR_STORE')
BEGIN   
    SELECT 
        dbo.[tbl_Store_Master].Store_Code AS 'LOCATION_NAME',
        dbo.[tbl_Encoding_Dtl].EAN, 
        dbo.[tbl_Encoding_Dtl].Encode_EPC,
        ISNULL(dbo.[tbl_Material_Master].Material, 'NA') AS 'ARTICLE',
        ISNULL(dbo.[tbl_Material_Master].ART_DESC, 'NA') AS 'ARTICLE_DESC',
        FORMAT(dbo.[tbl_Encoding_Dtl].Encode_Date,'yyyy-MM-dd HH:mm:ss') as 'Encode_Date'
    INTO #VIEW_ENCODING_SHOW_DATA_FOR_STORE
    FROM dbo.[tbl_Encoding_Dtl]
    LEFT JOIN dbo.[tbl_Ean_Mat_Mst] ON dbo.[tbl_Encoding_Dtl].ean = dbo.[tbl_Ean_Mat_Mst].EAN_UPC
    LEFT JOIN dbo.[tbl_Material_Master] ON dbo.[tbl_Ean_Mat_Mst].Material = dbo.[tbl_Material_Master].Material
    LEFT JOIN dbo.[tbl_Store_Master]  ON dbo.[tbl_Encoding_Dtl].Store_ID = dbo.[tbl_Store_Master].Store_ID
    WHERE dbo.[tbl_Encoding_Dtl].WH_ID IS NOT NULL AND dbo.[tbl_Encoding_Dtl].Store_ID IS NOT NULL  -- <<--- [BUG HERE]
    AND dbo.[tbl_Encoding_Dtl].EAN = COALESCE(NULLIF(@EAN, ''), dbo.[tbl_Encoding_Dtl].EAN)
    AND ((@Material='' and (dbo.[tbl_Material_Master].Material IS NULL)) 
         OR dbo.[tbl_Material_Master].Material = COALESCE(NULLIF(@material, ''), dbo.[tbl_Material_Master].Material))
    AND dbo.[tbl_Store_Master].Store_Code = COALESCE(NULLIF(@Store_code, ''), dbo.[tbl_Store_Master].Store_Code)
    AND convert(date, dbo.[tbl_Encoding_Dtl].Encode_Date, 103) between 
        convert(date, COALESCE(NULLIF(@fromdate, ''), dbo.[tbl_Encoding_Dtl].Encode_date), 103)
        and convert(date, COALESCE(NULLIF(@Todate, ''), dbo.[tbl_Encoding_Dtl].Encode_date), 103);
```

---

## 4. Required Fix

### In `[dbo].[SP_NEW_REPORT]`:

**Change line:**
```sql
WHERE dbo.[tbl_Encoding_Dtl].WH_ID IS NOT NULL AND dbo.[tbl_Encoding_Dtl].Store_ID IS NOT NULL
```

**To:**
```sql
WHERE dbo.[tbl_Encoding_Dtl].Store_ID IS NOT NULL
```
*(or `WHERE (dbo.[tbl_Encoding_Dtl].WH_ID IS NOT NULL OR dbo.[tbl_Encoding_Dtl].Store_ID IS NOT NULL)`)*

---

## 5. Verification Query (Run After Fix)
Test that the procedure returns rows for the store encoding:
```sql
DECLARE @RC INT, @TC INT;
EXEC [dbo].[SP_NEW_REPORT] 
    @status = 'VIEW_ENCODING_SHOW_DATA_FOR_STORE',
    @Store_code = 'HD44',
    @EAN = '19419463',
    @Material = '1130132760003',
    @fromdate = '2026-09-23',
    @todate = '2026-09-23',
    @PageIndex = 1,
    @PageSize = 10,
    @RecordCount = @RC OUTPUT,
    @TotalCount = @TC OUTPUT;

SELECT @RC AS RecordCount, @TC AS TotalCount;
```
Expected result: Returns the matching EPC row(s) and `@RC > 0`.
