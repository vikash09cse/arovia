IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'diagnosis_masters' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.diagnosis_masters (
        diagnosismasterid       UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_diagnosis_masters PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid                UNIQUEIDENTIFIER NOT NULL,
        globaldiagnosismasterid UNIQUEIDENTIFIER NULL,
        code                    NVARCHAR(50)     NOT NULL,
        name                    NVARCHAR(300)    NOT NULL,
        packjson                NVARCHAR(MAX)    NULL,
        sortorder               INT              NOT NULL CONSTRAINT DF_diagnosis_masters_sort DEFAULT (0),
        isactive                BIT              NOT NULL CONSTRAINT DF_diagnosis_masters_active DEFAULT (1),
        createdat               DATETIME2        NOT NULL CONSTRAINT DF_diagnosis_masters_createdat DEFAULT (SYSUTCDATETIME()),
        updatedat               DATETIME2        NOT NULL CONSTRAINT DF_diagnosis_masters_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_diagnosis_masters_tenant
            FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_diagnosis_masters_global
            FOREIGN KEY (globaldiagnosismasterid) REFERENCES dbo.global_diagnosis_masters (globaldiagnosismasterid),
        CONSTRAINT UQ_diagnosis_masters_tenant_code UNIQUE (tenantid, code),
        CONSTRAINT CK_diagnosis_masters_packjson
            CHECK (packjson IS NULL OR ISJSON(packjson) = 1)
    );

    CREATE INDEX IX_diagnosis_masters_tenant_active_sort
        ON dbo.diagnosis_masters (tenantid, isactive, sortorder, name);
END
GO
