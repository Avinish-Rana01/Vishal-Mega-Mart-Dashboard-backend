-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[SP_Master]	
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
	 @Store_Floor_ID int =0,
	 @Store_Floor Nvarchar(50)='',
	 @User_ID int=89,
	 @Device_ESN Nvarchar(50)='',
	 @Encode_DateTime  Nvarchar(50)='',
	 @WH_ID int=0,
	 @Message nvarchar(100)='' OUTPUT,
	 @Wh_Code nvarchar(30)='',
	 @Wh_Name nvarchar(50)='',
	 @Wh_Address nvarchar(100)='',
	 @MAIL_ID nvarchar(100)='',
	 @State_ID int=0,
	 @City_ID int=0,
	 @SM_ID int=0,
	 @AM_ID int=0,
	 @ZFM_ID int=0,
	 @LP_ID int=0,
	 @RoleName Nvarchar(50)='',
	 @State_Name Nvarchar(50)='',
	 @Emp_Code Nvarchar(50)='',
	 @Emp_Name Nvarchar(50)='',
	 @Role_ID int =0,
	 @Emp_ID int =0,
	 @UserType VARCHAR(50)='',
	 @CounterRoleid int=0,
	 @CounterEmpID int=0,
	 @CounterStoreID int =0,
	 @Email_Required_Flag int=0


AS
		SELECT @CounterRoleid=RM.ROLE_ID,@CounterEmpID = UR.User_ID,@CounterStoreID =UR.Store_ID
		FROM dbo.[User_Registration] UR INNER JOIN Role_Master RM ON UR.Role_ID = RM.Role_ID 
		WHERE User_ID=@User_ID AND UR.IS_Status='1';
		--SELECT @UserType = User_Type,@StoreID = Store_ID,@WHID = WH_ID FROM User_Registration WHERE User_ID = @User_ID;
BEGIN
	If(@status='Insert_tbl_Store_Master')
	Begin	
		IF EXISTS (SELECT 1 FROM tbl_Store_Master WHERE Store_Code = @Store_Code OR Store_Name=@Store_Name)
		BEGIN 
			SET @Message = 'Store Code / Store Name already exists.';
		END 
		ELSE IF EXISTS (SELECT 1 FROM tbl_Store_Master WHERE SM_ID = @SM_ID)
        BEGIN
			SET @Message = 'Store Manager is already assigned to another store.';
        END
        ELSE
        BEGIN	
			INSERT INTO tbl_Store_Master(Store_Code,Store_Name,Entry_By,SM_MAIL_ID, State_ID, City_ID, SM_ID, AM_ID, ZFM_ID, LP_ID) 
			VALUES (@Store_Code, @Store_Name,@Entry_By, @MAIL_ID, @State_ID, @City_ID, @SM_ID, @AM_ID, @ZFM_ID, @LP_ID);
			
			 

			 SET @Message = 'Record Insert successfully.';

	
		END
	End   
		
