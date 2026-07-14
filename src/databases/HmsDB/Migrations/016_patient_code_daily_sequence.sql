-- Patient numbers: YYYYMMDD-Serial (e.g. 20260714-01), serial resets each day per tenant.

IF COL_LENGTH('dbo.patient_sequences', 'sequencedate') IS NULL
BEGIN
    IF OBJECT_ID('dbo.FK_patient_sequences_tenant', 'F') IS NOT NULL
        ALTER TABLE dbo.patient_sequences DROP CONSTRAINT FK_patient_sequences_tenant;

    IF OBJECT_ID('dbo.PK_patient_sequences', 'PK') IS NOT NULL
        ALTER TABLE dbo.patient_sequences DROP CONSTRAINT PK_patient_sequences;

    DROP TABLE dbo.patient_sequences;

    CREATE TABLE dbo.patient_sequences (
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        sequencedate        DATE             NOT NULL,
        nextsequencenumber  INT              NOT NULL CONSTRAINT DF_patient_sequences_next DEFAULT (1),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_patient_sequences_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_patient_sequences PRIMARY KEY (tenantid, sequencedate),
        CONSTRAINT FK_patient_sequences_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );
END
GO
