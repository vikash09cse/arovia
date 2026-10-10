CREATE OR ALTER PROCEDURE dbo.sp_get_tenant_user_by_id
    @tenantid UNIQUEIDENTIFIER,
    @userid   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.userid,
        u.email,
        u.firstname,
        u.lastname,
        u.designation,
        u.phonenumber,
        u.emergencycontactnumber,
        u.usertype AS role,
        u.userstatus AS status,
        u.lastloginat,
        u.createdat,
        s.monthlysalary
    FROM dbo.users u
    LEFT JOIN dbo.user_salaries s
        ON s.tenantid = u.tenantid
       AND s.userid = u.userid
       AND s.isdeleted = 0
       AND s.effectiveto IS NULL
    WHERE u.tenantid = @tenantid
      AND u.userid = @userid
      AND u.isdeleted = 0;
END
GO
