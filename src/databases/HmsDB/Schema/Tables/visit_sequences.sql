IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'visit_sequences' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.visit_sequences (
        tenantid            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_visit_sequences PRIMARY KEY,
        nextsequencenumber  INT              NOT NULL CONSTRAINT DF_visit_sequences_next DEFAULT (1),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_visit_sequences_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_visit_sequences_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );
END
GO
