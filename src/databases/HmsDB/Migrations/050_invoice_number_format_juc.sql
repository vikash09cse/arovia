-- Invoice number format: {PREFIX}/{PROCEDURE}/{yyyyMMdd}-{NN} e.g. JUC/URSL/20260911-08

IF COL_LENGTH('dbo.admissions', 'invoicenumber') IS NOT NULL
BEGIN
    ALTER TABLE dbo.admissions ALTER COLUMN invoicenumber NVARCHAR(50) NULL;
END
GO

-- Prefer 3-letter hospital prefix for Janak Uro Care style codes (JUC).
UPDATE ts
SET patientidprefix = N'JUC'
FROM dbo.tenant_settings ts
INNER JOIN dbo.tenants t ON t.tenantid = ts.tenantid
WHERE t.isdeleted = 0
  AND (
        UPPER(REPLACE(REPLACE(ISNULL(ts.patientidprefix, N''), N'-', N''), N' ', N'')) IN (N'JU', N'JUC')
     OR t.hospitalname LIKE N'%Janak%Uro%'
  );
GO
