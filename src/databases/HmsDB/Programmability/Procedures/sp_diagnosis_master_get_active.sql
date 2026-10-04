CREATE OR ALTER PROCEDURE dbo.sp_diagnosis_master_get_active
    @tenantid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        dm.diagnosismasterid,
        dm.code,
        dm.name,
        dm.packjson,
        dm.sortorder
    FROM dbo.diagnosis_masters dm
    WHERE dm.tenantid = @tenantid
      AND dm.isactive = 1
    ORDER BY dm.sortorder, dm.name;
END
GO
