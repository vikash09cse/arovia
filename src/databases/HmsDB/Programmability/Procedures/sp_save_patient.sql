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

        DECLARE @prefix NVARCHAR(10);
        DECLARE @seq INT;
        DECLARE @patientcode NVARCHAR(20);

        SELECT @prefix = ISNULL(NULLIF(LTRIM(RTRIM(patientidprefix)), ''), 'PT-')
        FROM dbo.tenant_settings
        WHERE tenantid = @tenantid;

        IF @prefix IS NULL
            SET @prefix = 'PT-';

        IF NOT EXISTS (SELECT 1 FROM dbo.patient_sequences WITH (UPDLOCK, ROWLOCK) WHERE tenantid = @tenantid)
        BEGIN
            INSERT INTO dbo.patient_sequences (tenantid, nextsequencenumber)
            VALUES (@tenantid, 2);
            SET @seq = 1;
        END
        ELSE
        BEGIN
            SELECT @seq = nextsequencenumber
            FROM dbo.patient_sequences WITH (UPDLOCK, ROWLOCK)
            WHERE tenantid = @tenantid;

            UPDATE dbo.patient_sequences
            SET nextsequencenumber = @seq + 1,
                updatedat = SYSUTCDATETIME()
            WHERE tenantid = @tenantid;
        END

        SET @patientcode = @prefix + RIGHT(REPLICATE('0', 5) + CAST(@seq AS NVARCHAR(10)), 5);

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
