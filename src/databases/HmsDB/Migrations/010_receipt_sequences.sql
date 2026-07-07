-- Receipt number sequences and unique receipt index on payments

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'receipt_sequences' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.receipt_sequences (
        tenantid            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_receipt_sequences PRIMARY KEY,
        nextsequencenumber  INT              NOT NULL CONSTRAINT DF_receipt_sequences_next DEFAULT (1),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_receipt_sequences_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_receipt_sequences_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_payments_tenant_receiptnumber'
      AND object_id = OBJECT_ID('dbo.payments'))
BEGIN
    CREATE UNIQUE INDEX UQ_payments_tenant_receiptnumber
        ON dbo.payments (tenantid, receiptnumber)
        WHERE receiptnumber IS NOT NULL;
END
GO

-- Backfill receipt numbers for payments already marked paid
DECLARE @bf_tenantid UNIQUEIDENTIFIER;
DECLARE @bf_paymentid UNIQUEIDENTIFIER;
DECLARE @bf_seq INT;
DECLARE @bf_receipt NVARCHAR(20);

DECLARE bf CURSOR LOCAL FAST_FORWARD FOR
    SELECT tenantid, paymentid
    FROM dbo.payments
    WHERE paymentstatus = 2
      AND receiptnumber IS NULL
    ORDER BY COALESCE(collectiondatetime, createdat), paymentid;

OPEN bf;
FETCH NEXT FROM bf INTO @bf_tenantid, @bf_paymentid;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.receipt_sequences WITH (UPDLOCK, ROWLOCK)
        WHERE tenantid = @bf_tenantid)
    BEGIN
        INSERT INTO dbo.receipt_sequences (tenantid, nextsequencenumber)
        VALUES (@bf_tenantid, 1);
    END

    SELECT @bf_seq = nextsequencenumber
    FROM dbo.receipt_sequences WITH (UPDLOCK, ROWLOCK)
    WHERE tenantid = @bf_tenantid;

    UPDATE dbo.receipt_sequences
    SET nextsequencenumber = @bf_seq + 1,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @bf_tenantid;

    SET @bf_receipt = N'RCP-' + RIGHT(REPLICATE(N'0', 7) + CAST(@bf_seq AS NVARCHAR(10)), 7);

    UPDATE dbo.payments
    SET receiptnumber = @bf_receipt
    WHERE paymentid = @bf_paymentid;

    FETCH NEXT FROM bf INTO @bf_tenantid, @bf_paymentid;
END

CLOSE bf;
DEALLOCATE bf;
GO
