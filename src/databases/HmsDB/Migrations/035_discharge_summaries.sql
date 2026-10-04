-- Clinical discharge summaries + global/tenant diagnosis masters (Urology seed).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'global_diagnosis_masters' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.global_diagnosis_masters (
        globaldiagnosismasterid UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_global_diagnosis_masters PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        code                NVARCHAR(50)     NOT NULL,
        name                NVARCHAR(300)    NOT NULL,
        packjson            NVARCHAR(MAX)    NULL,
        sortorder           INT              NOT NULL CONSTRAINT DF_global_diagnosis_masters_sort DEFAULT (0),
        isactive            BIT              NOT NULL CONSTRAINT DF_global_diagnosis_masters_active DEFAULT (1),
        createdat           DATETIME2        NOT NULL CONSTRAINT DF_global_diagnosis_masters_createdat DEFAULT (SYSUTCDATETIME()),
        updatedat           DATETIME2        NOT NULL CONSTRAINT DF_global_diagnosis_masters_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_global_diagnosis_masters_code UNIQUE (code),
        CONSTRAINT CK_global_diagnosis_masters_packjson
            CHECK (packjson IS NULL OR ISJSON(packjson) = 1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'diagnosis_masters' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.diagnosis_masters (
        diagnosismasterid       UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_diagnosis_masters PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid                UNIQUEIDENTIFIER NOT NULL,
        globaldiagnosismasterid UNIQUEIDENTIFIER NULL,
        code                    NVARCHAR(50)     NOT NULL,
        name                    NVARCHAR(300)    NOT NULL,
        packjson                NVARCHAR(MAX)    NULL,
        sortorder               INT              NOT NULL CONSTRAINT DF_diagnosis_masters_sort DEFAULT (0),
        isactive                BIT              NOT NULL CONSTRAINT DF_diagnosis_masters_active DEFAULT (1),
        createdat               DATETIME2        NOT NULL CONSTRAINT DF_diagnosis_masters_createdat DEFAULT (SYSUTCDATETIME()),
        updatedat               DATETIME2        NOT NULL CONSTRAINT DF_diagnosis_masters_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_diagnosis_masters_tenant
            FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_diagnosis_masters_global
            FOREIGN KEY (globaldiagnosismasterid) REFERENCES dbo.global_diagnosis_masters (globaldiagnosismasterid),
        CONSTRAINT UQ_diagnosis_masters_tenant_code UNIQUE (tenantid, code),
        CONSTRAINT CK_diagnosis_masters_packjson
            CHECK (packjson IS NULL OR ISJSON(packjson) = 1)
    );

    CREATE INDEX IX_diagnosis_masters_tenant_active_sort
        ON dbo.diagnosis_masters (tenantid, isactive, sortorder, name);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'discharge_summaries' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.discharge_summaries (
        dischargesummaryid  UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_discharge_summaries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid            UNIQUEIDENTIFIER NOT NULL,
        admissionid         UNIQUEIDENTIFIER NOT NULL,
        dateofsurgery       DATE             NULL,
        dateofdischarge     DATE             NULL,
        finaldiagnosis      NVARCHAR(500)    NULL,
        diagnosiskey        NVARCHAR(100)    NULL,
        formschemaversion   INT              NOT NULL
            CONSTRAINT DF_discharge_summaries_formschemaversion DEFAULT (1),
        formjson            NVARCHAR(MAX)    NOT NULL
            CONSTRAINT DF_discharge_summaries_formjson DEFAULT (N'{}'),
        createdby           UNIQUEIDENTIFIER NOT NULL,
        createdat           DATETIME2        NOT NULL
            CONSTRAINT DF_discharge_summaries_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby           UNIQUEIDENTIFIER NOT NULL,
        updatedat           DATETIME2        NOT NULL
            CONSTRAINT DF_discharge_summaries_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_discharge_summaries_tenant
            FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_discharge_summaries_admission
            FOREIGN KEY (admissionid) REFERENCES dbo.admissions (admissionid),
        CONSTRAINT FK_discharge_summaries_createdby
            FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_discharge_summaries_updatedby
            FOREIGN KEY (updatedby) REFERENCES dbo.users (userid),
        CONSTRAINT UQ_discharge_summaries_tenant_admission UNIQUE (tenantid, admissionid),
        CONSTRAINT CK_discharge_summaries_formjson CHECK (ISJSON(formjson) = 1),
        CONSTRAINT CK_discharge_summaries_formschemaversion CHECK (formschemaversion >= 1)
    );

    CREATE INDEX IX_discharge_summaries_tenant_updated
        ON dbo.discharge_summaries (tenantid, updatedat DESC);

    CREATE INDEX IX_discharge_summaries_tenant_diagnosis
        ON dbo.discharge_summaries (tenantid, finaldiagnosis)
        WHERE finaldiagnosis IS NOT NULL;
END
GO

-- Seed global Urology diagnosis packs (idempotent by code)
IF NOT EXISTS (SELECT 1 FROM dbo.global_diagnosis_masters WHERE code = N'boo_bpe_stricture')
INSERT INTO dbo.global_diagnosis_masters (code, name, packjson, sortorder) VALUES (
    N'boo_bpe_stricture',
    N'BOO due to BPE + Bulbar stricture',
    N'{"procedureKey":"TURP","procedureExtras":{"bpeFindings":"Gd2–3 trilobar with large median lobe","resection":"Median lobe then R then L lobe","chips":"Evacuated, sent for HPE","catheter":"22F 3-way then 16F PUC","irrigation":"Started postop"},"chiefComplaints":"Burning micturition. Mixed LUTS. Urge incontinence.","briefHistory":"Known BPH. No hematuria/fever. Comorbidity-Nil. No addiction.","catheterKey":"16F PUC in situ","specimen":"Sent for HPE","dischargeType":"Routine discharge","followUpKey":"PUC removal","followUpNotes":"Plan OPD review.","pastHistory":{"comorbidities":"Nil","pastSurgery":"Nil","allergy":"Nil","addiction":"No addiction"},"treatmentDuringAdmission":"IV fluids and antibiotics\nPUC inserted\nTURP under SA\nPost-op irrigation","anaesthesia":"Spinal anaesthesia","indication":"BOO due to BPE + bulbar stricture","advice":"Take plenty of oral fluids.\nAvoid heavy work / straining.\nTake medicines on time.\nCome to OPD if fever / vomiting / hematuria.","generalCondition":"Stable","postopCourse":"Uneventful","medicinePackSize":null}',
    10);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.global_diagnosis_masters WHERE code = N'bpe_luts')
INSERT INTO dbo.global_diagnosis_masters (code, name, packjson, sortorder) VALUES (
    N'bpe_luts',
    N'BPE with LUTS',
    N'{"procedureKey":"TURP","procedureExtras":{"bpeFindings":"Trilobar enlargement","resection":"Complete resection to capsule","chips":"Sent for HPE","catheter":"22F 3-way PUC","irrigation":"Started"},"chiefComplaints":"Mixed LUTS for months. Poor stream. Nocturia.","briefHistory":"Progressive LUTS. No hematuria. No retention previously.","catheterKey":"22F 3-way PUC in situ","specimen":"Sent for HPE","dischargeType":"Routine discharge","followUpKey":"PUC removal","followUpNotes":"Trial without catheter as planned.","pastHistory":{"comorbidities":"Nil","pastSurgery":"Nil","allergy":"Nil","addiction":"Nil"},"treatmentDuringAdmission":"Antibiotics\nTURP\nContinuous bladder irrigation","anaesthesia":"Spinal anaesthesia","indication":"BPE with LUTS","advice":"Take plenty of oral fluids.\nAvoid heavy work / straining.\nTake medicines on time.\nCome to OPD if fever / vomiting / hematuria.","generalCondition":"Stable","postopCourse":"Uneventful","medicinePackSize":null}',
    20);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.global_diagnosis_masters WHERE code = N'ureteric_calculus')
