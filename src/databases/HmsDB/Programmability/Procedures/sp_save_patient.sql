CREATE OR ALTER PROCEDURE dbo.sp_save_patient
    @patientid              UNIQUEIDENTIFIER = NULL,
    @tenantid               UNIQUEIDENTIFIER,
    @firstname              NVARCHAR(100),
    @lastname               NVARCHAR(100),
    @dateofbirth            DATE = NULL,
    @age                    INT = NULL,
    @gender                 TINYINT,
    @bloodgroup             TINYINT = NULL,
    @referredby             NVARCHAR(100) = NULL,
    @phonecipher            VARBINARY(512),
    @emailcipher            VARBINARY(512) = NULL,
    @addresscipher          VARBINARY(2048),
    @emergencynamecipher    VARBINARY(512),
    @emergencyphonecipher   VARBINARY(512),
    @phoneblindindex        BINARY(32),
    @emailblindindex        BINARY(32) = NULL,
    @actorid                UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF @patientid IS NULL
    BEGIN
        SET @patientid = NEWID();

        DECLARE @seq INT;
        DECLARE @patientcode NVARCHAR(20);
        DECLARE @timezone NVARCHAR(50);
        DECLARE @regDate DATE;
        DECLARE @nowUtc DATETIME2 = SYSUTCDATETIME();

        SELECT @timezone = t.timezone
        FROM dbo.tenants t
        WHERE t.tenantid = @tenantid;

        IF @timezone IS NULL OR LTRIM(RTRIM(@timezone)) = ''
            SET @timezone = N'UTC';

        SET @timezone = dbo.fn_to_sql_timezone(@timezone);
        SET @regDate = CAST((@nowUtc AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE);

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.patient_sequences WITH (UPDLOCK, ROWLOCK)
            WHERE tenantid = @tenantid
              AND sequencedate = @regDate)
        BEGIN
            INSERT INTO dbo.patient_sequences (tenantid, sequencedate, nextsequencenumber)
            VALUES (@tenantid, @regDate, 2);
            SET @seq = 1;
        END
        ELSE
        BEGIN
            SELECT @seq = nextsequencenumber
            FROM dbo.patient_sequences WITH (UPDLOCK, ROWLOCK)
            WHERE tenantid = @tenantid
              AND sequencedate = @regDate;

            UPDATE dbo.patient_sequences
            SET nextsequencenumber = @seq + 1,
                updatedat = SYSUTCDATETIME()
            WHERE tenantid = @tenantid
              AND sequencedate = @regDate;
        END

        -- Format: YYYYMMDD-Serial (e.g. 20260714-01); serial pads to 2 digits, then grows.
        SET @patientcode = CONVERT(CHAR(8), @regDate, 112)
            + N'-'
            + CASE
                WHEN @seq < 100 THEN RIGHT(N'00' + CAST(@seq AS NVARCHAR(10)), 2)
                ELSE CAST(@seq AS NVARCHAR(10))
              END;

        INSERT INTO dbo.patients (
            patientid, tenantid, patientcode, sequencenumber,
            firstname, lastname, dateofbirth, age, gender, bloodgroup, referredby, patientstatus,
            phonecipher, emailcipher, addresscipher, emergencynamecipher, emergencyphonecipher,
            phoneblindindex, emailblindindex,
            registeredby, createdby, updatedby)
        VALUES (
            @patientid, @tenantid, @patientcode, @seq,
            @firstname, @lastname, @dateofbirth, @age, @gender, @bloodgroup, @referredby, 1,
            @phonecipher, @emailcipher, @addresscipher, @emergencynamecipher, @emergencyphonecipher,
            @phoneblindindex, @emailblindindex,
            @actorid, @actorid, @actorid);
    END
    ELSE
    BEGIN
        UPDATE dbo.patients
        SET firstname = @firstname,
            lastname = @lastname,
            dateofbirth = @dateofbirth,
            age = @age,
            gender = @gender,
            bloodgroup = @bloodgroup,
            referredby = @referredby,
            phonecipher = @phonecipher,
            emailcipher = @emailcipher,
            addresscipher = @addresscipher,
            emergencynamecipher = @emergencynamecipher,
            emergencyphonecipher = @emergencyphonecipher,
            phoneblindindex = @phoneblindindex,
            emailblindindex = @emailblindindex,
            updatedby = @actorid,
            updatedat = SYSUTCDATETIME()
        WHERE patientid = @patientid
          AND tenantid = @tenantid
          AND isdeleted = 0;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50404, 'Patient not found.', 1;
        END
    END

    COMMIT TRANSACTION;

    SELECT @patientid AS patientid;
END
GO