ELSE IF(@status='Update_tbl_Store_Master')
BEGIN
	IF EXISTS (SELECT 1 FROM tbl_Store_Master WHERE (Store_Code = @Store_Code OR Store_Name=@Store_Name) AND Store_ID <> @Store_ID)
    BEGIN
        SET @Message = 'Store Code / Store Name already exists.'
    END
	ELSE IF EXISTS (SELECT 1 FROM tbl_Store_Master WHERE SM_ID = @SM_ID AND Store_ID <> @Store_ID)
    BEGIN
        SET @Message = 'Store Manager is already assigned to another store.';
    END
	ELSE
	BEGIN

		Update tbl_Store_Master Set 
		Store_Code=@Store_Code,Store_Name=@Store_Name,Modify_Date=GETDATE(),Is_Status=1,Modify_By=@Modify_By,
		SM_MAIL_ID=@MAIL_ID, State_ID=@State_ID, City_ID=@City_ID, SM_ID=@SM_ID, AM_ID=@AM_ID, ZFM_ID=@ZFM_ID, LP_ID=@LP_ID
		Where Store_ID = @Store_ID;

	    Update User_Registration Set Store_ID=@Store_ID where User_ID = @SM_ID;

		Update User_Registration Set Store_ID=NULL,User_Type=NULL,Role_ID=NULL where User_ID = @User_ID;

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
      IF NOT EXISTS (SELECT * FROM User_Registration WHERE (User_Name=@User_Name))
      BEGIN	
         --INSERT INTO User_Registration(User_Name,Password,User_Type,Store_ID,WH_ID,Entry_By,Role_ID,Emp_ID) 
         --VALUES (@User_Name,@Password,@User_Type,
         --CASE WHEN @Store_ID = 0 THEN NULL ELSE @Store_ID END, 
         --CASE WHEN @WH_ID = 0 THEN NULL ELSE @WH_ID END,
         --@Entry_By,@Role_ID,@Emp_ID)
         --SET @Message = 'User Created Successfully.';
		 INSERT INTO User_Registration(User_Name,Password,User_Type,Store_ID,WH_ID,Entry_By,Role_ID,User_Email_ID,Email_Required_Flag,Is_Created_By_Admin_Flag)
		 --Emp_ID) 
         VALUES (@User_Name,@Password,@User_Type,
         CASE WHEN @Store_ID = 0 THEN NULL ELSE @Store_ID END, 
         CASE WHEN @WH_ID = 0 THEN NULL ELSE @WH_ID END,
         @Entry_By,@Role_ID,
		 @MAIL_ID,@Email_Required_Flag,1)
		 --@Emp_ID)
         SET @Message = 'User Created Successfully.';
      END
      ELSE
      BEGIN
          SET @Message = 'Username Already exists.';
      END
 End       

  ELSE IF(@status='Update_user_registration')
       BEGIN
	    Update User_Registration Set user_name=@User_Name,Password=@Password,User_Type=@User_Type,
	    Store_ID = CASE WHEN @Store_ID = 0 THEN NULL ELSE @Store_ID END,
        WH_ID = CASE WHEN @WH_ID = 0 THEN NULL ELSE @WH_ID END,
		Is_Status=1,Modify_By=@Modify_By,Modify_Date=GETDATE(),Role_ID=@Role_ID, 
		User_Email_ID=@MAIL_ID, Email_Required_Flag=@Email_Required_Flag
		Where User_ID = @User_ID;
       END

  ELSE IF(@status='Update_User_Password_First_Time_Login')
       BEGIN
	    Update User_Registration Set user_name=@User_Name,Modify_By=@Modify_By,Modify_Date=GETDATE(),Password=@Password, Is_Created_By_Admin_Flag=0
		Where User_ID = @User_ID
       END

  ELSE  IF(@status='Delete_user_registration')
       BEGIN
       --Update User_Registration Set Is_Status='0' Where User_ID = @User_ID
	   If (select Is_Status from dbo.[User_Registration] Where User_ID = @User_ID)='0'
	   Begin
	   Update dbo.[User_Registration] Set Is_Status='1',Modify_By=@Modify_By, Modify_Date=getdate() Where User_ID = @User_ID
	   end
	   else If (select Is_Status from dbo.[User_Registration] where User_ID = @User_ID)='1'
	   begin
	   Update dbo.[User_Registration] Set Is_Status='0',Modify_By=@Modify_By, Modify_Date=getdate() Where User_ID = @User_ID
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
		     --SELECT Distinct User_Registration.User_ID,User_Name,tbl_Store_Master.STORE_NAME,Wh_Name,User_Type,Store_Code,Wh_Code from User_Registration 
	      --   LEFT JOIN tbl_Store_Master ON User_Registration.Store_ID=tbl_Store_Master.Store_ID
	      --   LEFT JOIN tbl_Warehouse_Mst WM on User_Registration.WH_ID=WM.WH_ID
       --      where User_Name=@User_Name and Password=@Password 

	   		 select distinct User_ID, User_Name, Role_ID, User_Type, SM.Store_ID, SM.Store_Code, SM.Store_Name, WM.WH_ID, Wh_Code, Wh_Name 
			 ,Is_Created_By_Admin_Flag,ur.Is_Status from User_Registration ur
			 LEFT JOIN tbl_Store_Master SM ON UR.Store_ID=SM.Store_ID
			 LEFT JOIN tbl_Warehouse_Mst WM on UR.WH_ID=WM.WH_ID
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
	--SELECT Distinct Store_ID,Store_Code,
	--Store_Name,case when Is_Status=1 then 'Active' else 'In-Active' End as Status 
	--from tbl_Store_Master 

	--SELECT SM.Store_ID, SM.Store_Code, SM.Store_Name, SM.State_ID, STM.State_Name, CM.City_ID, CM.City_Name, SM.SM_ID, ESM.Emp_Name AS SM_NAME, SM.AM_ID, EAM.Emp_Name AS AM_NAME,
 --   SM.ZFM_ID, EZFM.Emp_Name AS ZFM_NAME, SM.LP_ID, ELP.Emp_Name AS LP_NAME,
 --   CASE WHEN SM.Is_Status = 1 THEN 'Active' ELSE 'In-Active' END AS Status
	--FROM dbo.tbl_Store_Master SM LEFT JOIN dbo.tbl_State_Mst STM ON STM.State_ID = SM.State_ID
	--LEFT JOIN dbo.tbl_City_Mst CM ON CM.City_ID = SM.City_ID
	--LEFT JOIN dbo.tbl_Employee_Mst ESM ON ESM.Emp_ID = SM.SM_ID
	--LEFT JOIN dbo.tbl_Employee_Mst EAM ON EAM.Emp_ID = SM.AM_ID
	--LEFT JOIN dbo.tbl_Employee_Mst EZFM ON EZFM.Emp_ID = SM.ZFM_ID
	--LEFT JOIN dbo.tbl_Employee_Mst ELP ON ELP.Emp_ID = SM.LP_ID;

	SELECT SM.Store_ID, SM.Store_Code, SM.Store_Name, SM.State_ID, STM.State_Name, CM.City_ID, CM.City_Name, SM.SM_ID,
	USM.User_Name AS SM_NAME, SM.AM_ID, UAM.User_Name AS AM_NAME,
    SM.ZFM_ID, UZFM.User_Name AS ZFM_NAME, SM.LP_ID, ULP.User_Name AS LP_NAME,
    CASE WHEN SM.Is_Status = 1 THEN 'Active' ELSE 'In-Active' END AS Status
	FROM dbo.tbl_Store_Master SM LEFT JOIN dbo.tbl_State_Mst STM ON STM.State_ID = SM.State_ID
	LEFT JOIN dbo.tbl_City_Mst CM ON CM.City_ID = SM.City_ID
	LEFT JOIN dbo.User_Registration USM ON USM.User_ID = SM.SM_ID
	LEFT JOIN dbo.User_Registration UAM ON UAM.User_ID = SM.AM_ID
	LEFT JOIN dbo.User_Registration UZFM ON UZFM.User_ID = SM.ZFM_ID
	LEFT JOIN dbo.User_Registration ULP ON ULP.User_ID = SM.LP_ID;

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
  --   SELECT @Store_ID = SM.Store_ID,@WH_ID = WM.WH_ID FROM User_Registration UR
  --   LEFT JOIN tbl_Store_Master SM ON UR.Store_ID = SM.Store_ID
  --   LEFT JOIN tbl_Warehouse_Mst WM ON UR.WH_ID = WM.WH_ID
  --   WHERE UR.User_ID = @User_ID

	 --SELECT User_ID,User_Name,Password,User_Type,UR.Store_ID,ISNULL(Store_Name,'NA') AS 'Store_Name',ISNULL(WM.Wh_Name,'NA') as 'Warehouse_Name',
	 -- case when UR.Is_Status=1 then 'Active' else 'In-active' End as Status FROM   dbo.User_Registration UR 
	 -- LEFT JOIN tbl_Store_Master SM on UR.Store_ID=SM.Store_ID
	 -- LEFT JOIN tbl_Warehouse_Mst WM on UR.WH_ID=WM.WH_ID
	 -- WHERE(
  --          (@User_Type = 'Store Admin' AND UR.User_Type IN ('Store', 'Store Admin') AND UR.Store_ID = @Store_ID)
  --          OR
  --          (@User_Type = 'Warehouse Admin' AND UR.User_Type IN ('Warehouse', 'Warehouse Admin') AND UR.WH_ID = @WH_ID)
  --          OR
  --          (@User_Type = 'Super Admin')
  --         );

   --   select User_ID, User_Name, Password, Role_ID, User_Type, UR.Store_ID, ISNULL(SM.Store_Name,'NA') as 'STORE_NAME', 
	  --UR.WH_ID, IsNull(Wh_Name,'NA') as 'WH_NAME', UR.Emp_ID, Emp_Code +' - '+ Emp_Name as 'EMP_NAME', 
	  --UR.User_Email_ID, UR.Email_Required_Flag, --added on 29/08/2026 by yash 
	  --case when UR.Is_Status=1 then 'Active' else 'In-active' End as Status from User_Registration UR 
	  --left join tbl_Store_Master SM on UR.Store_ID=SM.Store_ID
	  --left join tbl_Warehouse_Mst WM on UR.WH_ID=WM.WH_ID
	  --left join tbl_Employee_Mst EM on ur.Emp_ID=EM.Emp_ID
	  --order by User_Name


       DECLARE @LoginUserID INT,@LoginRoleID INT,@LoginStoreID INT,@LoginWHID INT;

	   SELECT @LoginUserID = UR.User_ID,@LoginRoleID = UR.Role_ID, @LoginStoreID = UR.Store_ID,@LoginWHID = UR.WH_ID
	   FROM dbo.User_Registration UR WHERE UR.User_ID=@User_ID ;


        SELECT User_ID, User_Name, Password,RM.Role_ID, User_Type, UR.Store_ID, ISNULL(SM.Store_Name,'NA') as 'STORE_NAME', 
	    UR.WH_ID, IsNull(Wh_Name,'NA') as 'WH_NAME',UR.User_Email_ID, UR.Email_Required_Flag,case when UR.Is_Status=1 then 'Active' else 'In-active' End as Status 
		FROM dbo.User_Registration UR
	    LEFT JOIN dbo.Role_Master RM ON RM.Role_ID = UR.Role_ID
		LEFT JOIN dbo.tbl_Store_Master SM ON SM.Store_ID = UR.Store_ID
		LEFT JOIN dbo.tbl_Warehouse_Mst WM ON WM.WH_ID = UR.WH_ID WHERE UR.IS_Status = '1'
        AND
	    (
			@LoginRoleID = 4
			OR
			(@LoginRoleID = 1 AND UR.Store_ID IN (SELECT S.Store_ID FROM dbo.tbl_Store_Master S WHERE S.AM_ID = @LoginUserID AND S.Is_Status = '1'))
			OR
			(@LoginRoleID = 2 AND UR.Store_ID IN (SELECT S.Store_ID FROM dbo.tbl_Store_Master S WHERE S.ZFM_ID = @LoginUserID AND S.Is_Status = '1'))
			OR
			(@LoginRoleID = 3 AND UR.Store_ID IN (SELECT S.Store_ID FROM dbo.tbl_Store_Master S WHERE S.LP_ID = @LoginUserID AND S.Is_Status = '1'))
			OR
			(@LoginRoleID = 5 AND UR.Store_ID = @LoginStoreID)
			OR
			(@LoginRoleID = 6 AND UR.User_ID = @LoginUserID)
			OR
			(@LoginRoleID = 7 AND UR.User_ID = @LoginUserID)
			OR
			(@LoginRoleID = 8 AND UR.WH_ID = @LoginWHID)
		)  ORDER BY SM.Store_Code, UR.User_ID;
