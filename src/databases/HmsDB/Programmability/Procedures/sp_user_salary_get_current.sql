CREATE OR ALTER PROCEDURE dbo.sp_user_salary_get_current
    @tenantid UNIQUEIDENTIFIER,
    @userid   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        s.usersalaryid,
        s.userid,
        s.monthlysalary,
        s.effectivefrom,
        s.effectiveto,
        s.notes,
        s.createdat,
        s.createdby
    FROM dbo.user_salaries s
    WHERE s.tenantid = @tenantid
      AND s.userid = @userid
      AND s.isdeleted = 0
      AND s.effectiveto IS NULL
    ORDER BY s.effectivefrom DESC, s.createdat DESC;
END
GO
