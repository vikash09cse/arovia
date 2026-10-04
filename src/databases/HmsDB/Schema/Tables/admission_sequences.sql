IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'admission_sequences' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.admission_sequences (
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        sequencedate        DATE             NOT NULL,
        nextsequencenumber  INT              NOT NULL CONSTRAINT DF_admission_sequences_next DEFAULT (1),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_admission_sequences_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_admission_sequences PRIMARY KEY (tenantid, sequencedate),
        CONSTRAINT FK_admission_sequences_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );
END
GO
