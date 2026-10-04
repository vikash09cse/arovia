-- Receipt numbers on IPD payments (shared RCP- sequence with visit payments).
IF OBJECT_ID(N'dbo.admission_payments', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.admission_payments', N'receiptnumber') IS NULL
BEGIN
    ALTER TABLE dbo.admission_payments ADD receiptnumber NVARCHAR(20) NULL;
END
GO

IF OBJECT_ID(N'dbo.admission_payments', N'U') IS NOT NULL
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
