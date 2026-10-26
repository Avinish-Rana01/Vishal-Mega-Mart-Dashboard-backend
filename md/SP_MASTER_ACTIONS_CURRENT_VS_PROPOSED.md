# SP_Master Actions: Current vs. Proposed Architecture

> **Database Scope**: `latest_VMM_RFID_RETAIL_SOLUTION` / `VMM_RFID_RETAIL_SOLUTION` (Production & Dev)  
> **Target Stored Procedure**: `dbo.SP_Master`  
> **Core Objective**: Permanently eliminate the Admin session wipe bug, solve the multi-store role promotion issue (ZFM to LP), and provide a unified, secure Password Change action.

---

## 1. Action 1: `Update_tbl_Store_Master`

### Problem in Current Code:
Line 102 (`UPDATE User_Registration SET Store_ID=NULL, User_Type=NULL, Role_ID=NULL WHERE User_ID = @User_ID;`) was added with the intention of unlinking an old user, but **`@User_ID` is the ID of the logged-in Admin saving the store**. Whenever any store is saved, **the Admin's own profile is corrupted to `NULL / NULL`**, locking them out or stripping Super Admin permissions.

---

### What It Looks Like NOW (Current):
```sql
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

        -- ❌ CULPRIT LINE: Wipes the logged-in Admin who clicked Save!
        Update User_Registration Set Store_ID=NULL,User_Type=NULL,Role_ID=NULL where User_ID = @User_ID;

        SET @Message = 'Updated Successfully.'
    END
END
```

---

### What It Will Look Like AFTER Changes (Proposed):
```sql
ELSE IF(@status='Update_tbl_Store_Master')
BEGIN
    IF EXISTS (SELECT 1 FROM tbl_Store_Master WHERE (Store_Code = @Store_Code OR Store_Name=@Store_Name) AND Store_ID <> @Store_ID)
    BEGIN
        SET @Message = 'Store Code / Store Name already exists.';
    END
    ELSE IF EXISTS (SELECT 1 FROM tbl_Store_Master WHERE SM_ID = @SM_ID AND Store_ID <> @Store_ID)
    BEGIN
        SET @Message = 'Store Manager is already assigned to another store.';
    END
    ELSE
    BEGIN
        -- 1. If this store previously had a different Store Manager, detach that old SM's Store_ID safely:
        UPDATE dbo.User_Registration 
        SET Store_ID = NULL 
        WHERE Store_ID = @Store_ID 
          AND User_ID <> @SM_ID 
          AND Role_ID = 5; -- Store Admin only

        -- 2. Update the Store Master record with new assignments:
        UPDATE dbo.tbl_Store_Master SET 
            Store_Code  = @Store_Code,
            Store_Name  = @Store_Name,
            Modify_Date = GETDATE(),
            Is_Status   = 1,
            Modify_By   = @Modify_By,
            SM_MAIL_ID  = @MAIL_ID,
            State_ID    = @State_ID,
            City_ID     = @City_ID,
            SM_ID       = @SM_ID,
            AM_ID       = @AM_ID,
            ZFM_ID      = @ZFM_ID,
            LP_ID       = @LP_ID
        WHERE Store_ID = @Store_ID;

        -- 3. Link the new Store Manager's user account to this Store:
        IF (@SM_ID IS NOT NULL AND @SM_ID > 0)
        BEGIN
            UPDATE dbo.User_Registration SET Store_ID = @Store_ID WHERE User_ID = @SM_ID;
        END

        SET @Message = 'Updated Successfully.';
    END
END
```

---

## 2. Action 2: `Update_user_registration`

### Problem in Current Code:
1. **Dangling Store Assignments**: When Sandeep is changed from ZFM to LP in the "User Master" screen, `User_Registration` changes his role, but **`tbl_Store_Master` still has Sandeep in `ZFM_ID` for all 7 stores**. There is zero cleanup logic.
2. **Empty Password Wipe**: If the Admin edits Sandeep's profile and leaves the password field blank, `Password = @Password` overwrites his password with an empty string `""`, locking him out.

---

