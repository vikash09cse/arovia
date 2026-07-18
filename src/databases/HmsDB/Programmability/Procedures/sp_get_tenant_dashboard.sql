CREATE OR ALTER PROCEDURE dbo.sp_get_tenant_dashboard
    @tenantid  UNIQUEIDENTIFIER,
    @datefrom  DATE = NULL,
    @dateto    DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @timezone NVARCHAR(50);
    DECLARE @nowUtc DATETIME2 = SYSUTCDATETIME();
    DECLARE @today DATE;
    DECLARE @monthStart DATE;
    DECLARE @filterFrom DATE;
    DECLARE @filterTo DATE;

    SELECT @timezone = t.timezone
    FROM dbo.tenants t
    WHERE t.tenantid = @tenantid
      AND t.isdeleted = 0;

    IF @timezone IS NULL OR LTRIM(RTRIM(@timezone)) = ''
        SET @timezone = N'UTC';

    SET @timezone = dbo.fn_to_sql_timezone(@timezone);
    SET @today = CAST((@nowUtc AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE);
    SET @monthStart = DATEFROMPARTS(YEAR(@today), MONTH(@today), 1);

    SET @filterTo = COALESCE(@dateto, @today);
    SET @filterFrom = COALESCE(@datefrom, @filterTo);

    IF @filterFrom > @filterTo
    BEGIN
        DECLARE @swap DATE = @filterFrom;
        SET @filterFrom = @filterTo;
        SET @filterTo = @swap;
    END

    ;WITH paid_collections AS (
        SELECT
            p.visitid,
            SUM(p.amountpaid) AS totalcollected
        FROM dbo.payments p
        WHERE p.tenantid = @tenantid
          AND p.paymentstatus = 2
        GROUP BY p.visitid
    )
    SELECT
        (
            SELECT COUNT(*)
            FROM dbo.patients p
            WHERE p.tenantid = @tenantid
              AND p.isdeleted = 0
        ) AS totalpatientcount,

        (
            SELECT COUNT(*)
            FROM dbo.patients p
            WHERE p.tenantid = @tenantid
              AND p.isdeleted = 0
              AND CAST((p.createdat AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @filterFrom
              AND CAST((p.createdat AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @filterTo
        ) AS todaynewpatientcount,

        (
            SELECT COUNT(*)
            FROM dbo.visits v
            WHERE v.tenantid = @tenantid
              AND v.isdeleted = 0
              AND CAST((v.visitdatetime AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @filterFrom
              AND CAST((v.visitdatetime AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @filterTo
        ) AS todayvisitcount,

        (
            SELECT ISNULL(SUM(p.amountpaid), 0)
            FROM dbo.payments p
            INNER JOIN dbo.visits v
                ON v.visitid = p.visitid
               AND v.tenantid = p.tenantid
            WHERE p.tenantid = @tenantid
              AND p.paymentstatus = 2
              AND v.isdeleted = 0
              AND v.visitstatus = 1
              AND CAST((COALESCE(p.collectiondatetime, p.createdat) AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @filterFrom
              AND CAST((COALESCE(p.collectiondatetime, p.createdat) AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @filterTo
        ) AS todayrevenue,

        (
            SELECT ISNULL(SUM(p.amountpaid), 0)
            FROM dbo.payments p
            INNER JOIN dbo.visits v
                ON v.visitid = p.visitid
               AND v.tenantid = p.tenantid
            WHERE p.tenantid = @tenantid
              AND p.paymentstatus = 2
              AND v.isdeleted = 0
              AND v.visitstatus = 1
              AND CAST((COALESCE(p.collectiondatetime, p.createdat) AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @monthStart
              AND CAST((COALESCE(p.collectiondatetime, p.createdat) AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @today
        ) AS currentmonthrevenue,

        (
            SELECT ISNULL(SUM(
                CASE
                    WHEN ISNULL(v.totalchargeamount, 0) - ISNULL(pc.totalcollected, 0) > 0
                        THEN ISNULL(v.totalchargeamount, 0) - ISNULL(pc.totalcollected, 0)
                    ELSE 0
                END
            ), 0)
            FROM dbo.visits v
            LEFT JOIN paid_collections pc ON pc.visitid = v.visitid
            WHERE v.tenantid = @tenantid
              AND v.visitstatus = 1
              AND v.isdeleted = 0
        ) AS totalpendingamount,

        (
            SELECT ISNULL(SUM(
                CASE
                    WHEN ISNULL(v.totalchargeamount, 0) - ISNULL(pc.totalcollected, 0) > 0
                        THEN ISNULL(v.totalchargeamount, 0) - ISNULL(pc.totalcollected, 0)
                    ELSE 0
                END
            ), 0)
            FROM dbo.visits v
            LEFT JOIN paid_collections pc ON pc.visitid = v.visitid
            WHERE v.tenantid = @tenantid
              AND v.visitstatus = 1
              AND v.isdeleted = 0
              AND CAST((v.visitdatetime AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @filterFrom
              AND CAST((v.visitdatetime AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @filterTo
        ) AS todaypendingamount,

        (
            SELECT COUNT(*)
            FROM dbo.visit_lab_agencies vla
            WHERE vla.tenantid = @tenantid
              AND CAST((vla.assignedat AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @filterFrom
              AND CAST((vla.assignedat AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @filterTo
        ) AS todaylabassigncount,

        (
            SELECT COUNT(*)
            FROM dbo.visit_lab_agencies vla
            WHERE vla.tenantid = @tenantid
              AND CAST((vla.assignedat AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @monthStart
              AND CAST((vla.assignedat AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @today
        ) AS currentmonthlabassigncount;
END
GO
