CREATE OR ALTER PROCEDURE dbo.sp_diagnosis_master_delete
    @tenantid           UNIQUEIDENTIFIER,
    @diagnosismasterid  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.diagnosis_masters dm
        WHERE dm.tenantid = @tenantid
          AND dm.diagnosismasterid = @diagnosismasterid)
        THROW 50404, 'Diagnosis not found.', 1;

    DELETE FROM dbo.diagnosis_masters
    WHERE tenantid = @tenantid
      AND diagnosismasterid = @diagnosismasterid;
END
GO
