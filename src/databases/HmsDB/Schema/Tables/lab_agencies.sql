IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'lab_agencies' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.lab_agencies (
        labagencyid     UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_lab_agencies PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid        UNIQUEIDENTIFIER NOT NULL,
        name            NVARCHAR(200)    NOT NULL,
        contactperson   NVARCHAR(100)    NULL,
        phone           NVARCHAR(20)     NULL,
        email           NVARCHAR(256)    NULL,
        address         NVARCHAR(500)    NULL,
        notes           NVARCHAR(500)    NULL,
        agencystatus    TINYINT          NOT NULL CONSTRAINT DF_lab_agencies_agencystatus DEFAULT (1),
        createdby       UNIQUEIDENTIFIER NOT NULL,
        createdat       DATETIME2        NOT NULL CONSTRAINT DF_lab_agencies_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby       UNIQUEIDENTIFIER NOT NULL,
        updatedat       DATETIME2        NOT NULL CONSTRAINT DF_lab_agencies_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_lab_agencies_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );

    CREATE INDEX IX_lab_agencies_tenant_status
        ON dbo.lab_agencies (tenantid, agencystatus, name);

    CREATE UNIQUE INDEX UQ_lab_agencies_tenant_name_active
        ON dbo.lab_agencies (tenantid, name)
        WHERE agencystatus = 1;
END
GO
