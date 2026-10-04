CREATE OR ALTER PROCEDURE dbo.sp_monthly_profile_get
    @tenantid   UNIQUEIDENTIFIER,
    @yearmonth  CHAR(7)
AS
BEGIN
    SET NOCOUNT ON;

    IF @yearmonth IS NULL
       OR @yearmonth NOT LIKE '[0-9][0-9][0-9][0-9]-[0-1][0-9]'
       OR SUBSTRING(@yearmonth, 6, 2) NOT BETWEEN '01' AND '12'
        THROW 50400, 'Invalid year-month. Use YYYY-MM.', 1;

    -- Result set 1: existing profile (0 or 1 row)
    SELECT
        mp.monthlyprofileid,
        mp.yearmonth,
        mp.profilestatus,
        mp.opdrevenue,
        mp.ipdrevenue,
        mp.totalrevenue,
        mp.totalexpenses,
        mp.profit,
        mp.note,
        mp.reconciledat,
        mp.reconciledby,
        LTRIM(RTRIM(CONCAT(ISNULL(u.firstname, N''), N' ', ISNULL(u.lastname, N'')))) AS reconcilername,
        mp.createdat
    FROM dbo.monthly_profiles mp
    LEFT JOIN dbo.users u
        ON u.userid = mp.reconciledby
       AND u.tenantid = mp.tenantid
    WHERE mp.tenantid = @tenantid
      AND mp.yearmonth = @yearmonth;

    -- Result set 2: live preview totals
    EXEC dbo.sp_monthly_profile_preview @tenantid = @tenantid, @yearmonth = @yearmonth;
END
GO
