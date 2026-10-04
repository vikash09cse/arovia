IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'discharge_summaries' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.discharge_summaries (
        dischargesummaryid  UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_discharge_summaries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        admissionid         UNIQUEIDENTIFIER NOT NULL,

        dateofsurgery       DATE             NULL,
        dateofdischarge     DATE             NULL,
        finaldiagnosis      NVARCHAR(500)    NULL,
        diagnosiskey        NVARCHAR(100)    NULL,

        formschemaversion   INT              NOT NULL
            CONSTRAINT DF_discharge_summaries_formschemaversion DEFAULT (1),
        formjson            NVARCHAR(MAX)    NOT NULL
            CONSTRAINT DF_discharge_summaries_formjson DEFAULT (N'{}'),

        createdby           UNIQUEIDENTIFIER NOT NULL,
        createdat           DATETIME2        NOT NULL
            CONSTRAINT DF_discharge_summaries_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby           UNIQUEIDENTIFIER NOT NULL,
        updatedat           DATETIME2        NOT NULL
            CONSTRAINT DF_discharge_summaries_updatedat DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_discharge_summaries_tenant
            FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_discharge_summaries_admission
            FOREIGN KEY (admissionid) REFERENCES dbo.admissions (admissionid),
        CONSTRAINT FK_discharge_summaries_createdby
            FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_discharge_summaries_updatedby
            FOREIGN KEY (updatedby) REFERENCES dbo.users (userid),
        CONSTRAINT UQ_discharge_summaries_tenant_admission UNIQUE (tenantid, admissionid),
        CONSTRAINT CK_discharge_summaries_formjson CHECK (ISJSON(formjson) = 1),
        CONSTRAINT CK_discharge_summaries_formschemaversion CHECK (formschemaversion >= 1)
    );

    CREATE INDEX IX_discharge_summaries_tenant_updated
        ON dbo.discharge_summaries (tenantid, updatedat DESC);

    CREATE INDEX IX_discharge_summaries_tenant_diagnosis
        ON dbo.discharge_summaries (tenantid, finaldiagnosis)
        WHERE finaldiagnosis IS NOT NULL;
END
GO
