CREATE OR ALTER PROCEDURE dbo.sp_department_set_status
    @tenantid          UNIQUEIDENTIFIER,
    @departmentid      UNIQUEIDENTIFIER,
    @departmentstatus  TINYINT,
    @actorid           UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @departmentstatus NOT IN (1, 2)
        THROW 50400, 'Invalid department status.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.departments d
        WHERE d.tenantid = @tenantid
          AND d.departmentid = @departmentid)
        THROW 50404, 'Department not found.', 1;

    UPDATE dbo.departments
    SET departmentstatus = @departmentstatus,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND departmentid = @departmentid;

    SELECT CAST(1 AS BIT) AS success;
END
GO