END


--ELSE IF(@Status='SP_DDL_StoreID')
--BEGIN
--   IF(@User_Type='Super Admin')
--   BEGIN
--	  SELECT Distinct Store_Name,Store_ID from tbl_Store_Master WHERE IS_STATUS =1
--   END
--   ELSE 
--   BEGIN
--      SELECT ISNULL(Store_Name,'NA') AS 'Store_Name',SM.Store_ID FROM  dbo.User_Registration UR LEFT JOIN tbl_Store_Master SM on UR.Store_ID=SM.Store_ID where UR.User_ID=@User_ID   
--   END
--END

ELSE IF(@Status='SP_DDL_StoreID') ------------------------------------------------------
BEGIN
   IF(@Role_ID=5 or @Role_ID=6)
   BEGIN
	  SELECT Distinct Store_ID as 'ID', Store_Name as 'Name', 'ST' as 'TYPE' from tbl_Store_Master WHERE IS_STATUS =1 order by Store_Name;
   END
   else if(@Role_ID=7 or @Role_ID=8)
   begin
	SELECT distinct WH_ID as 'ID', Wh_Name as 'Name', 'WH' as 'TYPE' from tbl_Warehouse_Mst where Is_Status=1 order by Wh_Name;
   end
END


ELSE IF(@Status='SP_DDL_ReaderID')
BEGIN
	SELECT distinct Reader_ID,Reader_MAC  from tbl_Reader_Mst WHERE IS_STATUS = 1
