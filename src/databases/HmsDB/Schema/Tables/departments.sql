IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'departments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.departments (
        departmentid       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_departments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid           UNIQUEIDENTIFIER NOT NULL,
        name               NVARCHAR(150)    NOT NULL,
        departmentstatus   TINYINT          NOT NULL CONSTRAINT DF_departments_status DEFAULT (1),
        createdby          UNIQUEIDENTIFIER NOT NULL,
        createdat          DATETIME2        NOT NULL CONSTRAINT DF_departments_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby          UNIQUEIDENTIFIER NOT NULL,
        updatedat          DATETIME2        NOT NULL CONSTRAINT DF_departments_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_departments_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_departments_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_departments_updatedby FOREIGN KEY (updatedby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_departments_status CHECK (departmentstatus IN (1, 2))
    );

    CREATE UNIQUE INDEX UQ_departments_tenant_name
        ON dbo.departments (tenantid, name);

    CREATE INDEX IX_departments_tenant_status
        ON dbo.departments (tenantid, departmentstatus, name);
END
GO

IF COL_LENGTH(N'dbo.users', N'departmentid') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD departmentid UNIQUEIDENTIFIER NULL;
END
GO

IF COL_LENGTH(N'dbo.users', N'departmentid') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_users_department')
BEGIN
    ALTER TABLE dbo.users
        ADD CONSTRAINT FK_users_department
        FOREIGN KEY (departmentid) REFERENCES dbo.departments (departmentid);
END
GO
