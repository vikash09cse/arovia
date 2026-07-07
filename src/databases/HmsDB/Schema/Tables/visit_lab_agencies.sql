IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'visit_lab_agencies' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.visit_lab_agencies (
        visitlabagencyid UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_visit_lab_agencies PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid         UNIQUEIDENTIFIER NOT NULL,
        visitid          UNIQUEIDENTIFIER NOT NULL,
        labagencyid      UNIQUEIDENTIFIER NOT NULL,
        assignedat       DATETIME2        NOT NULL CONSTRAINT DF_visit_lab_agencies_assignedat DEFAULT (SYSUTCDATETIME()),
        assignedby       UNIQUEIDENTIFIER NOT NULL,
        notes            NVARCHAR(500)    NULL,
        CONSTRAINT FK_visit_lab_agencies_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_visit_lab_agencies_visit FOREIGN KEY (visitid) REFERENCES dbo.visits (visitid),
        CONSTRAINT FK_visit_lab_agencies_agency FOREIGN KEY (labagencyid) REFERENCES dbo.lab_agencies (labagencyid),
        CONSTRAINT FK_visit_lab_agencies_assignedby FOREIGN KEY (assignedby) REFERENCES dbo.users (userid)
    );

    CREATE UNIQUE INDEX UQ_visit_lab_agencies_tenant_visit_agency
        ON dbo.visit_lab_agencies (tenantid, visitid, labagencyid);

    CREATE INDEX IX_visit_lab_agencies_tenant_visit
        ON dbo.visit_lab_agencies (tenantid, visitid, assignedat DESC);
END
GO
