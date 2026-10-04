CREATE OR ALTER PROCEDURE dbo.sp_diagnosis_master_get_by_id
    @tenantid           UNIQUEIDENTIFIER,
    @diagnosismasterid  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        dm.diagnosismasterid,
        dm.code,
        dm.name,
        dm.packjson,
        dm.sortorder,
        dm.isactive,
        dm.createdat,
        dm.updatedat
    FROM dbo.diagnosis_masters dm
    WHERE dm.tenantid = @tenantid
      AND dm.diagnosismasterid = @diagnosismasterid;
END
GO
