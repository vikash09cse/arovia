CREATE OR ALTER PROCEDURE dbo.sp_delete_patient
    @tenantid   UNIQUEIDENTIFIER,
    @patientid  UNIQUEIDENTIFIER,
    @updatedby  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.patients
    SET isdeleted = 1,
        updatedby = @updatedby,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND patientid = @patientid
      AND isdeleted = 0;

    IF @@ROWCOUNT = 0
        THROW 50404, 'Patient not found.', 1;
END
GO
