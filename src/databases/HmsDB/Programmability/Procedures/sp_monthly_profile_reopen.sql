CREATE OR ALTER PROCEDURE dbo.sp_monthly_profile_reopen
    @tenantid   UNIQUEIDENTIFIER,
    @yearmonth  CHAR(7),
    @actorid    UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @yearmonth IS NULL
       OR @yearmonth NOT LIKE '[0-9][0-9][0-9][0-9]-[0-1][0-9]'
       OR SUBSTRING(@yearmonth, 6, 2) NOT BETWEEN '01' AND '12'
        THROW 50400, 'Invalid year-month. Use YYYY-MM.', 1;

    -- Future: block reopen when paid investor payouts exist for this month.

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.monthly_profiles mp
        WHERE mp.tenantid = @tenantid
          AND mp.yearmonth = @yearmonth
          AND mp.profilestatus = 2)
        THROW 50409, 'Month is not reconciled.', 1;

    UPDATE dbo.monthly_profiles
    SET profilestatus = 1,
        reconciledat = NULL,
        reconciledby = NULL,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND yearmonth = @yearmonth
      AND profilestatus = 2;
END
GO