INSERT INTO dbo.global_diagnosis_masters (code, name, packjson, sortorder) VALUES (
    N'ureteric_calculus',
    N'Ureteric calculus',
    N'{"procedureKey":"URSL","procedureExtras":{"side":"Right","uretericLevel":"Mid ureter","stone":"8 mm","lithotripsy":"Pneumatic","djStent":"Yes"},"chiefComplaints":"Flank pain. Dysuria. Occasional hematuria.","briefHistory":"Acute colic. USG/CT confirms ureteric calculus. No fever.","catheterKey":"DJ stent in situ","specimen":"Not applicable","dischargeType":"Routine discharge","followUpKey":"DJ stent removal","followUpNotes":"Stent removal after 2–3 weeks.","pastHistory":{"comorbidities":"Nil","pastSurgery":"Nil","allergy":"Nil","addiction":"Nil"},"treatmentDuringAdmission":"Analgesia\nAntibiotics\nURSL + DJ stenting","anaesthesia":"Spinal anaesthesia","indication":"Ureteric calculus","advice":"Take plenty of oral fluids.\nAvoid heavy work / straining.\nTake medicines on time.\nCome to OPD if fever / vomiting / hematuria.","generalCondition":"Stable","postopCourse":"Uneventful","medicinePackSize":3}',
    30);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.global_diagnosis_masters WHERE code = N'renal_calculus')
