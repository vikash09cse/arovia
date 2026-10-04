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
        receiptnumber       NVARCHAR(20)     NULL,
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

-- Existing DBs: add column before any index that references it (separate batch).
IF OBJECT_ID(N'dbo.admission_payments', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.admission_payments', N'receiptnumber') IS NULL
BEGIN
    ALTER TABLE dbo.admission_payments ADD receiptnumber NVARCHAR(20) NULL;
END
GO

IF OBJECT_ID(N'dbo.admission_payments', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.admission_payments', N'receiptnumber') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UQ_admission_payments_tenant_receiptnumber'
          AND object_id = OBJECT_ID(N'dbo.admission_payments'))
BEGIN
    CREATE UNIQUE INDEX UQ_admission_payments_tenant_receiptnumber
        ON dbo.admission_payments (tenantid, receiptnumber)
        WHERE receiptnumber IS NOT NULL;
END
GO
