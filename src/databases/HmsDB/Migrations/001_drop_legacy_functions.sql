-- Drop legacy inline table-valued functions (replaced by stored procedures)
IF OBJECT_ID(N'dbo.fn_get_platform_user_by_email', N'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_get_platform_user_by_email;
GO

IF OBJECT_ID(N'dbo.fn_get_tenant_by_subdomain', N'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_get_tenant_by_subdomain;
GO

IF OBJECT_ID(N'dbo.fn_get_tenant_user_by_email', N'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_get_tenant_user_by_email;
GO

IF OBJECT_ID(N'dbo.fn_get_tenants_dashboard', N'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_get_tenants_dashboard;
GO

IF OBJECT_ID(N'dbo.fn_get_platform_dashboard', N'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_get_platform_dashboard;
GO
