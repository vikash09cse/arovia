IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'invoice_sequences' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.invoice_sequences (
        tenantid            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_invoice_sequences PRIMARY KEY,
        nextsequencenumber  INT              NOT NULL CONSTRAINT DF_invoice_sequences_next DEFAULT (1),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_invoice_sequences_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_invoice_sequences_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );
END
GO
