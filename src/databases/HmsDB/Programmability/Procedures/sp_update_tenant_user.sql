CREATE OR ALTER PROCEDURE dbo.sp_update_tenant_user
    @tenantid             UNIQUEIDENTIFIER,
    @userid               UNIQUEIDENTIFIER,
    @firstname            NVARCHAR(100),
    @lastname             NVARCHAR(100),
    @designation          NVARCHAR(100) = NULL,
    @updatedesignation    BIT = 0,
    @departmentid         UNIQUEIDENTIFIER = NULL,
    @updatedepartment     BIT = 0,
    @usertype             TINYINT,
    @updatedby            UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF @updatedepartment = 1 AND @usertype = 3
    BEGIN
        IF @departmentid IS NULL
            THROW 50400, 'Department is required for doctors.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.departments d
            WHERE d.departmentid = @departmentid
              AND d.tenantid = @tenantid
              AND d.departmentstatus = 1)
            THROW 50400, 'Department not found or not active.', 1;
    END

    UPDATE dbo.users
    SET firstname = @firstname,
        lastname = @lastname,
        designation = CASE WHEN @updatedesignation = 1 THEN @designation ELSE designation END,
        departmentid = CASE WHEN @updatedepartment = 1 THEN @departmentid ELSE departmentid END,
        usertype = @usertype,
        updatedby = @updatedby,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND userid = @userid
      AND isdeleted = 0;
END
GO
