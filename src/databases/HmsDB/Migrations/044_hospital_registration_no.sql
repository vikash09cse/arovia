-- Hospital registration number on tenant profile (Tenant Settings).

IF COL_LENGTH('dbo.tenants', 'hospitalregistrationno') IS NULL
BEGIN
    ALTER TABLE dbo.tenants ADD hospitalregistrationno NVARCHAR(100) NULL;
END
GO
