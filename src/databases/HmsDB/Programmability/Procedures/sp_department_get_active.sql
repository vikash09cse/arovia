CREATE OR ALTER PROCEDURE dbo.sp_department_get_active
    @tenantid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.departmentid,
        d.name
    FROM dbo.departments d
    WHERE d.tenantid = @tenantid
      AND d.departmentstatus = 1
    ORDER BY d.name;
END
GO