END

--ELSE IF(@Status='SP_DDL_WarehouseID')
--BEGIN
--   IF(@User_Type='Super Admin')
--   BEGIN
--	  SELECT distinct WH_ID,Wh_Name from tbl_Warehouse_Mst where Is_Status=1
--   END
--   ELSE
--   BEGIN
--    SELECT ISNULL(WM.Wh_Name,'NA') as 'Wh_Name',WM.WH_ID FROM  dbo.User_Registration UR LEFT JOIN tbl_Warehouse_Mst WM on UR.WH_ID=WM.WH_ID where UR.User_ID=@User_ID --and WM.Is_Status=1
--   END
--END

ELSE IF(@Status='SP_DDL_WarehouseID')---------------------------------------------------
BEGIN
   IF(@Role_ID=7 and @Role_ID=8)
   BEGIN
	  SELECT distinct WH_ID,Wh_Name from tbl_Warehouse_Mst where Is_Status=1
   END
END

ELSE IF(@Status='STORENAME_FOR_COUNTER_STATUS')
BEGIN
	SELECT DISTINCT dbo.[tbl_Device_Mst].Store_ID,dbo.[tbl_Store_Master].Store_Name FROM dbo.[tbl_Device_Mst] 
    JOIN dbo.[tbl_Store_Master] on dbo.[tbl_Device_Mst].Store_ID = dbo.[tbl_Store_Master].Store_ID
	WHERE (@CounterRoleid IN ('4','8') OR ( @CounterRoleid = '5' AND dbo.tbl_Store_Master.SM_ID = @CounterEmpID) OR (@CounterRoleid = '6'  AND dbo.tbl_Store_Master.Store_ID = @CounterStoreID)
	OR (@CounterRoleid = '1' AND dbo.tbl_Store_Master.AM_ID = @CounterEmpID)
        OR (@CounterRoleid = '2' AND dbo.tbl_Store_Master.ZFM_ID = @CounterEmpID) OR (@CounterRoleid = '3' AND dbo.tbl_Store_Master.LP_ID = @CounterEmpID)) 
        AND [dbo].[tbl_store_master].Is_Status='1'
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
			SET @Message = 'Warehouse Code / Warehouse Name already exists.';
		END
	End 

