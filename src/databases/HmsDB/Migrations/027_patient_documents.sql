-- Patient-scoped document uploads (PDF / images / Word).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'patient_documents' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.patient_documents (
        patientdocumentid UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_patient_documents PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid          UNIQUEIDENTIFIER NOT NULL,
        patientid         UNIQUEIDENTIFIER NOT NULL,
        displayname       NVARCHAR(260)    NOT NULL,
        storedfilename    NVARCHAR(260)    NOT NULL,
        isdeleted         BIT              NOT NULL CONSTRAINT DF_patient_documents_isdeleted DEFAULT (0),
        createdby         UNIQUEIDENTIFIER NOT NULL,
        createdat         DATETIME2        NOT NULL CONSTRAINT DF_patient_documents_createdat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_patient_documents_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_patient_documents_patient FOREIGN KEY (patientid) REFERENCES dbo.patients (patientid),
        CONSTRAINT FK_patient_documents_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid)
    );

    CREATE INDEX IX_patient_documents_tenant_patient_created
        ON dbo.patient_documents (tenantid, patientid, createdat DESC)
        WHERE isdeleted = 0;

    CREATE UNIQUE INDEX UQ_patient_documents_tenant_patient_stored
        ON dbo.patient_documents (tenantid, patientid, storedfilename)
        WHERE isdeleted = 0;
END
GO
