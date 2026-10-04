-- IPD admissions module (parallel to visits / visit payments).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'admission_sequences' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.admission_sequences (
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        sequencedate        DATE             NOT NULL,
        nextsequencenumber  INT              NOT NULL CONSTRAINT DF_admission_sequences_next DEFAULT (1),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_admission_sequences_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_admission_sequences PRIMARY KEY (tenantid, sequencedate),
        CONSTRAINT FK_admission_sequences_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'admissions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.admissions (
        admissionid         UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_admissions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        admissioncode       NVARCHAR(30)     NOT NULL,
        sequencenumber      INT              NOT NULL,
        patientid           UNIQUEIDENTIFIER NOT NULL,
        attendingdoctorid   UNIQUEIDENTIFIER NOT NULL,
        admittedat          DATETIME2        NOT NULL CONSTRAINT DF_admissions_admittedat DEFAULT (SYSUTCDATETIME()),
        dischargedat        DATETIME2        NULL,
        ward                NVARCHAR(100)    NOT NULL,
        bed                 NVARCHAR(50)     NOT NULL,
        roomclass           NVARCHAR(50)     NOT NULL,
        admissionstatus     TINYINT          NOT NULL CONSTRAINT DF_admissions_status DEFAULT (1),
        estimatedamount     DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_admissions_estimated DEFAULT (0),
        notes               NVARCHAR(1000)   NULL,
        discountamount      DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_admissions_discount DEFAULT (0),
        discountreason      NVARCHAR(500)    NULL,
        fromvisitid         UNIQUEIDENTIFIER NULL,
        createdby           UNIQUEIDENTIFIER NOT NULL,
        createdat           DATETIME2        NOT NULL CONSTRAINT DF_admissions_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby           UNIQUEIDENTIFIER NOT NULL,
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_admissions_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_admissions_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_admissions_patient FOREIGN KEY (patientid) REFERENCES dbo.patients (patientid),
        CONSTRAINT FK_admissions_doctor FOREIGN KEY (attendingdoctorid) REFERENCES dbo.users (userid),
        CONSTRAINT FK_admissions_fromvisit FOREIGN KEY (fromvisitid) REFERENCES dbo.visits (visitid),
        CONSTRAINT FK_admissions_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_admissions_updatedby FOREIGN KEY (updatedby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_admissions_status CHECK (admissionstatus IN (1, 2, 3)),
        CONSTRAINT CK_admissions_estimated CHECK (estimatedamount >= 0),
        CONSTRAINT CK_admissions_discount CHECK (discountamount >= 0),
        CONSTRAINT CK_admissions_discharge CHECK (dischargedat IS NULL OR dischargedat >= admittedat)
    );

    CREATE UNIQUE INDEX UQ_admissions_tenant_code
        ON dbo.admissions (tenantid, admissioncode);

    CREATE INDEX IX_admissions_tenant_status_admitted
        ON dbo.admissions (tenantid, admissionstatus, admittedat DESC);

    CREATE INDEX IX_admissions_tenant_patient
        ON dbo.admissions (tenantid, patientid, admittedat DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'admission_charges' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.admission_charges (
        admissionchargeid   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_admission_charges PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        admissionid         UNIQUEIDENTIFIER NOT NULL,
        chargecategory      TINYINT          NOT NULL,
        description         NVARCHAR(250)    NOT NULL,
        amount              DECIMAL(18, 2)   NOT NULL,
        chargedon           DATETIME2        NOT NULL CONSTRAINT DF_admission_charges_chargedon DEFAULT (SYSUTCDATETIME()),
        createdby           UNIQUEIDENTIFIER NOT NULL,
        createdat           DATETIME2        NOT NULL CONSTRAINT DF_admission_charges_createdat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_admission_charges_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_admission_charges_admission FOREIGN KEY (admissionid) REFERENCES dbo.admissions (admissionid),
        CONSTRAINT FK_admission_charges_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_admission_charges_category CHECK (chargecategory BETWEEN 1 AND 6),
        CONSTRAINT CK_admission_charges_amount CHECK (amount >= 0)
    );

    CREATE INDEX IX_admission_charges_tenant_admission
        ON dbo.admission_charges (tenantid, admissionid, chargedon DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'admission_payments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.admission_payments (
        admissionpaymentid  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_admission_payments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        admissionid         UNIQUEIDENTIFIER NOT NULL,
        patientid           UNIQUEIDENTIFIER NOT NULL,
        amount              DECIMAL(18, 2)   NOT NULL,
        paymentmethod       TINYINT          NOT NULL,
        paymentkind         TINYINT          NOT NULL,
        notes               NVARCHAR(500)    NULL,
        collectedby         UNIQUEIDENTIFIER NOT NULL,
        collectiondatetime  DATETIME2        NOT NULL CONSTRAINT DF_admission_payments_collectedat DEFAULT (SYSUTCDATETIME()),
        createdby           UNIQUEIDENTIFIER NOT NULL,
        createdat           DATETIME2        NOT NULL CONSTRAINT DF_admission_payments_createdat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_admission_payments_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_admission_payments_admission FOREIGN KEY (admissionid) REFERENCES dbo.admissions (admissionid),
        CONSTRAINT FK_admission_payments_patient FOREIGN KEY (patientid) REFERENCES dbo.patients (patientid),
        CONSTRAINT FK_admission_payments_collector FOREIGN KEY (collectedby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_admission_payments_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_admission_payments_amount CHECK (amount > 0),
        CONSTRAINT CK_admission_payments_method CHECK (paymentmethod BETWEEN 1 AND 4),
        CONSTRAINT CK_admission_payments_kind CHECK (paymentkind IN (1, 2, 3))
    );

    CREATE INDEX IX_admission_payments_tenant_admission
        ON dbo.admission_payments (tenantid, admissionid, collectiondatetime DESC);
END
GO
