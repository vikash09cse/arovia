CREATE OR ALTER PROCEDURE dbo.sp_visit_get_active_doctors
    @tenantid      UNIQUEIDENTIFIER,
    @departmentid  UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.userid,
        u.firstname,
        u.lastname,
        u.email,
        u.departmentid,
        dep.name AS departmentname
    FROM dbo.users u
    LEFT JOIN dbo.departments dep
        ON dep.departmentid = u.departmentid
       AND dep.tenantid = u.tenantid
    WHERE u.tenantid = @tenantid
      AND u.usertype = 3
      AND u.userstatus = 1
      AND u.isdeleted = 0
      AND (@departmentid IS NULL OR u.departmentid = @departmentid)
    ORDER BY u.lastname, u.firstname;
END
GO
