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
