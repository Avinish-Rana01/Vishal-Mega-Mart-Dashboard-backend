



-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SP_Master]	
	 @Status varchar(50)='',
	 @Store_Code nvarchar(10)='',
	 @Store_Name nvarchar(max)='',
	 @Entry_By int=0,
	 @Is_Status int=0,	 
	 @Reader_Id INT=0,
	 @Reader_Name nvarchar(20)='',
	 @Reader_MAC nvarchar(20)='',
	 @Antena int=0,
	 @User_Name nvarchar(20)='',
	 @Password nvarchar(20)='',
	 @User_Type nvarchar(20)=' ',
	 @Reader_Config_ID int=0,
	 @Modify_By int=0,
	 @Store_ID int=0,
	 @User_ID int=0,
	 @Device_ESN Nvarchar(50)='',
	 @Encode_DateTime  Nvarchar(50)='',
	 @WH_ID int=0,
	 @Message nvarchar(100)='' OUTPUT,
	 @Wh_Code nvarchar(30)='',
	 @Wh_Name nvarchar(50)='',
	 @Wh_Address nvarchar(100)='',
	 @Store_Floor_ID int=0,
	 @Store_Floor nvarchar(50)='',
	 @State nvarchar(100)='',
	 @City nvarchar(100)='',
	 @Store_Manager nvarchar(100)='',
	 @Area_Manager nvarchar(100)='',
	 @ZFM nvarchar(100)='',
	 @LP nvarchar(100)='',
	 @Email_ID nvarchar(150)='',
	 @Is_Email_Required bit=0


AS
BEGIN
	If(@status='Insert_tbl_Store_Master')
	Begin	
		IF NOT EXISTS (SELECT 1 FROM tbl_Store_Master WHERE Store_Code = @Store_Code OR Store_Name=@Store_Name)
		BEGIN	
			INSERT INTO tbl_Store_Master(Store_Code, Store_Name, State, City, Store_Manager, Area_Manager, ZFM, LP, Entry_By) 
			VALUES (@Store_Code, @Store_Name, @State, @City, @Store_Manager, @Area_Manager, @ZFM, @LP, @Entry_By);
			SET @Message = 'Record Insert successfully.';
		END
		ELSE
		BEGIN
			SET @Message = 'Store already exists.';
		END
	End   
		
ELSE IF(@status='Update_tbl_Store_Master')
BEGIN
	IF EXISTS (SELECT 1 FROM tbl_Store_Master WHERE (Store_Code = @Store_Code OR Store_Name=@Store_Name) AND Store_ID != @Store_ID)
    BEGIN
        SET @Message = 'Store code/Store Name already exists.'
    END
	ELSE
	BEGIN
		Update tbl_Store_Master Set 
		Store_Code=@Store_Code, Store_Name=@Store_Name, State=@State, City=@City, Store_Manager=@Store_Manager, Area_Manager=@Area_Manager, ZFM=@ZFM, LP=@LP, Modify_Date=GETDATE(), Is_Status=1, Modify_By=@Modify_By
		Where Store_ID = @Store_ID

		SET @Message = 'Updated Successfully.'
	END
END

ELSE IF(@status='Delete_tbl_Store_Master')
BEGIN
    --Update tbl_Store_Master Set Is_Status='0' Where Store_ID = @Store_ID
	If (select Is_Status from dbo.[tbl_Store_Master] where Store_ID = @Store_ID)='0'
	Begin
	   Update tbl_Store_Master Set Is_Status='1' Where Store_ID = @Store_ID
	end
	else If (select Is_Status from dbo.[tbl_Store_Master] where Store_ID = @Store_ID)='1'
	begin
	   Update tbl_Store_Master Set Is_Status='0' Where Store_ID = @Store_ID
	end
