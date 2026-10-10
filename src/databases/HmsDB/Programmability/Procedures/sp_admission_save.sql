CREATE OR ALTER PROCEDURE dbo.sp_admission_save
    @tenantid            UNIQUEIDENTIFIER,
    @patientid           UNIQUEIDENTIFIER,
    @departmentid        UNIQUEIDENTIFIER,
    @attendingdoctorid   UNIQUEIDENTIFIER,
    @ward                NVARCHAR(100),
    @bed                 NVARCHAR(50) = NULL,
    @roomclass           NVARCHAR(50),
    @estimatedamount     DECIMAL(18, 2) = 0,
    @notes               NVARCHAR(1000) = NULL,
    @fromvisitid         UNIQUEIDENTIFIER = NULL,
    @depositamount       DECIMAL(18, 2) = NULL,
    @depositpaymentmethod TINYINT = NULL,
    @collectedby         UNIQUEIDENTIFIER = NULL,
    @admissiondate        DATE = NULL, -- tenant-local calendar date; NULL = today
    @actorid             UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.patients p
        WHERE p.patientid = @patientid
          AND p.tenantid = @tenantid
          AND p.isdeleted = 0
          AND p.patientstatus = 1)
        THROW 50404, 'Patient not found or inactive.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.admissions a
        WHERE a.tenantid = @tenantid
          AND a.patientid = @patientid
          AND a.admissionstatus = 1
          AND a.isdeleted = 0) -- Admitted
        THROW 50409, 'Patient is already admitted. Discharge the current stay before admitting again.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.departments d
        WHERE d.departmentid = @departmentid
          AND d.tenantid = @tenantid
          AND d.departmentstatus = 1)
        THROW 50400, 'Department not found or not active.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.users u
        WHERE u.userid = @attendingdoctorid
          AND u.tenantid = @tenantid
          AND u.usertype = 3
          AND u.userstatus = 1
          AND u.isdeleted = 0
          AND u.departmentid = @departmentid)
        THROW 50400, 'Invalid attending doctor for the selected department.', 1;

    IF @ward IS NULL OR LTRIM(RTRIM(@ward)) = N''
        THROW 50400, 'Ward is required.', 1;

    IF @roomclass IS NULL OR LTRIM(RTRIM(@roomclass)) = N''
        THROW 50400, 'Room class is required.', 1;

    IF @estimatedamount IS NULL OR @estimatedamount < 0
        THROW 50400, 'Estimated amount cannot be negative.', 1;

    IF @fromvisitid IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM dbo.visits v
            WHERE v.visitid = @fromvisitid
              AND v.tenantid = @tenantid
              AND v.patientid = @patientid
              AND v.isdeleted = 0)
        THROW 50400, 'Source visit not found for this patient.', 1;

    IF @depositamount IS NOT NULL AND @depositamount < 0
        THROW 50400, 'Deposit amount cannot be negative.', 1;

    IF @depositamount IS NOT NULL AND @depositamount > 0
    BEGIN
        IF @collectedby IS NULL
            THROW 50400, 'Collector is required for deposit.', 1;
        IF @depositpaymentmethod IS NULL OR @depositpaymentmethod NOT BETWEEN 1 AND 4
            THROW 50400, 'Valid payment method is required for deposit.', 1;
        IF NOT EXISTS (
            SELECT 1 FROM dbo.users u
            WHERE u.userid = @collectedby
              AND u.tenantid = @tenantid
              AND u.usertype IN (1, 2)
              AND u.userstatus = 1
              AND u.isdeleted = 0)
            THROW 50400, 'Invalid payment collector.', 1;
    END

    DECLARE @timezone NVARCHAR(50);
    DECLARE @nowUtc DATETIME2 = SYSUTCDATETIME();
    DECLARE @nowLocal DATETIME2;
    DECLARE @admittedUtc DATETIME2;
    DECLARE @regDate DATE;
    DECLARE @localAdmit DATETIME2;
    DECLARE @seq INT;
    DECLARE @admissioncode NVARCHAR(30);
    DECLARE @admissionid UNIQUEIDENTIFIER = NEWID();

    SELECT @timezone = t.timezone
    FROM dbo.tenants t
    WHERE t.tenantid = @tenantid;

    IF @timezone IS NULL OR LTRIM(RTRIM(@timezone)) = N''
        SET @timezone = N'UTC';

    SET @timezone = dbo.fn_to_sql_timezone(@timezone);
    SET @nowLocal = (@nowUtc AT TIME ZONE 'UTC') AT TIME ZONE @timezone;
    SET @regDate = ISNULL(@admissiondate, CAST(@nowLocal AS DATE));

    -- Selected (or today) local date + current local time-of-day → store as UTC
    SET @localAdmit = DATETIME2FROMPARTS(
        YEAR(@regDate), MONTH(@regDate), DAY(@regDate),
        DATEPART(HOUR, @nowLocal), DATEPART(MINUTE, @nowLocal), DATEPART(SECOND, @nowLocal),
        0, 0);
    SET @admittedUtc = (@localAdmit AT TIME ZONE @timezone) AT TIME ZONE 'UTC';

    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.admission_sequences WITH (UPDLOCK, ROWLOCK)
        WHERE tenantid = @tenantid
          AND sequencedate = @regDate)
    BEGIN
        INSERT INTO dbo.admission_sequences (tenantid, sequencedate, nextsequencenumber)
        VALUES (@tenantid, @regDate, 2);
        SET @seq = 1;
    END
    ELSE
    BEGIN
        SELECT @seq = nextsequencenumber
        FROM dbo.admission_sequences WITH (UPDLOCK, ROWLOCK)
        WHERE tenantid = @tenantid
          AND sequencedate = @regDate;

        UPDATE dbo.admission_sequences
        SET nextsequencenumber = @seq + 1,
            updatedat = SYSUTCDATETIME()
        WHERE tenantid = @tenantid
          AND sequencedate = @regDate;
    END

    SET @admissioncode = N'IP-'
        + CONVERT(CHAR(8), @regDate, 112)
        + N'-'
        + CASE
            WHEN @seq < 100 THEN RIGHT(N'00' + CAST(@seq AS NVARCHAR(10)), 2)
            ELSE CAST(@seq AS NVARCHAR(10))
          END;

    INSERT INTO dbo.admissions (
        admissionid, tenantid, admissioncode, sequencenumber, patientid, departmentid, attendingdoctorid,
        admittedat, ward, bed, roomclass, admissionstatus, estimatedamount, notes,
        discountamount, discountreason, fromvisitid, createdby, updatedby)
    VALUES (
        @admissionid, @tenantid, @admissioncode, @seq, @patientid, @departmentid, @attendingdoctorid,
        @admittedUtc, LTRIM(RTRIM(@ward)), NULLIF(LTRIM(RTRIM(@bed)), N''), LTRIM(RTRIM(@roomclass)),
        1, @estimatedamount, NULLIF(LTRIM(RTRIM(@notes)), N''),
        0, NULL, @fromvisitid, @actorid, @actorid);

    IF @estimatedamount > 0
    BEGIN
        INSERT INTO dbo.admission_charges (
            admissionchargeid, tenantid, admissionid, chargecategory, description, amount, chargedon, createdby)
        VALUES (
            NEWID(), @tenantid, @admissionid, 1,
            N'Estimated room / stay', @estimatedamount, @admittedUtc, @actorid);
    END

    IF @depositamount IS NOT NULL AND @depositamount > 0
    BEGIN
        DECLARE @depositreceipt NVARCHAR(20);
        EXEC dbo.sp_allocate_receipt_number @tenantid, @depositreceipt OUTPUT;

        INSERT INTO dbo.admission_payments (
            admissionpaymentid, tenantid, admissionid, patientid, amount, paymentmethod,
            paymentkind, receiptnumber, notes, collectedby, collectiondatetime, createdby)
        VALUES (
            NEWID(), @tenantid, @admissionid, @patientid, @depositamount, @depositpaymentmethod,
            1, @depositreceipt, N'Deposit on admission', @collectedby, @admittedUtc, @actorid);
    END

    COMMIT TRANSACTION;

    SELECT @admissionid AS admissionid;
END
GO
