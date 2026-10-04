CREATE OR ALTER PROCEDURE dbo.sp_lab_agency_assignment_report_detail
    @tenantid           UNIQUEIDENTIFIER,
    @labagencyid        UNIQUEIDENTIFIER,
    @datefrom           DATE = NULL,
    @dateto             DATE = NULL,
    @patientcode        NVARCHAR(30) = NULL,
    @phoneblindindex    BINARY(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Match assignment-report: filter by assignment date in hospital timezone.
    DECLARE @timezone NVARCHAR(50);
    SELECT @timezone = t.timezone
    FROM dbo.tenants t
    WHERE t.tenantid = @tenantid
      AND t.isdeleted = 0;

    IF @timezone IS NULL OR LTRIM(RTRIM(@timezone)) = ''
        SET @timezone = N'UTC';

    SET @timezone = dbo.fn_to_sql_timezone(@timezone);

    SELECT
        vla.visitlabagencyid,
        v.visitid,
        p.patientid,
        p.patientcode,
        p.firstname AS patientfirstname,
        p.lastname AS patientlastname,
        v.visitdatetime,
        vla.assignedat,
        vla.testname,
        vla.notes
    FROM dbo.visit_lab_agencies vla
    INNER JOIN dbo.visits v
        ON v.visitid = vla.visitid
       AND v.tenantid = vla.tenantid
    INNER JOIN dbo.patients p
        ON p.patientid = v.patientid
       AND p.tenantid = v.tenantid
       AND p.isdeleted = 0
    WHERE vla.tenantid = @tenantid
      AND vla.labagencyid = @labagencyid
      AND (
            @datefrom IS NULL
            OR CAST((vla.assignedat AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @datefrom
          )
      AND (
            @dateto IS NULL
            OR CAST((vla.assignedat AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @dateto
          )
      AND (@patientcode IS NULL OR p.patientcode = @patientcode)
      AND (@phoneblindindex IS NULL OR p.phoneblindindex = @phoneblindindex)
    ORDER BY vla.assignedat DESC;
END
GO