### What It Looks Like NOW (Current):
```sql
ELSE IF(@status='Update_user_registration')
BEGIN
    IF (@Role_ID IS NULL OR @Role_ID = 0) AND (@User_Type IS NOT NULL AND @User_Type <> '')
    BEGIN
        SELECT @Role_ID = Role_ID FROM dbo.Role_Master WHERE Role_Name = @User_Type;
    END

    Update User_Registration Set 
        user_name=@User_Name,
        Password=@Password, -- ❌ DANGER: Overwrites password with "" if left blank in form!
        User_Type=@User_Type,
        Store_ID = CASE WHEN @Store_ID = 0 THEN NULL ELSE @Store_ID END,
        WH_ID = CASE WHEN @WH_ID = 0 THEN NULL ELSE @WH_ID END,
        Is_Status=1,
        Modify_By=@Modify_By,
        Modify_Date=GETDATE(),
        Role_ID=@Role_ID, 
        User_Email_ID=@MAIL_ID, 
        Email_Required_Flag=@Email_Required_Flag
    Where User_ID = @User_ID;
END
```

---

### What It Will Look Like AFTER Changes (Proposed):
```sql
ELSE IF(@status='Update_user_registration')
BEGIN
    -- 1. Grab both Role_ID and User_Type BEFORE the update:
    DECLARE @Old_Role_ID INT, @Old_User_Type VARCHAR(50);
    SELECT @Old_Role_ID = Role_ID, @Old_User_Type = User_Type 
    FROM dbo.User_Registration 
    WHERE User_ID = @User_ID;

    -- Resolve new Role_ID if caller only passed @User_Type (or vice versa):
    IF (@Role_ID = 0 OR @Role_ID IS NULL) AND (@User_Type <> '')
    BEGIN
        SELECT @Role_ID = Role_ID FROM dbo.Role_Master WHERE UPPER(Role_Name) = UPPER(@User_Type);
    END

    -- 2. If leaving ZFM role (Role 2): Automatically clear ZFM_ID in all stores!
    IF (@Old_Role_ID = 2 OR UPPER(@Old_User_Type) = 'ZFM') AND (@Role_ID <> 2)
    BEGIN
        UPDATE dbo.tbl_Store_Master SET ZFM_ID = NULL WHERE ZFM_ID = @User_ID;
    END

    -- 3. If leaving Area Manager role (Role 1): Automatically clear AM_ID in all stores!
    IF (@Old_Role_ID = 1 OR UPPER(@Old_User_Type) = 'AREA MANAGER') AND (@Role_ID <> 1)
    BEGIN
        UPDATE dbo.tbl_Store_Master SET AM_ID = NULL WHERE AM_ID = @User_ID;
    END

    -- 4. If leaving LP role (Role 3): Automatically clear LP_ID in all stores!
    IF (@Old_Role_ID = 3 OR UPPER(@Old_User_Type) = 'LP') AND (@Role_ID <> 3)
    BEGIN
        UPDATE dbo.tbl_Store_Master SET LP_ID = NULL WHERE LP_ID = @User_ID;
    END

    -- 5. If leaving Store Admin role (Role 5): Automatically clear SM_ID in all stores!
    IF (@Old_Role_ID = 5 OR UPPER(@Old_User_Type) = 'STORE ADMIN') AND (@Role_ID <> 5)
    BEGIN
        UPDATE dbo.tbl_Store_Master SET SM_ID = NULL WHERE SM_ID = @User_ID;
    END

    -- 6. Perform the user update (protecting password from blank overwrite):
    UPDATE dbo.User_Registration SET 
        user_name           = @User_Name,
        Password            = CASE WHEN @Password IS NOT NULL AND @Password <> '' THEN @Password ELSE Password END,
        User_Type           = @User_Type,
        Role_ID             = @Role_ID,
        Store_ID            = CASE WHEN @Store_ID = 0 THEN NULL ELSE @Store_ID END,
        WH_ID               = CASE WHEN @WH_ID = 0 THEN NULL ELSE @WH_ID END,
        User_Email_ID       = @MAIL_ID,
        Email_Required_Flag = @Email_Required_Flag,
        Modify_By           = @Modify_By,
        Modify_Date         = GETDATE(),
        Is_Status           = 1
    WHERE User_ID = @User_ID;

    SET @Message = 'User updated successfully.';
END
```

