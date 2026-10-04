CREATE OR ALTER PROCEDURE dbo.sp_expense_get_by_month
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

    DECLARE @isreconciled BIT = 0;
    IF EXISTS (
        SELECT 1
        FROM dbo.monthly_profiles mp
        WHERE mp.tenantid = @tenantid
          AND mp.yearmonth = @yearmonth
          AND mp.profilestatus = 2)
        SET @isreconciled = 1;

    -- Result set 1: summary
    SELECT
        @yearmonth AS yearmonth,
        @isreconciled AS isreconciled,
        ISNULL((
            SELECT SUM(e.amount)
            FROM dbo.expenses e
            WHERE e.tenantid = @tenantid
              AND e.isdeleted = 0
              AND e.expenseon >= @monthStart
              AND e.expenseon <= @monthEnd
        ), 0) AS monthtotal;

    -- Result set 2: category breakdown
    SELECT
        e.category,
        SUM(e.amount) AS total
    FROM dbo.expenses e
    WHERE e.tenantid = @tenantid
      AND e.isdeleted = 0
      AND e.expenseon >= @monthStart
      AND e.expenseon <= @monthEnd
    GROUP BY e.category
    ORDER BY SUM(e.amount) DESC, e.category;

    -- Result set 3: expense rows
    SELECT
        e.expenseid,
        e.amount,
        e.expenseon,
        e.category,
        e.note,
        e.staffuserid,
        LTRIM(RTRIM(CONCAT(ISNULL(u.firstname, N''), N' ', ISNULL(u.lastname, N'')))) AS staffname,
        e.createdat
    FROM dbo.expenses e
    LEFT JOIN dbo.users u
        ON u.userid = e.staffuserid
       AND u.tenantid = e.tenantid
    WHERE e.tenantid = @tenantid
      AND e.isdeleted = 0
      AND e.expenseon >= @monthStart
      AND e.expenseon <= @monthEnd
    ORDER BY e.expenseon DESC, e.createdat DESC;
END
GO
