IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'admissions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.admissions (
        admissionid         UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_admissions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        admissioncode       NVARCHAR(30)     NOT NULL,
        sequencenumber      INT              NOT NULL,
        patientid           UNIQUEIDENTIFIER NOT NULL,
        departmentid        UNIQUEIDENTIFIER NOT NULL,
        attendingdoctorid   UNIQUEIDENTIFIER NOT NULL,
        admittedat          DATETIME2        NOT NULL CONSTRAINT DF_admissions_admittedat DEFAULT (SYSUTCDATETIME()),
        dischargedat        DATETIME2        NULL,
        ward                NVARCHAR(100)    NOT NULL,
        bed                 NVARCHAR(50)     NULL,
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
        isdeleted           BIT              NOT NULL CONSTRAINT DF_admissions_isdeleted DEFAULT (0),
        CONSTRAINT FK_admissions_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_admissions_patient FOREIGN KEY (patientid) REFERENCES dbo.patients (patientid),
        CONSTRAINT FK_admissions_department FOREIGN KEY (departmentid) REFERENCES dbo.departments (departmentid),
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

    CREATE INDEX IX_admissions_tenant_department
        ON dbo.admissions (tenantid, departmentid);

    -- Dynamic SQL avoids compile-time binding if isdeleted is missing on older DBs.
    EXEC(N'
        CREATE UNIQUE INDEX UQ_admissions_tenant_patient_active
            ON dbo.admissions (tenantid, patientid)
            WHERE admissionstatus = 1 AND isdeleted = 0;
    ');
END
GO

-- Existing DBs: add soft-delete column before any filtered index that references it.
IF COL_LENGTH(N'dbo.admissions', N'isdeleted') IS NULL
   AND OBJECT_ID(N'dbo.admissions', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.admissions
        ADD isdeleted BIT NOT NULL
            CONSTRAINT DF_admissions_isdeleted DEFAULT (0);
END
GO

-- Refresh unique active-stay index to exclude soft-deleted rows (dynamic SQL for bind safety).
IF OBJECT_ID(N'dbo.admissions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.admissions', N'isdeleted') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UQ_admissions_tenant_patient_active'
          AND object_id = OBJECT_ID(N'dbo.admissions'))
        DROP INDEX UQ_admissions_tenant_patient_active ON dbo.admissions;

    EXEC(N'
        CREATE UNIQUE INDEX UQ_admissions_tenant_patient_active
            ON dbo.admissions (tenantid, patientid)
            WHERE admissionstatus = 1 AND isdeleted = 0;
    ');
END
GO

IF COL_LENGTH(N'dbo.admissions', N'departmentid') IS NULL
   AND OBJECT_ID(N'dbo.departments', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.admissions ADD departmentid UNIQUEIDENTIFIER NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.admissions')
      AND name = N'bed'
      AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.admissions ALTER COLUMN bed NVARCHAR(50) NULL;
END
GO
