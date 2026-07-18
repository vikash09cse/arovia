CREATE OR ALTER PROCEDURE dbo.sp_visit_lab_agency_remove
    @tenantid         UNIQUEIDENTIFIER,
    @visitid          UNIQUEIDENTIFIER,
    @visitlabagencyid UNIQUEIDENTIFIER,
    @actorid          UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.visits v
        WHERE v.tenantid = @tenantid
          AND v.visitid = @visitid
          AND v.visitstatus = 1
          AND v.isdeleted = 0)
        THROW 50400, 'Visit not found or not active.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.visit_lab_agencies vla
        WHERE vla.tenantid = @tenantid
          AND vla.visitid = @visitid
          AND vla.visitlabagencyid = @visitlabagencyid)
        THROW 50404, 'Lab agency assignment not found.', 1;

    DELETE FROM dbo.visit_lab_agencies
    WHERE tenantid = @tenantid
      AND visitid = @visitid
      AND visitlabagencyid = @visitlabagencyid;
END
GO