END


  ELSE  If(@status='Insert_tbl_Reader_Mst')
  BEGIN
     IF NOT EXISTS (SELECT * FROM tbl_Reader_Mst WHERE Reader_MAC = @Reader_MAC)
     BEGIN	
         INSERT INTO tbl_Reader_Mst(Reader_MAC,Entry_By) VALUES (@Reader_MAC,@Entry_By);
         SET @Message = 'Inserted successfully.';
     END
     ELSE
     BEGIN
         SET @Message = 'Already exists.';
     END
  END
          
  ELSE IF(@status='Update_tbl_Reader_Mst')
  BEGIN
     IF EXISTS (SELECT 1 FROM tbl_Reader_Mst  WHERE Reader_MAC = @Reader_MAC AND Reader_ID != @Reader_Id)
     BEGIN
        SET @Message = 'Reader MAC already exists.'
     END
     ELSE
	 BEGIN
        Update tbl_Reader_Mst Set Reader_MAC=@Reader_MAC,Modify_By=@Modify_By,Modify_Dt=GETDATE(),Is_Status =1
	    Where Reader_ID = @Reader_ID
        SET @Message = 'Updated Successfully.'
	 END
  END

  ELSE IF(@status='Delete_tbl_Reader_Mst')
  BEGIN
      Update tbl_Reader_Mst Set Is_Status='0' Where Reader_ID = @Reader_ID
  END  
	   
  ELSE  If(@status='Insert_user_registration')
  Begin	
	  Select @Store_ID=Store_ID from tbl_Reader_Configuration_Mst where  Reader_Config_ID=@Reader_Config_ID
      IF NOT EXISTS (SELECT * FROM User_Registration WHERE User_Name = @User_Name)
      BEGIN	
         INSERT INTO User_Registration(User_Name,Password,User_Type,Store_ID,WH_ID,Entry_By,Email_ID,Is_Email_Required) 
         VALUES (@User_Name,@Password,@User_Type,
         CASE WHEN @Store_ID = 0 THEN NULL ELSE @Store_ID END, 
         CASE WHEN @WH_ID = 0 THEN NULL ELSE @WH_ID END,
         @Entry_By, @Email_ID, @Is_Email_Required)
         SET @Message = 'User Created Successfully.';
      END
      ELSE
      BEGIN
          SET @Message = 'User Already exists.';
      END
 End       

  ELSE IF(@status='Update_user_registration')
       BEGIN
	    Update User_Registration Set user_name=@User_Name,Password=@Password,User_Type=@User_Type,
		--Store_ID=@Store_ID,WH_ID=@WH_ID,
	    Store_ID = CASE WHEN @Store_ID = 0 THEN NULL ELSE @Store_ID END,
        WH_ID = CASE WHEN @WH_ID = 0 THEN NULL ELSE @WH_ID END,
        Email_ID = @Email_ID, Is_Email_Required = @Is_Email_Required,
		Is_Status=1,Modify_By=@Modify_By,Modify_Date=GETDATE()
		Where User_ID = @User_ID
       END

  ELSE  IF(@status='Delete_user_registration')
       BEGIN
       --Update User_Registration Set Is_Status='0' Where User_ID = @User_ID
	   If (select Is_Status from dbo.[User_Registration] Where User_ID = @User_ID)='0'
	   Begin
	   Update dbo.[User_Registration] Set Is_Status='1' Where User_ID = @User_ID
	   end
	   else If (select Is_Status from dbo.[User_Registration] where User_ID = @User_ID)='1'
	   begin
	   Update dbo.[User_Registration] Set Is_Status='0' Where User_ID = @User_ID
	   end
       END

  ELSE  If(@status='Insert_Reader_configuration_master')
	  Begin
			IF NOT EXISTS (SELECT * FROM tbl_Reader_Configuration_Mst 
			WHERE Reader_Name=@Reader_Name and Store_ID=@Store_ID and Reader_ID=@Reader_Id)
             BEGIN	
                 INSERT INTO tbl_Reader_Configuration_Mst (Reader_Name,Store_ID,Reader_ID,Antena,Entry_By,Entry_Date) 
                 VALUES (@Reader_Name,@Store_ID,@Reader_Id,@Antena,@Entry_By,GETDATE());
                 SET @Message = 'Inserted successfully.';
             END
             ELSE
             BEGIN
                 SET @Message = 'Already exists.';
             END
       End       

ELSE IF(@status='Update_Reader_configuration_master')
BEGIN
   IF EXISTS (SELECT 1 FROM tbl_Reader_Configuration_Mst WHERE Reader_Name = @Reader_Name AND Reader_Config_ID != @Reader_Config_ID)
   BEGIN
      SET @Message = 'Reader Name already exists.'
    END
    ELSE
	BEGIN
	    Update tbl_Reader_Configuration_Mst
	    Set Reader_Name=@Reader_Name,Store_ID=@Store_ID,Reader_ID=@Reader_Id,Antena=@Antena,Modify_Date=GETDATE(),
		Modify_By=@Modify_By,Is_Status=1 Where Reader_Config_ID=@Reader_Config_ID
	END
