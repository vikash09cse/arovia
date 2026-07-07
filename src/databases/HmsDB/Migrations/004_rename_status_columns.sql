-- Rename generic status columns to descriptive names (no underscores).

IF COL_LENGTH('dbo.users', 'status') IS NOT NULL AND COL_LENGTH('dbo.users', 'userstatus') IS NULL
    EXEC sp_rename 'dbo.users.status', 'userstatus', 'COLUMN';
GO

IF COL_LENGTH('dbo.tenants', 'status') IS NOT NULL AND COL_LENGTH('dbo.tenants', 'tenantstatus') IS NULL
    EXEC sp_rename 'dbo.tenants.status', 'tenantstatus', 'COLUMN';
GO
