CREATE OR ALTER PROCEDURE dbo.sp_delete_patient
    @tenantid   UNIQUEIDENTIFIER,
    @patientid  UNIQUEIDENTIFIER,
    @updatedby  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.patients
    SET isdeleted = 1,
        updatedby = @updatedby,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND patientid = @patientid
      AND isdeleted = 0;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50404, 'Patient not found.', 1;
    END

    -- Soft-delete visits so they drop from visit/payment/receipt/dashboard displays
    UPDATE dbo.visits
    SET isdeleted = 1,
        visitstatus = 2,
        updatedby = @updatedby,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND patientid = @patientid
      AND isdeleted = 0;

    -- Clear unpaid charge lines; collected payments remain on record
    UPDATE dbo.payments
    SET paymentstatus = 3,
        updatedby = @updatedby,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND patientid = @patientid
      AND paymentstatus = 1;

    COMMIT TRANSACTION;
END
GO
