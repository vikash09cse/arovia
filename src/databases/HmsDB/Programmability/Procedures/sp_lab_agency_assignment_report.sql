CREATE OR ALTER PROCEDURE dbo.sp_lab_agency_assignment_report
    @tenantid           UNIQUEIDENTIFIER,
    @datefrom           DATE = NULL,
    @dateto             DATE = NULL,
    @patientcode        NVARCHAR(30) = NULL,
    @phoneblindindex    BINARY(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH matching_assignments AS (
        SELECT
            vla.labagencyid,
            vla.visitid
        FROM dbo.visit_lab_agencies vla
        INNER JOIN dbo.visits v
            ON v.visitid = vla.visitid
           AND v.tenantid = vla.tenantid
        INNER JOIN dbo.patients p
            ON p.patientid = v.patientid
           AND p.tenantid = v.tenantid
           AND p.isdeleted = 0
        WHERE vla.tenantid = @tenantid
          AND (@datefrom IS NULL OR CAST(v.visitdatetime AS DATE) >= @datefrom)
          AND (@dateto IS NULL OR CAST(v.visitdatetime AS DATE) <= @dateto)
          AND (@patientcode IS NULL OR p.patientcode = @patientcode)
          AND (@phoneblindindex IS NULL OR p.phoneblindindex = @phoneblindindex)
    )
    SELECT
        la.labagencyid,
        la.name,
        la.contactperson,
        la.phone,
        la.agencystatus,
        COUNT(DISTINCT ma.visitid) AS visitcount
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
        COUNT(DISTINCT ma.visitid) DESC,
        la.name;
END
GO
