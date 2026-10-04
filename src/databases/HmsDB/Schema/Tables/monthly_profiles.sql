IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'monthly_profiles' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.monthly_profiles (
        monthlyprofileid UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_monthly_profiles PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid         UNIQUEIDENTIFIER NOT NULL,
        yearmonth        CHAR(7)          NOT NULL,
        profilestatus    TINYINT          NOT NULL CONSTRAINT DF_monthly_profiles_status DEFAULT (1),
        opdrevenue       DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_monthly_profiles_opd DEFAULT (0),
        ipdrevenue       DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_monthly_profiles_ipd DEFAULT (0),
        totalrevenue     DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_monthly_profiles_revenue DEFAULT (0),
        totalexpenses    DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_monthly_profiles_expenses DEFAULT (0),
        profit           DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_monthly_profiles_profit DEFAULT (0),
        note             NVARCHAR(500)    NULL,
        reconciledat     DATETIME2        NULL,
        reconciledby     UNIQUEIDENTIFIER NULL,
        createdby        UNIQUEIDENTIFIER NOT NULL,
        createdat        DATETIME2        NOT NULL CONSTRAINT DF_monthly_profiles_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby        UNIQUEIDENTIFIER NOT NULL,
        updatedat        DATETIME2        NOT NULL CONSTRAINT DF_monthly_profiles_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_monthly_profiles_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_monthly_profiles_reconciledby FOREIGN KEY (reconciledby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_monthly_profiles_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_monthly_profiles_updatedby FOREIGN KEY (updatedby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_monthly_profiles_status CHECK (profilestatus IN (1, 2)),
        CONSTRAINT CK_monthly_profiles_yearmonth CHECK (
            yearmonth LIKE '[0-9][0-9][0-9][0-9]-[0-1][0-9]'
            AND SUBSTRING(yearmonth, 6, 2) BETWEEN '01' AND '12')
    );

    CREATE UNIQUE INDEX UQ_monthly_profiles_tenant_yearmonth
        ON dbo.monthly_profiles (tenantid, yearmonth);

    CREATE INDEX IX_monthly_profiles_tenant_status
        ON dbo.monthly_profiles (tenantid, profilestatus, yearmonth DESC);
END
GO
