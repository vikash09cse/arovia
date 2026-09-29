IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'user_documents' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.user_documents (
        userdocumentid  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_user_documents PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid        UNIQUEIDENTIFIER NOT NULL,
        userid          UNIQUEIDENTIFIER NOT NULL,
        documenttype    TINYINT          NOT NULL,
        displayname     NVARCHAR(260)    NOT NULL,
        storedfilename  NVARCHAR(260)    NOT NULL,
        isdeleted       BIT              NOT NULL CONSTRAINT DF_user_documents_isdeleted DEFAULT (0),
        createdby       UNIQUEIDENTIFIER NOT NULL,
        createdat       DATETIME2        NOT NULL CONSTRAINT DF_user_documents_createdat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_user_documents_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_user_documents_user FOREIGN KEY (userid) REFERENCES dbo.users (userid),
        CONSTRAINT FK_user_documents_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_user_documents_type CHECK (documenttype IN (1, 2, 3, 4))
    );

    CREATE INDEX IX_user_documents_tenant_user_created
        ON dbo.user_documents (tenantid, userid, createdat DESC)
        WHERE isdeleted = 0;

    CREATE UNIQUE INDEX UQ_user_documents_tenant_user_stored
        ON dbo.user_documents (tenantid, userid, storedfilename)
        WHERE isdeleted = 0;
END
GO
