CREATE OR ALTER PROCEDURE dbo.sp_expense_save
    @tenantid    UNIQUEIDENTIFIER,
    @amount      DECIMAL(18, 2),
    @expenseon   DATE,
    @category    NVARCHAR(50),
    @note        NVARCHAR(500) = NULL,
    @staffuserid UNIQUEIDENTIFIER = NULL,
    @actorid     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @amount IS NULL OR @amount <= 0
        THROW 50400, 'Amount must be greater than 0.', 1;

    IF @expenseon IS NULL
        THROW 50400, 'Expense date is required.', 1;

    DECLARE @trimmedCategory NVARCHAR(50) = LTRIM(RTRIM(@category));
    IF @trimmedCategory IS NULL OR @trimmedCategory = N''
        THROW 50400, 'Category is required.', 1;

    IF @trimmedCategory NOT IN (N'Rent', N'Salaries', N'Utilities', N'Supplies', N'Staff advance', N'Other')
        THROW 50400, 'Invalid expense category.', 1;

    IF @trimmedCategory = N'Staff advance'
    BEGIN
        IF @staffuserid IS NULL
            THROW 50400, 'Select a staff member for the advance.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.users u
            WHERE u.tenantid = @tenantid
              AND u.userid = @staffuserid
              AND u.isdeleted = 0
              AND u.userstatus = 1
              AND u.usertype IN (1, 2, 3))
            THROW 50400, 'Selected staff member is not available.', 1;
    END
    ELSE
        SET @staffuserid = NULL;

    DECLARE @yearmonth CHAR(7) = CONVERT(CHAR(7), @expenseon, 23);

    IF EXISTS (
        SELECT 1
        FROM dbo.monthly_profiles mp
        WHERE mp.tenantid = @tenantid
          AND mp.yearmonth = @yearmonth
          AND mp.profilestatus = 2)
        THROW 50409, 'This month is closed. Reopen it before adding expenses.', 1;

    DECLARE @noteTrimmed NVARCHAR(500) = NULLIF(LTRIM(RTRIM(@note)), N'');
    DECLARE @expenseid UNIQUEIDENTIFIER = NEWID();

    INSERT INTO dbo.expenses (
        expenseid, tenantid, amount, expenseon, category, note, staffuserid,
        createdby, updatedby)
    VALUES (
        @expenseid, @tenantid, @amount, @expenseon, @trimmedCategory, @noteTrimmed, @staffuserid,
        @actorid, @actorid);

    SELECT @expenseid AS expenseid;
END
GO
