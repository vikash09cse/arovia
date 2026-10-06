CREATE OR ALTER PROCEDURE dbo.sp_admission_add_payment
    @tenantid       UNIQUEIDENTIFIER,
    @admissionid    UNIQUEIDENTIFIER,
    @amount         DECIMAL(18, 2),
    @paymentmethod  TINYINT,
    @paymentkind    TINYINT,
    @notes          NVARCHAR(500) = NULL,
    @collectedby    UNIQUEIDENTIFIER,
    @actorid        UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @patientid UNIQUEIDENTIFIER;

    SELECT @patientid = a.patientid
    FROM dbo.admissions a
    WHERE a.tenantid = @tenantid
      AND a.admissionid = @admissionid
      AND a.admissionstatus = 1
      AND a.isdeleted = 0;

    IF @patientid IS NULL
        THROW 50400, 'Admission not found or not admitted.', 1;

    IF @amount IS NULL OR @amount <= 0
        THROW 50400, 'Amount must be greater than zero.', 1;

    IF @paymentmethod NOT BETWEEN 1 AND 4
        THROW 50400, 'Invalid payment method.', 1;

    IF @paymentkind NOT IN (1, 2, 3)
        THROW 50400, 'Invalid payment kind.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.users u
        WHERE u.userid = @collectedby
          AND u.tenantid = @tenantid
          AND u.usertype IN (1, 2)
          AND u.userstatus = 1
          AND u.isdeleted = 0)
        THROW 50400, 'Invalid payment collector.', 1;

    DECLARE @admissionpaymentid UNIQUEIDENTIFIER = NEWID();
    DECLARE @collectedat DATETIME2 = SYSUTCDATETIME();
    DECLARE @receiptnumber NVARCHAR(20);

    BEGIN TRANSACTION;

    EXEC dbo.sp_allocate_receipt_number @tenantid, @receiptnumber OUTPUT;

    INSERT INTO dbo.admission_payments (
        admissionpaymentid, tenantid, admissionid, patientid, amount, paymentmethod,
        paymentkind, receiptnumber, notes, collectedby, collectiondatetime, createdby)
    VALUES (
        @admissionpaymentid, @tenantid, @admissionid, @patientid, @amount, @paymentmethod,
        @paymentkind, @receiptnumber, NULLIF(LTRIM(RTRIM(@notes)), N''), @collectedby, @collectedat, @actorid);

    UPDATE dbo.admissions
    SET updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND admissionid = @admissionid;

    COMMIT TRANSACTION;

    SELECT
        @admissionpaymentid AS admissionpaymentid,
        @receiptnumber AS receiptnumber;
END
GO
