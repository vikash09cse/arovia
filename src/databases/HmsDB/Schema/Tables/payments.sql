IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'payments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.payments (
        paymentid           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_payments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        visitid             UNIQUEIDENTIFIER NOT NULL,
        patientid           UNIQUEIDENTIFIER NOT NULL,
        paymentlinetype     TINYINT          NOT NULL,
        feeamount           DECIMAL(18, 2)   NOT NULL,
        paymentstatus       TINYINT          NOT NULL CONSTRAINT DF_payments_paymentstatus DEFAULT (1),
        receiptnumber       NVARCHAR(20)     NULL,
        amountpaid          DECIMAL(18, 2)   NULL,
        paymentmethod       TINYINT          NULL,
        notes               NVARCHAR(500)    NULL,
        collectedby         UNIQUEIDENTIFIER NULL,
        collectiondatetime  DATETIME2        NULL,
        createdby           UNIQUEIDENTIFIER NOT NULL,
        createdat           DATETIME2        NOT NULL CONSTRAINT DF_payments_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby           UNIQUEIDENTIFIER NOT NULL,
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_payments_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_payments_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_payments_visit FOREIGN KEY (visitid) REFERENCES dbo.visits (visitid),
        CONSTRAINT FK_payments_patient FOREIGN KEY (patientid) REFERENCES dbo.patients (patientid)
    );

    CREATE INDEX IX_payments_tenant_status_created
        ON dbo.payments (tenantid, paymentstatus, createdat DESC);
END
GO
