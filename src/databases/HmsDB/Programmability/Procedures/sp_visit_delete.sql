CREATE OR ALTER PROCEDURE dbo.sp_visit_delete
    @tenantid UNIQUEIDENTIFIER,
    @visitid  UNIQUEIDENTIFIER,
    @actorid  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.visits
    SET isdeleted = 1,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE visitid = @visitid
      AND tenantid = @tenantid
      AND isdeleted = 0;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50404, 'Visit not found or already deleted.', 1;
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
