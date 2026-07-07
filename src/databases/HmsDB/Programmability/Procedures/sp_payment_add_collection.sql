CREATE OR ALTER PROCEDURE dbo.sp_payment_add_collection
    @tenantid       UNIQUEIDENTIFIER,
    @visitid        UNIQUEIDENTIFIER,
    @amount         DECIMAL(18, 2),
    @collectedby    UNIQUEIDENTIFIER,
    @notes          NVARCHAR(500) = NULL,
    @paymentmethod  TINYINT = NULL,
    @actorid        UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF @amount IS NULL OR @amount <= 0
        THROW 50400, 'Collection amount must be greater than zero.', 1;

    DECLARE @patientid UNIQUEIDENTIFIER;
    DECLARE @totaldue DECIMAL(18, 2);
    DECLARE @totalcollected DECIMAL(18, 2);
    DECLARE @balancedue DECIMAL(18, 2);
    DECLARE @paymentid UNIQUEIDENTIFIER = NEWID();
    DECLARE @receiptnumber NVARCHAR(20);
    DECLARE @collector UNIQUEIDENTIFIER = ISNULL(@collectedby, @actorid);

    SELECT
        @patientid = v.patientid,
        @totaldue = ISNULL(v.totalchargeamount, 0)
    FROM dbo.visits v
    WHERE v.visitid = @visitid
      AND v.tenantid = @tenantid
      AND v.visitstatus = 1;

    IF @patientid IS NULL
        THROW 50400, 'Visit not found or not active.', 1;

    IF @totaldue <= 0
        THROW 50400, 'This visit has no charges to collect.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.users u
        WHERE u.userid = @collector
          AND u.tenantid = @tenantid
          AND u.usertype IN (1, 2)
          AND u.userstatus = 1
          AND u.isdeleted = 0)
    BEGIN
        THROW 50400, 'Invalid payment collector.', 1;
    END

    SELECT @totalcollected = ISNULL(SUM(p.amountpaid), 0)
    FROM dbo.payments p
    WHERE p.tenantid = @tenantid
      AND p.visitid = @visitid
      AND p.paymentstatus = 2;

    SET @balancedue = @totaldue - @totalcollected;

    IF @amount > @balancedue
        THROW 50400, 'Collection amount exceeds the remaining balance.', 1;

    EXEC dbo.sp_allocate_receipt_number @tenantid, @receiptnumber OUTPUT;

    INSERT INTO dbo.payments (
        paymentid, tenantid, visitid, patientid, paymentlinetype, feeamount, paymentstatus,
        receiptnumber, amountpaid, paymentmethod, collectedby, collectiondatetime, notes,
        createdby, updatedby)
    VALUES (
        @paymentid, @tenantid, @visitid, @patientid, 3, @amount, 2,
        @receiptnumber, @amount, @paymentmethod, @collector, SYSUTCDATETIME(),
        NULLIF(LTRIM(RTRIM(@notes)), ''),
        @actorid, @actorid);

    SELECT
        @paymentid AS paymentid,
        @receiptnumber AS receiptnumber,
        @amount AS amount,
        @totaldue AS totaldue,
        @totalcollected + @amount AS totalcollected,
        @balancedue - @amount AS balancedue;
END
GO