INSERT INTO dbo.global_diagnosis_masters (code, name, packjson, sortorder) VALUES (
    N'renal_calculus',
    N'Renal calculus',
    N'{"procedureKey":"RIRS","procedureExtras":{"side":"Left","stoneLocation":"Lower calyx","stoneSize":"12","laser":"Holmium","fragmentation":"Dusting","djStent":"Yes","residual":"Dust"},"chiefComplaints":"Flank pain. Occasional hematuria.","briefHistory":"Recurrent renal colic. CT confirms renal calculus.","catheterKey":"DJ stent in situ","specimen":"Not applicable","dischargeType":"Routine discharge","followUpKey":"DJ stent removal","followUpNotes":"Review with X-ray KUB.","pastHistory":{"comorbidities":"Nil","pastSurgery":"Nil","allergy":"Nil","addiction":"Nil"},"treatmentDuringAdmission":"Analgesia\nRIRS with laser dusting\nDJ stent placed","anaesthesia":"General anaesthesia","indication":"Renal calculus","advice":"Take plenty of oral fluids.\nAvoid heavy work / straining.\nTake medicines on time.\nCome to OPD if fever / vomiting / hematuria.","generalCondition":"Stable","postopCourse":"Uneventful","medicinePackSize":3}',
    40);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.global_diagnosis_masters WHERE code = N'urethral_stricture')
INSERT INTO dbo.global_diagnosis_masters (code, name, packjson, sortorder) VALUES (
    N'urethral_stricture',
    N'Urethral stricture',
    N'{"procedureKey":"OIU","procedureExtras":{"strictureSite":"Proximal bulbar","length":"~2 cm","knife":"Cold knife @ 12 o''clock","catheter":"16F PUC"},"chiefComplaints":"Poor stream. Straining. Thin stream.","briefHistory":"Progressive obstructive voiding. RGU/MCU suggests bulbar stricture.","catheterKey":"16F PUC in situ","specimen":"Not applicable","dischargeType":"Routine discharge","followUpKey":"PUC removal","followUpNotes":"Uroflowmetry on follow-up.","pastHistory":{"comorbidities":"Nil","pastSurgery":"Nil","allergy":"Nil","addiction":"Nil"},"treatmentDuringAdmission":"OIU\nPUC placed","anaesthesia":"Spinal anaesthesia","indication":"Urethral stricture","advice":"Take plenty of oral fluids.\nAvoid heavy work / straining.\nTake medicines on time.\nCome to OPD if fever / vomiting / hematuria.","generalCondition":"Stable","postopCourse":"Uneventful","medicinePackSize":4}',
    50);
GO

-- Copy active globals into every existing tenant that lacks the code
INSERT INTO dbo.diagnosis_masters (
    diagnosismasterid, tenantid, globaldiagnosismasterid, code, name, packjson, sortorder, isactive)
SELECT
    NEWID(), t.tenantid, g.globaldiagnosismasterid, g.code, g.name, g.packjson, g.sortorder, g.isactive
FROM dbo.tenants t
CROSS JOIN dbo.global_diagnosis_masters g
WHERE t.isdeleted = 0
  AND g.isactive = 1
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.diagnosis_masters dm
      WHERE dm.tenantid = t.tenantid
        AND dm.code = g.code);
GO
