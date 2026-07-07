IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'patients' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.patients (
        patientid               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_patients PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid                UNIQUEIDENTIFIER NOT NULL,
        patientcode             NVARCHAR(20)     NOT NULL,
        sequencenumber          INT              NOT NULL,
        firstname               NVARCHAR(100)    NOT NULL,
        lastname                NVARCHAR(100)    NOT NULL,
        dateofbirth             DATE             NULL,
        age                     INT              NULL,
        gender                  TINYINT          NOT NULL,
        bloodgroup              TINYINT          NULL,
        referredby              NVARCHAR(100)    NULL,
        patientstatus           TINYINT          NOT NULL CONSTRAINT DF_patients_patientstatus DEFAULT (1),
        phonecipher             VARBINARY(512)   NOT NULL,
        emailcipher             VARBINARY(512)   NULL,
        addresscipher           VARBINARY(2048)  NOT NULL,
        emergencynamecipher     VARBINARY(512)   NULL,
        emergencyphonecipher    VARBINARY(512)   NULL,
        phoneblindindex         BINARY(32)       NOT NULL,
        emailblindindex         BINARY(32)       NULL,
        registeredby            UNIQUEIDENTIFIER NOT NULL,
        isdeleted               BIT              NOT NULL CONSTRAINT DF_patients_isdeleted DEFAULT (0),
        createdby               UNIQUEIDENTIFIER NOT NULL,
        createdat               DATETIME2        NOT NULL CONSTRAINT DF_patients_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby               UNIQUEIDENTIFIER NOT NULL,
        updatedat               DATETIME2        NOT NULL CONSTRAINT DF_patients_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_patients_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );

    CREATE UNIQUE INDEX UQ_patients_tenant_patientcode
        ON dbo.patients (tenantid, patientcode)
        WHERE isdeleted = 0;

    CREATE UNIQUE INDEX UQ_patients_tenant_phoneblind
        ON dbo.patients (tenantid, phoneblindindex)
        WHERE isdeleted = 0;

    CREATE INDEX IX_patients_tenant_name
        ON dbo.patients (tenantid, lastname, firstname)
        WHERE isdeleted = 0;

    CREATE INDEX IX_patients_tenant_status_created
        ON dbo.patients (tenantid, patientstatus, createdat DESC)
        WHERE isdeleted = 0;
END
GO
