-- Rename legacy underscore / generic id columns to descriptive names without underscores.
-- For greenfield databases created from current Schema/Tables scripts, this migration is a no-op.

IF COL_LENGTH('dbo.tenants', 'id') IS NOT NULL AND COL_LENGTH('dbo.tenants', 'tenantid') IS NULL
BEGIN
    EXEC sp_rename 'dbo.tenants.id', 'tenantid', 'COLUMN';
    EXEC sp_rename 'dbo.tenants.hospital_name', 'hospitalname', 'COLUMN';
    EXEC sp_rename 'dbo.tenants.primary_contact_name', 'primarycontactname', 'COLUMN';
    EXEC sp_rename 'dbo.tenants.primary_contact_email', 'primarycontactemail', 'COLUMN';
    EXEC sp_rename 'dbo.tenants.primary_contact_phone', 'primarycontactphone', 'COLUMN';
    EXEC sp_rename 'dbo.tenants.logo_url', 'logourl', 'COLUMN';
    EXEC sp_rename 'dbo.tenants.is_deleted', 'isdeleted', 'COLUMN';
    EXEC sp_rename 'dbo.tenants.created_at', 'createdat', 'COLUMN';
    EXEC sp_rename 'dbo.tenants.updated_at', 'updatedat', 'COLUMN';
END
GO

IF COL_LENGTH('dbo.users', 'id') IS NOT NULL AND COL_LENGTH('dbo.users', 'userid') IS NULL
BEGIN
    EXEC sp_rename 'dbo.users.id', 'userid', 'COLUMN';
    EXEC sp_rename 'dbo.users.tenant_id', 'tenantid', 'COLUMN';
    EXEC sp_rename 'dbo.users.password_hash', 'passwordhash', 'COLUMN';
    EXEC sp_rename 'dbo.users.first_name', 'firstname', 'COLUMN';
    EXEC sp_rename 'dbo.users.last_name', 'lastname', 'COLUMN';
    EXEC sp_rename 'dbo.users.user_type', 'usertype', 'COLUMN';
    EXEC sp_rename 'dbo.users.last_login_at', 'lastloginat', 'COLUMN';
    EXEC sp_rename 'dbo.users.is_deleted', 'isdeleted', 'COLUMN';
    EXEC sp_rename 'dbo.users.created_by', 'createdby', 'COLUMN';
    EXEC sp_rename 'dbo.users.created_at', 'createdat', 'COLUMN';
    EXEC sp_rename 'dbo.users.updated_by', 'updatedby', 'COLUMN';
    EXEC sp_rename 'dbo.users.updated_at', 'updatedat', 'COLUMN';
END
GO

IF COL_LENGTH('dbo.tenant_settings', 'id') IS NOT NULL AND COL_LENGTH('dbo.tenant_settings', 'tenantsettingsid') IS NULL
BEGIN
    EXEC sp_rename 'dbo.tenant_settings.id', 'tenantsettingsid', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.tenant_id', 'tenantid', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.visit_fee_amount', 'visitfeeamount', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.free_visit_window_days', 'freevisitwindowdays', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.patient_id_prefix', 'patientidprefix', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.branding_primary_color', 'brandingprimarycolor', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.branding_secondary_color', 'brandingsecondarycolor', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.receipt_header_text', 'receiptheadertext', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.receipt_footer_text', 'receiptfootertext', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.gst_tax_number', 'gsttaxnumber', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.created_at', 'createdat', 'COLUMN';
    EXEC sp_rename 'dbo.tenant_settings.updated_at', 'updatedat', 'COLUMN';
END
GO

IF COL_LENGTH('dbo.login_audit', 'id') IS NOT NULL AND COL_LENGTH('dbo.login_audit', 'loginauditid') IS NULL
BEGIN
    EXEC sp_rename 'dbo.login_audit.id', 'loginauditid', 'COLUMN';
    EXEC sp_rename 'dbo.login_audit.tenant_id', 'tenantid', 'COLUMN';
    EXEC sp_rename 'dbo.login_audit.user_identifier', 'useridentifier', 'COLUMN';
    EXEC sp_rename 'dbo.login_audit.login_type', 'logintype', 'COLUMN';
    EXEC sp_rename 'dbo.login_audit.is_success', 'issuccess', 'COLUMN';
    EXEC sp_rename 'dbo.login_audit.failure_reason', 'failurereason', 'COLUMN';
    EXEC sp_rename 'dbo.login_audit.ip_address', 'ipaddress', 'COLUMN';
    EXEC sp_rename 'dbo.login_audit.created_at', 'createdat', 'COLUMN';
END
GO

IF COL_LENGTH('dbo.refresh_tokens', 'id') IS NOT NULL AND COL_LENGTH('dbo.refresh_tokens', 'refreshtokenid') IS NULL
BEGIN
    EXEC sp_rename 'dbo.refresh_tokens.id', 'refreshtokenid', 'COLUMN';
    EXEC sp_rename 'dbo.refresh_tokens.user_id', 'userid', 'COLUMN';
    EXEC sp_rename 'dbo.refresh_tokens.tenant_id', 'tenantid', 'COLUMN';
    EXEC sp_rename 'dbo.refresh_tokens.token_hash', 'tokenhash', 'COLUMN';
    EXEC sp_rename 'dbo.refresh_tokens.expires_at', 'expiresat', 'COLUMN';
    EXEC sp_rename 'dbo.refresh_tokens.is_revoked', 'isrevoked', 'COLUMN';
    EXEC sp_rename 'dbo.refresh_tokens.created_at', 'createdat', 'COLUMN';
END
GO
