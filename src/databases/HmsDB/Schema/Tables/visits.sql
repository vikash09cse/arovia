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
        discountamount              DECIMAL(18, 2)   NULL,
        discountreason              NVARCHAR(500)    NULL,
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

    CREATE UNIQUE INDEX UQ_visits_tenant_visitcode
        ON dbo.visits (tenantid, visitcode);

    CREATE INDEX IX_visits_tenant_patient_datetime
        ON dbo.visits (tenantid, patientid, visitdatetime DESC);

    CREATE INDEX IX_visits_tenant_patient_feestatus
        ON dbo.visits (tenantid, patientid, feestatus, visitdatetime DESC)
        WHERE visitstatus = 1;
END
GO