ELSE IF(@status='Update_tbl_warehouse_Master')
BEGIN
	IF EXISTS (SELECT 1 FROM dbo.[tbl_Warehouse_Mst] WHERE (Wh_Code = @Wh_Code OR Wh_Name=@Wh_Name) AND WH_ID != @WH_ID)
    BEGIN
        SET @Message = 'Warehouse Code / Warehouse Name already exists.'
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

ELSE IF(@Status='Bind_State_Id')
BEGIN
	SELECT DISTINCT State_ID, State_Name FROM dbo.[tbl_State_Mst];
END

ELSE IF(@Status='Bind_City_Id')
BEGIN
	SELECT DISTINCT City_ID, City_Name FROM dbo.[tbl_City_Mst] C JOIN dbo.[tbl_State_Mst] S ON C.State_ID=S.State_ID WHERE S.State_Name=@State_Name;
END

ELSE IF(@Status = 'Bind_Employee_By_Role')
BEGIN
    SELECT E.Emp_ID,E.Emp_Name FROM dbo.tbl_Employee_Mst E JOIN dbo.Role_Master R ON E.Emp_Role_ID = R.Role_ID WHERE R.Role_Name = @RoleName;
END

ELSE IF(@Status = 'Bind_AM_By_Role')
BEGIN    
	--select E.Emp_ID, E.Emp_Code +' - '+E.Emp_Name as 'EMP_NAME' from tbl_Employee_Mst E where Emp_Role_ID=1 and Is_Status='1' order by E.Emp_Name;
	select U.User_ID, U.User_Name  as 'USER_NAME' from User_Registration U where Role_ID=1 and Is_Status='1' order by U.User_Name;
END

--ELSE IF(@Status = 'Bind_ZM_By_Role')
ELSE IF(@Status = 'Bind_ZFM_By_Role')
BEGIN  
	--select E.Emp_ID, E.Emp_Code +' - '+E.Emp_Name as 'EMP_NAME' from tbl_Employee_Mst E where Emp_Role_ID=2 and Is_Status='1' order by E.Emp_Name;
	select U.User_ID, U.User_Name  as 'USER_NAME' from User_Registration U where Role_ID=2 and Is_Status='1' order by U.User_Name;
END

