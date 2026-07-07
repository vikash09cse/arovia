-- Visit addon catalog and per-visit charge lines

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

INSERT INTO dbo.visit_addon_catalog (
    visitaddonid, tenantid, name, code, defaultamount, addonstatus, createdby, updatedby)
SELECT
    NEWID(),
    t.tenantid,
    N'UroMetric Test',
    N'UROMETRIC',
    300.00,
    1,
    actor.actorid,
    actor.actorid
FROM dbo.tenants t
CROSS APPLY (
    SELECT TOP 1 u.userid AS actorid
    FROM dbo.users u
    WHERE u.tenantid = t.tenantid
      AND u.isdeleted = 0
    ORDER BY CASE WHEN u.usertype = 1 THEN 0 ELSE 1 END, u.createdat
) actor
WHERE actor.actorid IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.visit_addon_catalog c
      WHERE c.tenantid = t.tenantid
        AND c.code = N'UROMETRIC');
GO
