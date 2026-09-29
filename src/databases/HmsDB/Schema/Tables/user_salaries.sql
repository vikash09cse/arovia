IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'user_salaries' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.user_salaries (
        usersalaryid    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_user_salaries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid        UNIQUEIDENTIFIER NOT NULL,
        userid          UNIQUEIDENTIFIER NOT NULL,
        monthlysalary   DECIMAL(18,2)    NOT NULL,
        effectivefrom   DATE             NOT NULL,
        effectiveto     DATE             NULL,
        notes           NVARCHAR(500)    NULL,
        isdeleted       BIT              NOT NULL CONSTRAINT DF_user_salaries_isdeleted DEFAULT (0),
        createdby       UNIQUEIDENTIFIER NOT NULL,
        createdat       DATETIME2        NOT NULL CONSTRAINT DF_user_salaries_createdat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_user_salaries_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_user_salaries_user FOREIGN KEY (userid) REFERENCES dbo.users (userid),
        CONSTRAINT FK_user_salaries_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_user_salaries_amount CHECK (monthlysalary >= 0),
        CONSTRAINT CK_user_salaries_period CHECK (effectiveto IS NULL OR effectiveto >= effectivefrom)
    );

    CREATE INDEX IX_user_salaries_tenant_user_from
        ON dbo.user_salaries (tenantid, userid, effectivefrom DESC)
        WHERE isdeleted = 0;

    CREATE UNIQUE INDEX UQ_user_salaries_tenant_user_open
        ON dbo.user_salaries (tenantid, userid)
        WHERE isdeleted = 0 AND effectiveto IS NULL;
END
GO
