CREATE OR ALTER PROCEDURE dbo.sp_visit_lab_agency_assign
    @tenantid    UNIQUEIDENTIFIER,
    @visitid     UNIQUEIDENTIFIER,
    @labagencyid UNIQUEIDENTIFIER,
    @testname    NVARCHAR(250) = NULL,
    @notes       NVARCHAR(500) = NULL,
    @actorid     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.visits v
        WHERE v.tenantid = @tenantid
          AND v.visitid = @visitid
          AND v.visitstatus = 1)
        THROW 50400, 'Visit not found or not active.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.lab_agencies la
        WHERE la.tenantid = @tenantid
          AND la.labagencyid = @labagencyid
          AND la.agencystatus = 1)
        THROW 50400, 'Lab agency not found or not active.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.visit_lab_agencies vla
        WHERE vla.tenantid = @tenantid
          AND vla.visitid = @visitid
          AND vla.labagencyid = @labagencyid)
        THROW 50409, 'This lab agency is already assigned to the visit.', 1;

    DECLARE @visitlabagencyid UNIQUEIDENTIFIER = NEWID();
    DECLARE @assignedat DATETIME2 = SYSUTCDATETIME();

    INSERT INTO dbo.visit_lab_agencies (
        visitlabagencyid, tenantid, visitid, labagencyid, assignedat, assignedby, testname, notes)
    VALUES (
        @visitlabagencyid, @tenantid, @visitid, @labagencyid, @assignedat, @actorid,
        NULLIF(LTRIM(RTRIM(@testname)), ''),
        NULLIF(LTRIM(RTRIM(@notes)), ''));

    SELECT
        vla.visitlabagencyid,
        vla.labagencyid,
        la.name AS agencyname,
        vla.assignedat,
        vla.assignedby AS assignedbyuserid,
        u.firstname AS assignerfirstname,
        u.lastname AS assignerlastname,
        vla.testname,
        vla.notes
    FROM dbo.visit_lab_agencies vla
    INNER JOIN dbo.lab_agencies la ON la.labagencyid = vla.labagencyid AND la.tenantid = vla.tenantid
    INNER JOIN dbo.users u ON u.userid = vla.assignedby
    WHERE vla.visitlabagencyid = @visitlabagencyid;
END
GO
