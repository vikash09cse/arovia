-- Visits module: visit_sequences, visits, payments
-- Enums: VisitType 1=OPD 2=FollowUp 3=PreOp 4=Surgery
--        VisitFeeStatus 1=Charged 2=Free
--        VisitStatus 1=Active 2=Cancelled
--        PaymentLineType 1=Consultation 2=Procedure
--        PaymentStatus 1=Pending 2=Paid 3=Refunded

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'visit_sequences' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.visit_sequences (
        tenantid            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_visit_sequences PRIMARY KEY,
        nextsequencenumber  INT              NOT NULL CONSTRAINT DF_visit_sequences_next DEFAULT (1),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_visit_sequences_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_visit_sequences_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'visits' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.visits (
        visitid                     UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_visits PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid                    UNIQUEIDENTIFIER NOT NULL,
        patientid                   UNIQUEIDENTIFIER NOT NULL,
        visitcode                   NVARCHAR(20)     NOT NULL,
        sequencenumber              INT              NOT NULL,
        consultingdoctorid          UNIQUEIDENTIFIER NOT NULL,
        visitdatetime               DATETIME2        NOT NULL CONSTRAINT DF_visits_visitdatetime DEFAULT (SYSUTCDATETIME()),
        visittype                   TINYINT          NOT NULL CONSTRAINT DF_visits_visittype DEFAULT (1),
        purpose                     NVARCHAR(300)    NOT NULL,
        visitnotes                  NVARCHAR(1000)   NULL,
        scheduledsurgerydate        DATE             NULL,
        feestatus                   TINYINT          NOT NULL,
        feeamount                   DECIMAL(18, 2)   NULL,
        procedurechargeamount       DECIMAL(18, 2)   NULL,
        totalchargeamount           DECIMAL(18, 2)   NULL,
        visitstatus                 TINYINT          NOT NULL CONSTRAINT DF_visits_visitstatus DEFAULT (1),
        isfeeoverridden             BIT              NOT NULL CONSTRAINT DF_visits_isfeeoverridden DEFAULT (0),
        feeoverridereason           NVARCHAR(500)    NULL,
        cancellationreason          NVARCHAR(500)    NULL,
        freevisitwindowdayssnapshot INT              NOT NULL,
        dayssincelastcharged        INT              NULL,
        lastchargedvisitdatetime    DATETIME2        NULL,
        createdby                   UNIQUEIDENTIFIER NOT NULL,
        createdat                   DATETIME2        NOT NULL CONSTRAINT DF_visits_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby                   UNIQUEIDENTIFIER NOT NULL,
        updatedat                   DATETIME2        NOT NULL CONSTRAINT DF_visits_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_visits_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_visits_patient FOREIGN KEY (patientid) REFERENCES dbo.patients (patientid),
        CONSTRAINT FK_visits_doctor FOREIGN KEY (consultingdoctorid) REFERENCES dbo.users (userid)
    );

    CREATE UNIQUE INDEX UQ_visits_tenant_visitcode ON dbo.visits (tenantid, visitcode);
    CREATE INDEX IX_visits_tenant_patient_datetime ON dbo.visits (tenantid, patientid, visitdatetime DESC);
    CREATE INDEX IX_visits_tenant_patient_feestatus ON dbo.visits (tenantid, patientid, feestatus, visitdatetime DESC) WHERE visitstatus = 1;
END
GO

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

    CREATE UNIQUE INDEX UQ_payments_tenant_visit_linetype_active ON dbo.payments (tenantid, visitid, paymentlinetype) WHERE paymentstatus <> 3;
    CREATE INDEX IX_payments_tenant_status_created ON dbo.payments (tenantid, paymentstatus, createdat DESC);
END
GO
