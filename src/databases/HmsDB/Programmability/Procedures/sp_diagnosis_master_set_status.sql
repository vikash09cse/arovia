CREATE OR ALTER PROCEDURE dbo.sp_diagnosis_master_set_status
    @tenantid           UNIQUEIDENTIFIER,
    @diagnosismasterid  UNIQUEIDENTIFIER,
    @isactive           BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.diagnosis_masters dm
        WHERE dm.tenantid = @tenantid
          AND dm.diagnosismasterid = @diagnosismasterid)
        THROW 50404, 'Diagnosis not found.', 1;

    UPDATE dbo.diagnosis_masters
    SET isactive = @isactive,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND diagnosismasterid = @diagnosismasterid;
END
GO
