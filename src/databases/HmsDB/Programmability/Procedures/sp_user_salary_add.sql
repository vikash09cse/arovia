CREATE OR ALTER PROCEDURE dbo.sp_user_salary_add
    @tenantid       UNIQUEIDENTIFIER,
    @userid         UNIQUEIDENTIFIER,
    @monthlysalary  DECIMAL(18,2),
    @effectivefrom  DATE,
    @notes          NVARCHAR(500) = NULL,
    @actorid        UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @usertype TINYINT;
    DECLARE @openid UNIQUEIDENTIFIER;
    DECLARE @openfrom DATE;
    DECLARE @usersalaryid UNIQUEIDENTIFIER = NEWID();
    DECLARE @trimmednotes NVARCHAR(500) = NULLIF(LTRIM(RTRIM(@notes)), N'');

    SELECT @usertype = u.usertype
    FROM dbo.users u
    WHERE u.tenantid = @tenantid
      AND u.userid = @userid
      AND u.isdeleted = 0;

    IF @usertype IS NULL
        THROW 50404, 'User not found.', 1;

    -- Staff only (usertype = 2)
    IF @usertype <> 2
        THROW 50400, 'Monthly salary can only be set for Staff users.', 1;

    IF @monthlysalary < 0
        THROW 50400, 'Monthly salary cannot be negative.', 1;

    IF @effectivefrom IS NULL
        THROW 50400, 'Effective from date is required.', 1;

    BEGIN TRAN;

    SELECT TOP (1)
        @openid = s.usersalaryid,
        @openfrom = s.effectivefrom
    FROM dbo.user_salaries s WITH (UPDLOCK, HOLDLOCK)
    WHERE s.tenantid = @tenantid
      AND s.userid = @userid
      AND s.isdeleted = 0
      AND s.effectiveto IS NULL
    ORDER BY s.effectivefrom DESC, s.createdat DESC;

    IF @openid IS NOT NULL AND @effectivefrom = @openfrom
    BEGIN
        -- Same-day / same effective date: update the open row in place
        UPDATE dbo.user_salaries
        SET monthlysalary = @monthlysalary,
            notes = @trimmednotes
        WHERE usersalaryid = @openid
          AND tenantid = @tenantid
          AND isdeleted = 0
          AND effectiveto IS NULL;

        SET @usersalaryid = @openid;
    END
    ELSE
    BEGIN
        IF @openid IS NOT NULL
        BEGIN
            IF @effectivefrom < @openfrom
                THROW 50400, 'Effective from must be on or after the current salary start date.', 1;

            UPDATE dbo.user_salaries
            SET effectiveto = DATEADD(DAY, -1, @effectivefrom)
            WHERE usersalaryid = @openid
              AND tenantid = @tenantid
              AND isdeleted = 0
              AND effectiveto IS NULL;
        END

        INSERT INTO dbo.user_salaries (
            usersalaryid, tenantid, userid, monthlysalary, effectivefrom, effectiveto, notes, isdeleted, createdby)
        VALUES (
            @usersalaryid, @tenantid, @userid, @monthlysalary, @effectivefrom, NULL, @trimmednotes, 0, @actorid);
    END

    COMMIT TRAN;

    SELECT
        s.usersalaryid,
        s.userid,
        s.monthlysalary,
        s.effectivefrom,
        s.effectiveto,
        s.notes,
        s.createdat,
        s.createdby
    FROM dbo.user_salaries s
    WHERE s.usersalaryid = @usersalaryid;
END
GO
