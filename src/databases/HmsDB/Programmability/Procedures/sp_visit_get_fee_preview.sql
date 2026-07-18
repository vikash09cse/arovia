CREATE OR ALTER PROCEDURE dbo.sp_visit_get_fee_preview
    @tenantid  UNIQUEIDENTIFIER,
    @patientid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @visitfeeamount DECIMAL(18, 2);
    DECLARE @freevisitwindowdays INT;
    DECLARE @timezone NVARCHAR(50);
    DECLARE @feestatus TINYINT;
    DECLARE @lastchargedvisitdatetime DATETIME2;
    DECLARE @dayssince INT = NULL;
    DECLARE @visitUtc DATETIME2 = SYSUTCDATETIME();

    SELECT
        @visitfeeamount = ts.visitfeeamount,
        @freevisitwindowdays = ts.freevisitwindowdays
    FROM dbo.tenant_settings ts
    WHERE ts.tenantid = @tenantid;

    IF @visitfeeamount IS NULL
    BEGIN
        SET @visitfeeamount = 0;
        SET @freevisitwindowdays = 10;
    END

    SELECT @timezone = t.timezone
    FROM dbo.tenants t
    WHERE t.tenantid = @tenantid;

    IF @timezone IS NULL OR LTRIM(RTRIM(@timezone)) = ''
        SET @timezone = N'UTC';

    SET @timezone = dbo.fn_to_sql_timezone(@timezone);

    SELECT TOP 1 @lastchargedvisitdatetime = v.visitdatetime
    FROM dbo.visits v
    WHERE v.tenantid = @tenantid
      AND v.patientid = @patientid
      AND v.visitstatus = 1
      AND v.isdeleted = 0
      AND v.feestatus = 1
    ORDER BY v.visitdatetime DESC;

    IF @lastchargedvisitdatetime IS NULL
        SET @feestatus = 1;
    ELSE
    BEGIN
        DECLARE @lastLocalDate DATE = CAST(
            (@lastchargedvisitdatetime AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE);
        DECLARE @visitLocalDate DATE = CAST(
            (@visitUtc AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE);

        SET @dayssince = DATEDIFF(day, @lastLocalDate, @visitLocalDate);

        IF @dayssince <= @freevisitwindowdays
            SET @feestatus = 2;
        ELSE
            SET @feestatus = 1;
    END

    SELECT
        @feestatus AS proposedfeestatus,
        CASE WHEN @feestatus = 1 THEN @visitfeeamount ELSE NULL END AS proposedfeeamount,
        @visitfeeamount AS tenantvisitfeeamount,
        @freevisitwindowdays AS freevisitwindowdays,
        @lastchargedvisitdatetime AS lastchargedvisitdatetime,
        @dayssince AS dayssincelastcharged;
END
GO
