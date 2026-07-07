CREATE OR ALTER PROCEDURE dbo.sp_set_patient_status
    @tenantid       UNIQUEIDENTIFIER,
    @patientid      UNIQUEIDENTIFIER,
    @patientstatus  TINYINT,
    @updatedby      UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.patients
    SET patientstatus = @patientstatus,
        updatedby = @updatedby,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND patientid = @patientid
      AND isdeleted = 0;

    IF @@ROWCOUNT = 0
        THROW 50404, 'Patient not found.', 1;
END
GO
