CREATE OR ALTER PROCEDURE dbo.sp_department_get_by_id
    @tenantid     UNIQUEIDENTIFIER,
    @departmentid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.departmentid,
        d.name,
        d.departmentstatus,
        d.createdat,
        d.updatedat
    FROM dbo.departments d
    WHERE d.tenantid = @tenantid
      AND d.departmentid = @departmentid;
END
GO