END

ELSE IF(@status='Delete_Reader_configuration_master')
BEGIN
    Update tbl_Reader_Configuration_Mst Set Is_Status='0' Where Reader_Config_ID=@Reader_Config_ID
END

 --Below Code ADDED BY DIPTI
ELSE IF(@Status='SP_Login')
BEGIN
    DECLARE @USERCOUNT INT;
	DECLARE @USERSTATUS INT
	DECLARE @PASSWORDCORRECT INT;
	SELECT @USERCOUNT=COUNT(*) FROM dbo.[User_Registration] where  User_Name=@User_Name;
	SELECT @USERSTATUS=COUNT(*) FROM dbo.[User_Registration] where  User_Name=@User_Name and Is_Status =1;
	SELECT @PASSWORDCORRECT=COUNT(*) FROM dbo.[User_Registration] where  User_Name=@User_Name and Password=@Password;

	IF (@USERCOUNT>0)
	BEGIN
	   IF(@USERSTATUS>0)
	   BEGIN
	      IF(@PASSWORDCORRECT>0)
		  BEGIN
		     SELECT Distinct User_Registration.User_ID,User_Name,tbl_Store_Master.STORE_NAME,Wh_Name,User_Type,Store_Code,Wh_Code from User_Registration 
	         LEFT JOIN tbl_Store_Master ON User_Registration.Store_ID=tbl_Store_Master.Store_ID
	         LEFT JOIN tbl_Warehouse_Mst WM on User_Registration.WH_ID=WM.WH_ID
             where User_Name=@User_Name and Password=@Password 

			 SET @Message='LOGIN SUCCESSFULLY'
			 PRINT @Message
		  END
		  ELSE
		  BEGIN
		     SET @Message='INCORRECT PASSWORD'
			 PRINT @Message
		  END
       END
	   ELSE
	   BEGIN
		  SET @Message='USER IS INACTIVE'
		  PRINT @Message
	   END
    END
	ELSE
	BEGIN
	   SET @Message='INVALID USER'
	   PRINT @Message
	END
END

ELSE IF(@Status='SP_Bind_StoreMaster')
BEGIN
	SELECT Distinct Store_ID,Store_Code,
	Store_Name,
	ISNULL(State, '') AS State,
	ISNULL(City, '') AS City,
	ISNULL(Store_Manager, '') AS Store_Manager,
	ISNULL(Area_Manager, '') AS Area_Manager,
	ISNULL(ZFM, '') AS ZFM,
	ISNULL(LP, '') AS LP,
	case when Is_Status=1 then 'Active' else 'In-Active' End as Status
	from tbl_Store_Master 
END

ELSE IF(@Status='SP_Bind_tbl_Reader_Mst')
BEGIN
	SELECT Distinct Reader_ID,Reader_MAC,case when Is_Status=1 then 'Active' else 'In-Active' End as Status  
	from tbl_Reader_Mst
END


ELSE IF(@Status='SP_Bind_Config_Master')
BEGIN
   select Reader_Config_ID,Reader_Name,sm.Store_Name,RM.Reader_MAC,Antena,case when RCM.Is_Status=1 then 'Active' else 'In-Active' End as Status from tbl_Reader_Configuration_Mst RCM 
   JOIN tbl_Store_Master SM on RCM.Store_ID = SM.Store_ID JOIN tbl_Reader_Mst RM on RCM.Reader_ID= RM.Reader_ID
END


