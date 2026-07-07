IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'visit_addon_lines' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.visit_addon_lines (
        visitaddonlineid UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_visit_addon_lines PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid         UNIQUEIDENTIFIER NOT NULL,
        visitid          UNIQUEIDENTIFIER NOT NULL,
        visitaddonid     UNIQUEIDENTIFIER NOT NULL,
        addonname        NVARCHAR(200)    NOT NULL,
        amount           DECIMAL(18, 2)   NOT NULL,
        createdby        UNIQUEIDENTIFIER NOT NULL,
        createdat        DATETIME2        NOT NULL CONSTRAINT DF_visit_addon_lines_createdat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_visit_addon_lines_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_visit_addon_lines_visit FOREIGN KEY (visitid) REFERENCES dbo.visits (visitid),
        CONSTRAINT FK_visit_addon_lines_catalog FOREIGN KEY (visitaddonid) REFERENCES dbo.visit_addon_catalog (visitaddonid),
        CONSTRAINT FK_visit_addon_lines_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_visit_addon_lines_amount CHECK (amount >= 0)
    );

    CREATE UNIQUE INDEX UQ_visit_addon_lines_tenant_visit_addon
        ON dbo.visit_addon_lines (tenantid, visitid, visitaddonid);

    CREATE INDEX IX_visit_addon_lines_tenant_visit
        ON dbo.visit_addon_lines (tenantid, visitid);
END
GO
