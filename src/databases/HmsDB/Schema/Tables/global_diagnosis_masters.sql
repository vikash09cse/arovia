IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'global_diagnosis_masters' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.global_diagnosis_masters (
        globaldiagnosismasterid UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_global_diagnosis_masters PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        code                NVARCHAR(50)     NOT NULL,
        name                NVARCHAR(300)    NOT NULL,
        packjson            NVARCHAR(MAX)    NULL,
        sortorder           INT              NOT NULL CONSTRAINT DF_global_diagnosis_masters_sort DEFAULT (0),
        isactive            BIT              NOT NULL CONSTRAINT DF_global_diagnosis_masters_active DEFAULT (1),
        createdat           DATETIME2        NOT NULL CONSTRAINT DF_global_diagnosis_masters_createdat DEFAULT (SYSUTCDATETIME()),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_global_diagnosis_masters_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_global_diagnosis_masters_code UNIQUE (code),
        CONSTRAINT CK_global_diagnosis_masters_packjson
            CHECK (packjson IS NULL OR ISJSON(packjson) = 1)
    );
END
GO