ELSE IF(@Status='SP_Bind_User_Master')
BEGIN
     SELECT @Store_ID = SM.Store_ID,@WH_ID = WM.WH_ID FROM User_Registration UR
     LEFT JOIN tbl_Store_Master SM ON UR.Store_ID = SM.Store_ID
     LEFT JOIN tbl_Warehouse_Mst WM ON UR.WH_ID = WM.WH_ID
     WHERE UR.User_ID = @User_ID

	 SELECT User_ID,User_Name,Password,User_Type,UR.Store_ID,ISNULL(Store_Name,'NA') AS 'Store_Name',ISNULL(WM.Wh_Name,'NA') as 'Warehouse_Name',
	  ISNULL(UR.Email_ID, '') AS 'Email_ID',
	  ISNULL(UR.Is_Email_Required, 0) AS 'Is_Email_Required',
	  case when UR.Is_Status=1 then 'Active' else 'In-active' End as Status FROM   dbo.User_Registration UR 
	  LEFT JOIN tbl_Store_Master SM on UR.Store_ID=SM.Store_ID
	  LEFT JOIN tbl_Warehouse_Mst WM on UR.WH_ID=WM.WH_ID
	  WHERE(
            (@User_Type = 'Store Admin' AND UR.User_Type IN ('Store', 'Store Admin') AND UR.Store_ID = @Store_ID)
            OR
            (@User_Type = 'Warehouse Admin' AND UR.User_Type IN ('Warehouse', 'Warehouse Admin') AND UR.WH_ID = @WH_ID)
            OR
            (@User_Type = 'Super Admin')
           );
END


ELSE IF(@Status='SP_DDL_StoreID')
BEGIN
   IF(@User_Type='Super Admin')
   BEGIN
	  SELECT Distinct Store_Name,Store_ID from tbl_Store_Master WHERE IS_STATUS =1
   END
   ELSE 
   BEGIN
      SELECT ISNULL(Store_Name,'NA') AS 'Store_Name',SM.Store_ID FROM  dbo.User_Registration UR LEFT JOIN tbl_Store_Master SM on UR.Store_ID=SM.Store_ID where UR.User_ID=@User_ID AND SM.Is_Status = 1   
   END
END

ELSE IF(@Status='SP_DDL_ReaderID')
BEGIN
	SELECT distinct Reader_ID,Reader_MAC  from tbl_Reader_Mst WHERE IS_STATUS = 1
END

ELSE IF(@Status='SP_DDL_WarehouseID')
BEGIN
   IF(@User_Type='Super Admin')
   BEGIN
	  SELECT distinct WH_ID,Wh_Name from tbl_Warehouse_Mst where Is_Status=1
   END
   ELSE
   BEGIN
    SELECT ISNULL(WM.Wh_Name,'NA') as 'Wh_Name',WM.WH_ID FROM  dbo.User_Registration UR LEFT JOIN tbl_Warehouse_Mst WM on UR.WH_ID=WM.WH_ID where UR.User_ID=@User_ID --and WM.Is_Status=1
   END
END

ELSE IF(@Status='STORENAME_FOR_COUNTER_STATUS')
BEGIN
	SELECT DISTINCT dbo.[tbl_Device_Mst].Store_ID,dbo.[tbl_Store_Master].Store_Name FROM dbo.[tbl_Device_Mst] 
    JOIN dbo.[tbl_Store_Master] on dbo.[tbl_Device_Mst].Store_ID = dbo.[tbl_Store_Master].Store_ID
    WHERE dbo.[tbl_Store_Master].Is_Status = 1
	--and dbo.[tbl_Device_Mst].Cash_Counter_No<> 'VALIDATION'
END

ELSE IF(@Status='COUNTER_STATUS_DETAILS')
BEGIN
	--select BB.[Cash_Counter]
	--,CASE WHEN DATEDIFF(MINUTE, BB.Checkout_Dt, BB.curr_date) > 10 
	--THEN 1 ---RED LIGHT
	--ELSE 0 ---GREEN LIGHT
	--END AS 'STATUS',
	--FORMAT (BB.Checkout_Dt,'dd-MM-yyyy HH:mm:ss') AS 'LAST_UPDATED_DATE'
	--from (
	--select dbo.[tbl_Device_Mst].cash_counter_no as 'Cash_Counter',
	--dbo.[tbl_Checkout_Dtl].Checkout_Dt,getdate() as 'curr_date',
	-- ROW_NUMBER() OVER (PARTITION BY dbo.[tbl_Device_Mst].cash_counter_no ORDER BY dbo.[tbl_Checkout_Dtl].Checkout_Dt desc) AS rankno
	--from dbo.[tbl_Checkout_Dtl] 
	--JOIN dbo.[tbl_Device_Mst] ON dbo.[tbl_Checkout_Dtl].MAC=dbo.[tbl_Device_Mst].Device_ESN
	--WHERE dbo.[tbl_Device_Mst].Store_ID=@Store_ID
 --   ) BB where BB.rankno=1 ORDER BY BB.[Cash_Counter]

