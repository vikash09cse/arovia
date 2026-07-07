IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'visit_addon_catalog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.visit_addon_catalog (
        visitaddonid    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_visit_addon_catalog PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid        UNIQUEIDENTIFIER NOT NULL,
        name            NVARCHAR(200)    NOT NULL,
        code            NVARCHAR(50)     NULL,
        defaultamount   DECIMAL(18, 2)   NOT NULL,
        addonstatus     TINYINT          NOT NULL CONSTRAINT DF_visit_addon_catalog_status DEFAULT (1),
        createdby       UNIQUEIDENTIFIER NOT NULL,
        createdat       DATETIME2        NOT NULL CONSTRAINT DF_visit_addon_catalog_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby       UNIQUEIDENTIFIER NOT NULL,
        updatedat       DATETIME2        NOT NULL CONSTRAINT DF_visit_addon_catalog_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_visit_addon_catalog_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT CK_visit_addon_catalog_amount CHECK (defaultamount >= 0)
    );

    CREATE INDEX IX_visit_addon_catalog_tenant_status
        ON dbo.visit_addon_catalog (tenantid, addonstatus, name);

    CREATE UNIQUE INDEX UQ_visit_addon_catalog_tenant_code_active
        ON dbo.visit_addon_catalog (tenantid, code)
        WHERE addonstatus = 1 AND code IS NOT NULL;
END
GO
