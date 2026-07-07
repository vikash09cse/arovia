CREATE OR ALTER PROCEDURE dbo.sp_visit_get_active_doctors
    @tenantid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.userid,
        u.firstname,
        u.lastname,
        u.email
    FROM dbo.users u
    WHERE u.tenantid = @tenantid
      AND u.usertype = 3
      AND u.userstatus = 1
      AND u.isdeleted = 0
    ORDER BY u.lastname, u.firstname;
END
GO
