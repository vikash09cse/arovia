CREATE OR ALTER PROCEDURE dbo.sp_expense_delete
    @tenantid   UNIQUEIDENTIFIER,
    @expenseid  UNIQUEIDENTIFIER,
    @actorid    UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @expenseon DATE;

    SELECT @expenseon = e.expenseon
    FROM dbo.expenses e
    WHERE e.tenantid = @tenantid
      AND e.expenseid = @expenseid
      AND e.isdeleted = 0;

    IF @expenseon IS NULL
        THROW 50404, 'Expense not found.', 1;

    DECLARE @yearmonth CHAR(7) = CONVERT(CHAR(7), @expenseon, 23);

    IF EXISTS (
        SELECT 1
        FROM dbo.monthly_profiles mp
        WHERE mp.tenantid = @tenantid
          AND mp.yearmonth = @yearmonth
          AND mp.profilestatus = 2)
        THROW 50409, 'This month is closed. Reopen it before deleting expenses.', 1;

    UPDATE dbo.expenses
    SET isdeleted = 1,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND expenseid = @expenseid
      AND isdeleted = 0;
END
GO
