CREATE OR ALTER PROCEDURE dbo.sp_monthly_profile_get_history
    @tenantid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

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
    ORDER BY mp.yearmonth DESC;
END
GO
