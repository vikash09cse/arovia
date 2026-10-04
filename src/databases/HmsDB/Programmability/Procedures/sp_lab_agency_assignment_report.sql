CREATE OR ALTER PROCEDURE dbo.sp_lab_agency_assignment_report
    @tenantid           UNIQUEIDENTIFIER,
    @datefrom           DATE = NULL,
    @dateto             DATE = NULL,
    @patientcode        NVARCHAR(30) = NULL,
    @phoneblindindex    BINARY(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Match dashboard lab-assign cards: filter by assignment date in hospital timezone.
    DECLARE @timezone NVARCHAR(50);
    SELECT @timezone = t.timezone
    FROM dbo.tenants t
    WHERE t.tenantid = @tenantid
      AND t.isdeleted = 0;

    IF @timezone IS NULL OR LTRIM(RTRIM(@timezone)) = ''
        SET @timezone = N'UTC';

    SET @timezone = dbo.fn_to_sql_timezone(@timezone);

    ;WITH matching_assignments AS (
        SELECT
            vla.labagencyid,
            vla.visitid,
            vla.visitlabagencyid
        FROM dbo.visit_lab_agencies vla
        INNER JOIN dbo.visits v
            ON v.visitid = vla.visitid
           AND v.tenantid = vla.tenantid
        INNER JOIN dbo.patients p
            ON p.patientid = v.patientid
           AND p.tenantid = v.tenantid
           AND p.isdeleted = 0
        WHERE vla.tenantid = @tenantid
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
    )
    SELECT
        la.labagencyid,
        la.name,
        la.contactperson,
        la.phone,
        la.agencystatus,
        COUNT(ma.visitlabagencyid) AS visitcount
    FROM dbo.lab_agencies la
    LEFT JOIN matching_assignments ma ON ma.labagencyid = la.labagencyid
    WHERE la.tenantid = @tenantid
    GROUP BY
        la.labagencyid,
        la.name,
        la.contactperson,
        la.phone,
        la.agencystatus
    ORDER BY
        COUNT(ma.visitlabagencyid) DESC,
        la.name;
END
GO
