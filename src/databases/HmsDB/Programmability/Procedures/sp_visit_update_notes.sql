CREATE OR ALTER PROCEDURE dbo.sp_visit_update_notes
    @tenantid   UNIQUEIDENTIFIER,
    @visitid    UNIQUEIDENTIFIER,
    @visitnotes NVARCHAR(1000),
    @actorid    UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.visits
    SET visitnotes = @visitnotes,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE visitid = @visitid
      AND tenantid = @tenantid
      AND visitstatus = 1;

    IF @@ROWCOUNT = 0
        THROW 50404, 'Visit not found or cancelled.', 1;
END
GO