ELSE IF(@Status = 'Bind_LP_By_Role')
BEGIN    
	--select E.Emp_ID, E.Emp_Code +' - '+E.Emp_Name as 'EMP_NAME' from tbl_Employee_Mst E where Emp_Role_ID=3 and Is_Status='1' order by E.Emp_Name;
	select U.User_ID, U.User_Name  as 'USER_NAME' from User_Registration U where Role_ID=3 and Is_Status='1' order by U.User_Name;
END

ELSE IF(@Status = 'Bind_SM_By_Role')
BEGIN    
	--select E.Emp_ID, E.Emp_Code +' - '+E.Emp_Name as 'EMP_NAME' from tbl_Employee_Mst E where Emp_Role_ID=5 and Is_Status='1' order by E.Emp_Name;
	select U.User_ID, U.User_Name  as 'USER_NAME' from User_Registration U where Role_ID=5 and Is_Status='1' order by U.User_Name;
END

ELSE IF(@Status = 'Bind_Role_Id')
BEGIN
    SELECT Role_ID, Role_Name FROM dbo.Role_Master WHERE IS_Status='1' order by Role_ID;
END

ELSE IF(@Status = 'Bind_Role_By_EmpId')
BEGIN
    select Emp_Role_ID from tbl_Employee_Mst where Emp_ID=@Emp_ID and Is_Status='1'
END

ELSE IF(@status='Insert_tbl_Employee_Mst')
	BEGIN	
		IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_Employee_Mst] WHERE Emp_code =@Emp_Code)
		BEGIN	
			INSERT INTO [dbo].[tbl_Employee_Mst] (Emp_Code,Emp_Name,Emp_Email_ID,Emp_Role_ID,C_By) 
			VALUES (@Emp_Code,@Emp_Name,@MAIL_ID,@Role_ID,@User_ID);
			SET @Message = 'Record Insert successfully.';
		END
		ELSE
		BEGIN
			SET @Message = 'Employee Code already exists.';
		END
	End  

ELSE IF(@status='Update_tbl_Employee_Mst')
BEGIN
	IF EXISTS (SELECT 1 FROM [dbo].[tbl_Employee_Mst] WHERE (Emp_code =@Emp_Code AND Emp_Name=@Emp_Name) AND Emp_ID != @Emp_ID)
    BEGIN
        SET @Message = 'Emp Code / Emp Name already exists.'
    END
	ELSE
	BEGIN
		Update [dbo].[tbl_Employee_Mst]  Set 
		Emp_Code=@Emp_Code, Emp_Name=@Emp_Name, Emp_Email_ID=@MAIL_ID, Emp_Role_ID=@Role_ID, M_By=@User_ID, M_Dt=GETDATE()
		Where Emp_ID = @emp_ID;

		SET @Message = 'Updated Successfully.'
	END
END

ELSE IF(@status='Delete_tbl_Employee_Mst')
BEGIN
    If (select Is_Status from dbo.[tbl_Employee_Mst] where Emp_Id = @Emp_ID)='0'
	Begin
	   Update dbo.[tbl_Employee_Mst]  Set Is_Status='1' Where Emp_Id = @Emp_ID;
	end
	else If (select Is_Status from dbo.[tbl_Employee_Mst]  where Emp_Id = @Emp_ID)='1'
	begin
	   Update dbo.[tbl_Employee_Mst]  Set Is_Status='0' Where Emp_Id = @Emp_ID;
	end
END

ELSE IF(@Status = 'Bind_Employee_Grid')
BEGIN
    SELECT Emp_Id, Emp_Code, Emp_Name, Emp_Email_ID, Role_ID, Role_Name, case when E.Is_Status=1 then 'Active' else 'In-Active' End as Status FROM [dbo].[tbl_Employee_Mst] E JOIN dbo.Role_Master R ON E.Emp_Role_ID = R.Role_ID 
	order by Emp_Code;

	--SELECT E.Emp_Id, Emp_Code, Emp_Name, Emp_Email_ID, R.Role_ID, Role_Name, case when E.Is_Status=1 then 'Active' else 'In-Active' End as Status FROM tbl_Employee_Mst E
	--left JOIN User_Registration U ON E.Emp_ID = U.Emp_ID left JOIN Role_Master R ON E.Emp_Role_ID = R.Role_ID
	--WHERE @UserType = 'Super Admin' OR (@UserType = 'Store Admin' AND U.Store_ID = @Store_ID) OR (@UserType = 'Warehouse Admin' AND U.WH_ID = @WH_ID)
 --   ORDER BY E.Emp_Code;

END

