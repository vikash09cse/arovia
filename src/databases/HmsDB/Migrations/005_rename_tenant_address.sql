IF COL_LENGTH('dbo.tenants', 'address') IS NOT NULL AND COL_LENGTH('dbo.tenants', 'tenantaddress') IS NULL
    EXEC sp_rename 'dbo.tenants.address', 'tenantaddress', 'COLUMN';
GO