select BB.[Cash_Counter]
        ,CASE WHEN DATEDIFF(MINUTE, BB.Checkout_DtTime, BB.curr_date) > 10 
        THEN 1 ---RED LIGHT
        ELSE 0 ---GREEN LIGHT
        END AS 'STATUS',
        FORMAT (BB.Checkout_DtTime,'dd-MM-yyyy HH:mm:ss') AS 'LAST_UPDATED_DATE'
        from (
        select dbo.[tbl_Device_Mst].cash_counter_no as 'Cash_Counter',
        dbo.tbl_Encoding_Dtl.Checkout_DtTime,getdate() as 'curr_date',
        ROW_NUMBER() OVER (PARTITION BY dbo.[tbl_Device_Mst].cash_counter_no ORDER BY dbo.tbl_Encoding_Dtl.Checkout_DtTime desc) AS rankno
        from dbo.[tbl_Encoding_Dtl]
        JOIN dbo.[tbl_Device_Mst] ON dbo.[tbl_Device_Mst].Device_ESN = dbo.tbl_Encoding_Dtl.Checkout_MAC
        WHERE dbo.[tbl_Device_Mst].Store_ID=@Store_ID AND dbo.[tbl_Device_Mst].cash_counter_no <> 'VALIDATION'
    ) BB where BB.rankno=1 ORDER BY BB.[Cash_Counter]

END


ELSE IF(@Status='BIND_USER_TYPE')
BEGIN
	--SELECT DISTINCT dbo.[User_Registration].User_Type FROM dbo.[User_Registration] 
      SELECT 'Super Admin' AS User_Type UNION ALL SELECT 'Warehouse Admin' UNION ALL SELECT 'Store Admin' UNION ALL SELECT 'Warehouse' UNION ALL SELECT 'Store'
	  UNION ALL SELECT 'Dispatch Admin' UNION ALL SELECT 'Tag Admin';
END

ELSE If(@status='Insert_tbl_Warehouse_Master')
	Begin	
		IF NOT EXISTS (SELECT 1 FROM dbo.[tbl_Warehouse_Mst] WHERE Wh_Code = @Wh_Code OR Wh_Name=@Wh_Name)
		BEGIN	
			INSERT INTO dbo.[tbl_Warehouse_Mst] (Wh_Code,Wh_Name,Wh_Address,C_By) 
			VALUES (@Wh_Code, @Wh_Name,@Wh_Address,@Entry_By);
			SET @Message = 'Warehouse Created successfully.';
		END
		ELSE
		BEGIN
			SET @Message = 'Warehouse already exists.';
		END
	End 

ELSE IF(@status='Update_tbl_warehouse_Master')
BEGIN
	IF EXISTS (SELECT 1 FROM dbo.[tbl_Warehouse_Mst] WHERE (Wh_Code = @Wh_Code OR Wh_Name=@Wh_Name) AND WH_ID != @WH_ID)
    BEGIN
        SET @Message = 'Warehouse code/ Warehouse Name already exists.'
    END
	ELSE
	BEGIN
		Update dbo.[tbl_Warehouse_Mst] Set 
		Wh_Code=@Wh_Code,Wh_Name=@Wh_Name,Wh_Address=@Wh_Address,U_Dt=GETDATE(),Is_Status=1,U_By=@Modify_By
		Where WH_ID = @WH_ID

		SET @Message = 'Warehouse Details Updated Successfully.'
	END
END

ELSE IF(@status='Delete_tbl_warehouse_Master')
BEGIN
    If (select Is_Status from dbo.[tbl_Warehouse_Mst] where WH_ID = @WH_ID)='0'
	Begin
	   Update dbo.[tbl_Warehouse_Mst] Set Is_Status='1' Where WH_ID = @WH_ID
	end
	else If (select Is_Status from dbo.[tbl_Warehouse_Mst] where WH_ID = @WH_ID)='1'
	begin
	   Update dbo.[tbl_Warehouse_Mst] Set Is_Status='0' Where WH_ID = @WH_ID
	end
END

