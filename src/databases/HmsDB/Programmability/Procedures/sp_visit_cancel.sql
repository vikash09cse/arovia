CREATE OR ALTER PROCEDURE dbo.sp_visit_cancel
    @tenantid             UNIQUEIDENTIFIER,
    @visitid              UNIQUEIDENTIFIER,
    @cancellationreason   NVARCHAR(500),
    @actorid              UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @cancellationreason IS NOT NULL AND LTRIM(RTRIM(@cancellationreason)) = ''
        SET @cancellationreason = NULL;

    BEGIN TRANSACTION;

    UPDATE dbo.visits
    SET visitstatus = 2,
        cancellationreason = @cancellationreason,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE visitid = @visitid
      AND tenantid = @tenantid
      AND visitstatus = 1;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50404, 'Visit not found or already cancelled.', 1;
    END

    UPDATE dbo.payments
    SET paymentstatus = 3,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND visitid = @visitid
      AND paymentstatus = 1;

    COMMIT TRANSACTION;
END
GO
