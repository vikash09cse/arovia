CREATE OR ALTER PROCEDURE dbo.sp_update_my_profile
    @tenantid     UNIQUEIDENTIFIER,
    @userid       UNIQUEIDENTIFIER,
    @firstname    NVARCHAR(100),
    @lastname     NVARCHAR(100),
    @designation  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @firstname IS NULL OR LTRIM(RTRIM(@firstname)) = N''
        THROW 50400, 'First name is required.', 1;

    IF @lastname IS NULL OR LTRIM(RTRIM(@lastname)) = N''
        THROW 50400, 'Last name is required.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.users u
        WHERE u.tenantid = @tenantid
          AND u.userid = @userid
          AND u.isdeleted = 0)
        THROW 50404, 'User not found.', 1;

    UPDATE dbo.users
    SET firstname = LTRIM(RTRIM(@firstname)),
        lastname = LTRIM(RTRIM(@lastname)),
        designation = NULLIF(LTRIM(RTRIM(@designation)), N''),
        updatedby = @userid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND userid = @userid
      AND isdeleted = 0;

    SELECT
        u.userid,
        u.email,
        u.firstname,
        u.lastname,
        u.designation
    FROM dbo.users u
    WHERE u.tenantid = @tenantid
      AND u.userid = @userid
      AND u.isdeleted = 0;
END
GO
