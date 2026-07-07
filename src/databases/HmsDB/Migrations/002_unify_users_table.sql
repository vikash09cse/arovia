-- Unify platform and tenant users into dbo.users (single database, shared user table)

IF OBJECT_ID(N'dbo.sp_get_platform_user_by_email', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_get_platform_user_by_email;
GO

IF OBJECT_ID(N'dbo.sp_get_tenant_user_by_email', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_get_tenant_user_by_email;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'backoffice_users')
BEGIN
    INSERT INTO dbo.users (id, tenant_id, email, password_hash, first_name, last_name, user_type, status, created_at, updated_at, is_deleted)
    SELECT b.id, NULL, b.email, b.password_hash, b.first_name, b.last_name, 0, b.status, b.created_at, b.updated_at, b.is_deleted
    FROM dbo.backoffice_users b
    WHERE NOT EXISTS (SELECT 1 FROM dbo.users u WHERE u.id = b.id);

    DROP TABLE dbo.backoffice_users;
END
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tenant_users')
BEGIN
    INSERT INTO dbo.users (id, tenant_id, email, password_hash, first_name, last_name, user_type, status, last_login_at, created_by, created_at, updated_by, updated_at, is_deleted)
    SELECT t.id, t.tenant_id, t.email, t.password_hash, t.first_name, t.last_name, t.role, t.status, t.last_login_at, t.created_by, t.created_at, t.updated_by, t.updated_at, t.is_deleted
    FROM dbo.tenant_users t
    WHERE NOT EXISTS (SELECT 1 FROM dbo.users u WHERE u.id = t.id);

    DROP TABLE dbo.tenant_users;
END
GO
