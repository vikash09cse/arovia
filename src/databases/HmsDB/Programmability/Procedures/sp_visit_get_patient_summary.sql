CREATE OR ALTER PROCEDURE dbo.sp_visit_get_patient_summary
    @tenantid  UNIQUEIDENTIFIER,
    @patientid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS totalvisits,
        SUM(CASE WHEN v.feestatus = 1 THEN 1 ELSE 0 END) AS totalcharged,
        SUM(CASE WHEN v.feestatus = 2 THEN 1 ELSE 0 END) AS totalfree,
        (
            SELECT TOP 1 v2.scheduledsurgerydate
            FROM dbo.visits v2
            WHERE v2.tenantid = @tenantid
              AND v2.patientid = @patientid
              AND v2.visitstatus = 1
              AND v2.isdeleted = 0
              AND v2.scheduledsurgerydate IS NOT NULL
              AND v2.scheduledsurgerydate >= CAST(SYSUTCDATETIME() AS DATE)
            ORDER BY v2.visitdatetime DESC
        ) AS upcomingscheduledsurgerydate
    FROM dbo.visits v
    WHERE v.tenantid = @tenantid
      AND v.patientid = @patientid
      AND v.visitstatus = 1
      AND v.isdeleted = 0;
END
GO
