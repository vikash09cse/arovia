CREATE OR ALTER PROCEDURE dbo.sp_monthly_profile_reconcile
    @tenantid   UNIQUEIDENTIFIER,
    @yearmonth  CHAR(7),
    @note       NVARCHAR(500) = NULL,
    @actorid    UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @yearmonth IS NULL
       OR @yearmonth NOT LIKE '[0-9][0-9][0-9][0-9]-[0-1][0-9]'
       OR SUBSTRING(@yearmonth, 6, 2) NOT BETWEEN '01' AND '12'
        THROW 50400, 'Invalid year-month. Use YYYY-MM.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.monthly_profiles mp
        WHERE mp.tenantid = @tenantid
          AND mp.yearmonth = @yearmonth
          AND mp.profilestatus = 2)
        THROW 50409, 'This month is already reconciled.', 1;

    DECLARE @opd DECIMAL(18, 2);
    DECLARE @ipd DECIMAL(18, 2);
    DECLARE @totalRevenue DECIMAL(18, 2);
    DECLARE @expenses DECIMAL(18, 2);
    DECLARE @profit DECIMAL(18, 2);

    CREATE TABLE #preview (
        opdrevenue    DECIMAL(18, 2) NOT NULL,
        ipdrevenue    DECIMAL(18, 2) NOT NULL,
        totalrevenue  DECIMAL(18, 2) NOT NULL,
        totalexpenses DECIMAL(18, 2) NOT NULL,
        profit        DECIMAL(18, 2) NOT NULL
    );

    INSERT INTO #preview
    EXEC dbo.sp_monthly_profile_preview @tenantid = @tenantid, @yearmonth = @yearmonth;

    SELECT
        @opd = opdrevenue,
        @ipd = ipdrevenue,
        @totalRevenue = totalrevenue,
        @expenses = totalexpenses,
        @profit = profit
    FROM #preview;

    DECLARE @noteTrimmed NVARCHAR(500) = NULLIF(LTRIM(RTRIM(@note)), N'');
    DECLARE @now DATETIME2 = SYSUTCDATETIME();
    DECLARE @profileid UNIQUEIDENTIFIER;

    SELECT @profileid = mp.monthlyprofileid
    FROM dbo.monthly_profiles mp
    WHERE mp.tenantid = @tenantid
      AND mp.yearmonth = @yearmonth;

    IF @profileid IS NULL
    BEGIN
        SET @profileid = NEWID();
        INSERT INTO dbo.monthly_profiles (
            monthlyprofileid, tenantid, yearmonth, profilestatus,
            opdrevenue, ipdrevenue, totalrevenue, totalexpenses, profit,
            note, reconciledat, reconciledby, createdby, updatedby)
        VALUES (
            @profileid, @tenantid, @yearmonth, 2,
            @opd, @ipd, @totalRevenue, @expenses, @profit,
            @noteTrimmed, @now, @actorid, @actorid, @actorid);
    END
    ELSE
    BEGIN
        UPDATE dbo.monthly_profiles
        SET profilestatus = 2,
            opdrevenue = @opd,
            ipdrevenue = @ipd,
            totalrevenue = @totalRevenue,
            totalexpenses = @expenses,
            profit = @profit,
            note = COALESCE(@noteTrimmed, note),
            reconciledat = @now,
            reconciledby = @actorid,
            updatedby = @actorid,
            updatedat = @now
        WHERE tenantid = @tenantid
          AND monthlyprofileid = @profileid;
    END

    SELECT
        mp.monthlyprofileid,
        mp.yearmonth,
        mp.profilestatus,
        mp.opdrevenue,
        mp.ipdrevenue,
        mp.totalrevenue,
        mp.totalexpenses,
        mp.profit,
        mp.note,
        mp.reconciledat,
        mp.reconciledby,
        LTRIM(RTRIM(CONCAT(ISNULL(u.firstname, N''), N' ', ISNULL(u.lastname, N'')))) AS reconcilername,
        mp.createdat
    FROM dbo.monthly_profiles mp
    LEFT JOIN dbo.users u
        ON u.userid = mp.reconciledby
       AND u.tenantid = mp.tenantid
    WHERE mp.tenantid = @tenantid
      AND mp.monthlyprofileid = @profileid;
END
GO
