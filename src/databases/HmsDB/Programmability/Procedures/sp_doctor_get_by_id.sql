CREATE OR ALTER PROCEDURE dbo.sp_doctor_get_by_id
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
        u.usertype AS role,
        u.userstatus AS status,
        u.departmentid,
        dep.name AS departmentname,
        u.lastloginat,
        u.createdat
    FROM dbo.users u
    LEFT JOIN dbo.departments dep
        ON dep.departmentid = u.departmentid
       AND dep.tenantid = u.tenantid
    WHERE u.tenantid = @tenantid
      AND u.userid = @userid
      AND u.usertype = 3
      AND u.isdeleted = 0;
END
GO