ELSE IF(@Status='SP_Bind_warehouseMaster')
BEGIN
	SELECT Distinct WH_ID,Wh_Code,Wh_Name,Wh_Address,case when Is_Status=1 then 'Active' else 'In-Active' End as Status 
	from dbo.[tbl_Warehouse_Mst] 
END

ELSE IF(@Status='SP_Bind_FloorMaster')
BEGIN
	SELECT Distinct f.Store_Floor_ID, f.Store_ID, ISNULL(s.Store_Name, 'NA') AS Store_Name, ISNULL(s.Store_Code, '') AS Store_Code, f.Store_Floor,
	       case when ISNULL(f.Is_Status, 1)=1 then 'Active' else 'In-Active' end as Status
	FROM dbo.[tbl_Store_Floor_Mst] f
	LEFT JOIN dbo.[tbl_Store_Master] s ON f.Store_ID = s.Store_ID
	ORDER BY f.Store_Floor_ID ASC
END

ELSE IF(@status='Insert_tbl_Store_Floor_Mst')
BEGIN
	IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Store_Floor_Mst WHERE Store_ID = @Store_ID AND Store_Floor = @Store_Floor)
	BEGIN
		INSERT INTO dbo.tbl_Store_Floor_Mst (Store_ID, Store_Floor, C_By, C_Dt, Is_Status)
		VALUES (@Store_ID, @Store_Floor, @Entry_By, GETDATE(), 1);
		SET @Message = 'Floor Record Inserted Successfully.';
	END
	ELSE
	BEGIN
		SET @Message = 'Floor already exists for this store.';
	END
END

ELSE IF(@status='Update_tbl_Store_Floor_Mst')
BEGIN
	IF EXISTS (SELECT 1 FROM dbo.tbl_Store_Floor_Mst WHERE Store_ID = @Store_ID AND Store_Floor = @Store_Floor AND Store_Floor_ID != @Store_Floor_ID)
	BEGIN
		SET @Message = 'Floor already exists for this store.';
	END
	ELSE
	BEGIN
		UPDATE dbo.tbl_Store_Floor_Mst 
		SET Store_ID = @Store_ID, Store_Floor = @Store_Floor, U_By = @Modify_By, U_Dt = GETDATE()
		WHERE Store_Floor_ID = @Store_Floor_ID;
		SET @Message = 'Floor Details Updated Successfully.';
	END
END

ELSE IF(@status='Delete_tbl_Store_Floor_Mst')
BEGIN
	IF (SELECT ISNULL(Is_Status, 1) FROM dbo.tbl_Store_Floor_Mst WHERE Store_Floor_ID = @Store_Floor_ID) = 0
	BEGIN
		UPDATE dbo.tbl_Store_Floor_Mst SET Is_Status = 1 WHERE Store_Floor_ID = @Store_Floor_ID;
	END
	ELSE
	BEGIN
		UPDATE dbo.tbl_Store_Floor_Mst SET Is_Status = 0 WHERE Store_Floor_ID = @Store_Floor_ID;
	END
END

ELSE IF(@Status='SP_DDL_Store_Dropdowns_JSON')
BEGIN
	SELECT (
		SELECT DISTINCT State FROM dbo.tbl_Store_Master WHERE State IS NOT NULL AND State <> '' FOR JSON PATH
	) AS States,
	(
		SELECT DISTINCT City, State FROM dbo.tbl_Store_Master WHERE City IS NOT NULL AND City <> '' FOR JSON PATH
	) AS Cities,
	(
		SELECT DISTINCT Area_Manager FROM dbo.tbl_Store_Master WHERE Area_Manager IS NOT NULL AND Area_Manager <> '' FOR JSON PATH
	) AS AreaManagers,
	(
		SELECT DISTINCT ZFM FROM dbo.tbl_Store_Master WHERE ZFM IS NOT NULL AND ZFM <> '' FOR JSON PATH
	) AS ZFMs,
	(
		SELECT DISTINCT LP FROM dbo.tbl_Store_Master WHERE LP IS NOT NULL AND LP <> '' FOR JSON PATH
	) AS LPs,
	(
		SELECT DISTINCT User_Name AS Store_Manager FROM dbo.User_Registration WHERE (User_Type = 'Store Admin' OR Role_ID = 5) AND Is_Status = 1 AND User_Name LIKE '%[A-Za-z]%' FOR JSON PATH
	) AS StoreManagers;
END

END----last

