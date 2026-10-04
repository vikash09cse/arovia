CREATE OR ALTER PROCEDURE dbo.sp_monthly_profile_preview
    @tenantid   UNIQUEIDENTIFIER,
    @yearmonth  CHAR(7)
AS
BEGIN
    SET NOCOUNT ON;

    IF @yearmonth IS NULL
       OR @yearmonth NOT LIKE '[0-9][0-9][0-9][0-9]-[0-1][0-9]'
       OR SUBSTRING(@yearmonth, 6, 2) NOT BETWEEN '01' AND '12'
        THROW 50400, 'Invalid year-month. Use YYYY-MM.', 1;

    DECLARE @monthStart DATE = CONVERT(DATE, @yearmonth + '-01', 23);
    DECLARE @monthEnd DATE = EOMONTH(@monthStart);

    DECLARE @timezone NVARCHAR(50);
    SELECT @timezone = t.timezone
    FROM dbo.tenants t
    WHERE t.tenantid = @tenantid
      AND t.isdeleted = 0;

    IF @timezone IS NULL OR LTRIM(RTRIM(@timezone)) = ''
        SET @timezone = N'UTC';

    SET @timezone = dbo.fn_to_sql_timezone(@timezone);

    DECLARE @opd DECIMAL(18, 2) = ISNULL((
        SELECT SUM(p.amountpaid)
        FROM dbo.payments p
        INNER JOIN dbo.visits v
            ON v.visitid = p.visitid
           AND v.tenantid = p.tenantid
        INNER JOIN dbo.patients pt
            ON pt.patientid = v.patientid
           AND pt.tenantid = v.tenantid
           AND pt.isdeleted = 0
        WHERE p.tenantid = @tenantid
          AND p.paymentstatus = 2
          AND v.isdeleted = 0
          AND v.visitstatus = 1
          AND CAST((COALESCE(p.collectiondatetime, p.createdat) AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @monthStart
          AND CAST((COALESCE(p.collectiondatetime, p.createdat) AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @monthEnd
    ), 0);

    DECLARE @ipd DECIMAL(18, 2) = ISNULL((
        SELECT SUM(ap.amount)
        FROM dbo.admission_payments ap
        INNER JOIN dbo.patients pt
            ON pt.patientid = ap.patientid
           AND pt.tenantid = ap.tenantid
           AND pt.isdeleted = 0
        WHERE ap.tenantid = @tenantid
          AND CAST((ap.collectiondatetime AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) >= @monthStart
          AND CAST((ap.collectiondatetime AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE) <= @monthEnd
    ), 0);

    DECLARE @expenses DECIMAL(18, 2) = ISNULL((
        SELECT SUM(e.amount)
        FROM dbo.expenses e
        WHERE e.tenantid = @tenantid
          AND e.isdeleted = 0
          AND e.expenseon >= @monthStart
          AND e.expenseon <= @monthEnd
    ), 0);

    DECLARE @totalRevenue DECIMAL(18, 2) = ROUND(@opd + @ipd, 2);
    DECLARE @profit DECIMAL(18, 2) = CASE
        WHEN @totalRevenue - @expenses > 0 THEN ROUND(@totalRevenue - @expenses, 2)
        ELSE 0
    END;

    SELECT
        @opd AS opdrevenue,
        @ipd AS ipdrevenue,
        @totalRevenue AS totalrevenue,
        @expenses AS totalexpenses,
        @profit AS profit;
END
GO
