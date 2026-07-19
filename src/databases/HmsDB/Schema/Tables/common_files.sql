IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'common_files' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.common_files (
        commonfileid   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_common_files PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid       UNIQUEIDENTIFIER NOT NULL,
        displayname    NVARCHAR(260)    NOT NULL,
        storedfilename NVARCHAR(260)    NOT NULL,
        isdeleted      BIT              NOT NULL CONSTRAINT DF_common_files_isdeleted DEFAULT (0),
        createdby      UNIQUEIDENTIFIER NOT NULL,
        createdat      DATETIME2        NOT NULL CONSTRAINT DF_common_files_createdat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_common_files_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_common_files_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid)
    );

    CREATE INDEX IX_common_files_tenant_created
        ON dbo.common_files (tenantid, createdat DESC)
        WHERE isdeleted = 0;

    CREATE UNIQUE INDEX UQ_common_files_tenant_stored
        ON dbo.common_files (tenantid, storedfilename)
        WHERE isdeleted = 0;
END
GO
