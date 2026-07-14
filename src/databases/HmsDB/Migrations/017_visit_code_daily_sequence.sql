-- Visit IDs: VIS-YYYYMMDD-Serial (e.g. VIS-20260714-01), serial resets each day per tenant.

IF COL_LENGTH('dbo.visit_sequences', 'sequencedate') IS NULL
BEGIN
    IF OBJECT_ID('dbo.FK_visit_sequences_tenant', 'F') IS NOT NULL
        ALTER TABLE dbo.visit_sequences DROP CONSTRAINT FK_visit_sequences_tenant;

    IF OBJECT_ID('dbo.PK_visit_sequences', 'PK') IS NOT NULL
        ALTER TABLE dbo.visit_sequences DROP CONSTRAINT PK_visit_sequences;

    DROP TABLE dbo.visit_sequences;

    CREATE TABLE dbo.visit_sequences (
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        sequencedate        DATE             NOT NULL,
        nextsequencenumber  INT              NOT NULL CONSTRAINT DF_visit_sequences_next DEFAULT (1),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_visit_sequences_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_visit_sequences PRIMARY KEY (tenantid, sequencedate),
        CONSTRAINT FK_visit_sequences_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.visits')
      AND name = N'visitcode'
      AND max_length = 40 -- NVARCHAR(20) stores as 40 bytes
)
BEGIN
    ALTER TABLE dbo.visits ALTER COLUMN visitcode NVARCHAR(30) NOT NULL;
END
GO
