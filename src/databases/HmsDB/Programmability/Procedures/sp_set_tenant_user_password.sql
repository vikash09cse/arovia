CREATE OR ALTER PROCEDURE dbo.sp_set_tenant_user_password
    @tenantid      UNIQUEIDENTIFIER,
    @userid        UNIQUEIDENTIFIER,
    @passwordhash  NVARCHAR(256),
    @updatedby     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @passwordhash IS NULL OR LTRIM(RTRIM(@passwordhash)) = N''
        THROW 50400, 'Password hash is required.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.users u
        WHERE u.tenantid = @tenantid
          AND u.userid = @userid
          AND u.isdeleted = 0)
        THROW 50404, 'User not found.', 1;

    UPDATE dbo.users
    SET passwordhash = @passwordhash,
        updatedby = @updatedby,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND userid = @userid
      AND isdeleted = 0;
END
GO
