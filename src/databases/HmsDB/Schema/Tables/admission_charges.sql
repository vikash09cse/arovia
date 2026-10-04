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