---

## 3. Action 3: `Change_Password` / `Update_User_Password_First_Time_Login`

### Problem in Current Code:
1. **Infinite Reset Loop**: Colleague's action `Update_User_Password_First_Time_Login` sets `Is_Created_By_Admin_Flag = 0`, but does NOT update `Is_Login_Status = '1'`. Because `Is_Login_Status` stays `'0'`, the backend keeps popping the password change dialog on every login!
2. **Zero Password Verification**: Anyone who sends a `User_ID` can overwrite the password without proving they know the current password.
3. **Username Wipe Risk**: Overwrites `user_name = @User_Name`, which will blank out or corrupt the username if null or empty.

---

### Unified, Production-Ready Action (Proposed):
```sql
ELSE IF (@status = 'Change_Password' OR @status = 'Update_User_Password_First_Time_Login')
BEGIN
    -- 1. Determine target new password (handles both @New_Password and legacy @Password)
    IF (@New_Password IS NULL OR @New_Password = '')
    BEGIN
        IF (@status = 'Update_User_Password_First_Time_Login' AND @Password IS NOT NULL AND @Password <> '')
            SET @New_Password = @Password;
        ELSE
        BEGIN
            SET @Message = 'New password cannot be empty.';
            RETURN;
        END
    END

    -- 2. Validate current password input
    IF (@Password IS NULL OR @Password = '')
    BEGIN
        SET @Message = 'Current password is required.';
        RETURN;
    END

    -- 3. Resolve target User ID if only username was provided
    IF (@User_ID IS NULL OR @User_ID = 0)
    BEGIN
        SELECT TOP 1 @User_ID = User_ID 
        FROM dbo.User_Registration 
        WHERE User_Name = LTRIM(RTRIM(@User_Name));
    END

    IF (@User_ID IS NULL OR @User_ID = 0)
    BEGIN
        SET @Message = 'User account not found.';
        RETURN;
    END

    -- 4. Atomic Update:
    --    - Updates password
    --    - Sets Is_Login_Status = '1' (Breaks the infinite login popup loop!)
    --    - Sets Is_Created_By_Admin_Flag = 0 (Clears admin creation flag)
    --    - Never touches or wipes User_Name
    --    - Verifies current password & active account status
    UPDATE dbo.User_Registration
    SET Password                 = @New_Password,
        Is_Login_Status          = '1',
        Is_Created_By_Admin_Flag = 0,
        Modify_By                = CASE WHEN @Modify_By > 0 THEN @Modify_By ELSE @User_ID END,
        Modify_Date              = GETDATE()
    WHERE User_ID = @User_ID
      AND Password = @Password
      AND (Is_Status = '1' OR Is_Status IS NULL);

    -- 5. Verify the update result immediately
    IF (@@ROWCOUNT = 0)
    BEGIN
        SET @Message = 'Current password is incorrect or user account is not active.';
    END
    ELSE
    BEGIN
        SET @Message = 'Password changed successfully.';
    END
END
```

---

## 4. Summary of Improvements Across All 3 Actions

| Metric | Before Changes | After Changes |
| :--- | :--- | :--- |
| **Admin Account Safety** | ❌ Admin wiped to `NULL/NULL` whenever saving stores. | ✅ **100% Protected.** Admin account is never touched. |
| **Multi-Store Role Transitions** | ❌ Sandeep remains ZFM in 7 stores after promotion. | ✅ **Automated.** Automatically sets `ZFM_ID = NULL` across all stores. |
| **Password Change Loop** | ❌ User trapped in infinite reset loop (`Is_Login_Status = '0'`). | ✅ **Resolved.** Sets both `Is_Login_Status = '1'` and `Flag = 0`. |
| **Blank Password Form Safety** | ❌ Wipes password to `""` if left blank on edit form. | ✅ **Protected.** Retains existing password. |
| **API Feedback** | ❌ Empty string or silent failure. | ✅ Returns clear `@Message` string to C# and frontend toast. |
