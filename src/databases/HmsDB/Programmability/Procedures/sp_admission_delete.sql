CREATE OR ALTER PROCEDURE dbo.sp_admission_delete
    @tenantid    UNIQUEIDENTIFIER,
    @admissionid UNIQUEIDENTIFIER,
    @actorid     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.admissions
    SET isdeleted = 1,
        admissionstatus = 3, -- Cancelled so active-stay indexes / filters stay consistent
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE admissionid = @admissionid
      AND tenantid = @tenantid
      AND isdeleted = 0;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50404, 'Admission not found or already deleted.', 1;
    END

    COMMIT TRANSACTION;
END
GO
