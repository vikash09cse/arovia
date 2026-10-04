IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'expenses' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.expenses (
        expenseid       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_expenses PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid        UNIQUEIDENTIFIER NOT NULL,
        amount          DECIMAL(18, 2)   NOT NULL,
        expenseon       DATE             NOT NULL,
        category        NVARCHAR(50)     NOT NULL,
        note            NVARCHAR(500)    NULL,
        staffuserid     UNIQUEIDENTIFIER NULL,
        isdeleted       BIT              NOT NULL CONSTRAINT DF_expenses_isdeleted DEFAULT (0),
        createdby       UNIQUEIDENTIFIER NOT NULL,
        createdat       DATETIME2        NOT NULL CONSTRAINT DF_expenses_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby       UNIQUEIDENTIFIER NOT NULL,
        updatedat       DATETIME2        NOT NULL CONSTRAINT DF_expenses_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_expenses_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_expenses_staff FOREIGN KEY (staffuserid) REFERENCES dbo.users (userid),
        CONSTRAINT FK_expenses_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_expenses_updatedby FOREIGN KEY (updatedby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_expenses_amount CHECK (amount > 0)
    );

    CREATE INDEX IX_expenses_tenant_expenseon
        ON dbo.expenses (tenantid, expenseon DESC)
        WHERE isdeleted = 0;

    CREATE INDEX IX_expenses_tenant_category
        ON dbo.expenses (tenantid, category, expenseon DESC)
        WHERE isdeleted = 0;
END
GO