ELSE IF(@Status = 'SP_DDL_EmployeeId')
BEGIN
    SELECT E.Emp_ID, E.Emp_Code+' - '+E.Emp_Name as 'EMP_NAME' FROM dbo.tbl_Employee_Mst E where Is_Status='1' --and E.Emp_ID not in(select UR.Emp_ID from User_Registration UR where UR.Emp_ID=E.Emp_ID) 
	order by E.Emp_Code;
END

------------------------------------------------for Store Floor Master----------------------------------------------
If(@status='Insert_tbl_Store_Floor_Master')
	Begin	
		IF NOT EXISTS (SELECT 1 FROM tbl_Store_Floor_Mst WHERE Store_ID = @Store_ID AND Store_Floor=@Store_Floor)
		BEGIN	
			INSERT INTO tbl_Store_Floor_Mst(Store_ID,Store_Floor,C_By) 
			VALUES (@Store_ID, @Store_Floor,@Entry_By);
			SET @Message = 'Record Insert successfully.';
		END
		ELSE
		BEGIN
			SET @Message = 'Store Floor already exists.';
		END
	End   
		
ELSE IF(@status='Update_tbl_Store_Floor_Master')
BEGIN
	IF EXISTS (SELECT 1 FROM tbl_Store_Floor_Mst WHERE (Store_ID = @Store_ID OR Store_Floor=@Store_Floor) AND Store_Floor_ID != @Store_Floor_ID)
    BEGIN
        SET @Message = 'Store Floor already exists.'
    END
	ELSE
	BEGIN
		Update tbl_Store_Floor_Mst Set 
		Store_ID=@Store_ID,Store_Floor=@Store_Floor,C_Dt=GETDATE(),Is_Status=1,C_By=@Modify_By
		Where Store_Floor_ID = @Store_Floor_ID

		SET @Message = 'Updated Successfully.'
	END
END

ELSE IF(@status='Delete_tbl_Store_Floor_Master')
BEGIN
    --Update tbl_Store_Master Set Is_Status='0' Where Store_ID = @Store_ID
	If (select Is_Status from dbo.[tbl_Store_Floor_Mst] where Store_Floor_ID = @Store_Floor_ID)='0'
	Begin
	   Update tbl_Store_Floor_Mst Set Is_Status='1' Where Store_Floor_ID = @Store_Floor_ID
	end
	else If (select Is_Status from dbo.[tbl_Store_Floor_Mst] where Store_Floor_ID = @Store_Floor_ID)='1'
	begin
	   Update tbl_Store_Floor_Mst Set Is_Status='0' Where Store_Floor_ID = @Store_Floor_ID
	end
END

ELSE IF(@Status='SP_Bind_DDL_StoreID') ------------------------------------------------------
BEGIN
	  SELECT Distinct Store_ID as 'Store_ID', Store_Name as 'Store_Name' from tbl_Store_Master WHERE IS_STATUS =1 
	  AND (@CounterRoleid = '4' OR ( @CounterRoleid = '5' AND dbo.tbl_Store_Master.SM_ID = @CounterEmpID))
	  --order by Store_Name;
END

ELSE IF(@Status='SP_Bind_Store_Floor_Master')
BEGIN

	--DECLARE @LoginUserID INT,@LoginRoleID INT,@LoginStoreID INT,@LoginWHID INT;

	   SELECT @LoginUserID = UR.User_ID,@LoginRoleID = UR.Role_ID, @LoginStoreID = UR.Store_ID,@LoginWHID = UR.WH_ID
	   FROM dbo.User_Registration UR WHERE UR.User_ID=@User_ID ;

	SELECT SFM.Store_Floor_ID,ISNULL(SM.Store_Name,'NA') as 'STORE_NAME',SFM.Store_Floor,case when SFM.Is_Status=1 then 'Active' else 'In-Active' End as Status  
	from tbl_Store_Floor_Mst SFM
	--LEFT JOIN dbo.User_Registration UR ON SFM.Store_ID = UR.Store_ID
	LEFT JOIN dbo.tbl_Store_Master SM ON SM.Store_ID = SFM.Store_ID WHERE-- SFM.IS_Status = '1'
	--AND
	    (
			 @LoginRoleID = 4
			 OR
			(@LoginRoleID = 5 AND SFM.Store_ID =@LoginStoreID)
			
		)  ORDER BY SFM.Store_ID;

END
------------------------------------------------for Store Floor Master----------------------------------------------

END----last


