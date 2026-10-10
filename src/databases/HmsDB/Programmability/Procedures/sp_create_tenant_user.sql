CREATE OR ALTER PROCEDURE dbo.sp_create_tenant_user
    @userid       UNIQUEIDENTIFIER,
    @tenantid     UNIQUEIDENTIFIER,
    @email        NVARCHAR(100),
    @passwordhash NVARCHAR(256),
    @firstname    NVARCHAR(100),
    @lastname     NVARCHAR(100),
    @designation  NVARCHAR(500) = NULL,
    @phonenumber  NVARCHAR(20) = NULL,
    @emergencycontactnumber NVARCHAR(20) = NULL,
    @departmentid UNIQUEIDENTIFIER = NULL,
    @usertype     TINYINT,
    @userstatus   TINYINT,
    @createdby    UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF @departmentid IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM dbo.departments d
           WHERE d.departmentid = @departmentid
             AND d.tenantid = @tenantid
             AND d.departmentstatus = 1)
        THROW 50400, 'Department not found or not active.', 1;

    IF @usertype = 3 AND @departmentid IS NULL
        THROW 50400, 'Department is required for doctors.', 1;

    INSERT INTO dbo.users (
        userid, tenantid, email, passwordhash, firstname, lastname, designation,
        phonenumber, emergencycontactnumber, departmentid, usertype, userstatus, createdby)
    VALUES (
        @userid, @tenantid, @email, @passwordhash, @firstname, @lastname, @designation,
        NULLIF(LTRIM(RTRIM(@phonenumber)), ''),
        NULLIF(LTRIM(RTRIM(@emergencycontactnumber)), ''),
        @departmentid, @usertype, @userstatus, @createdby);

    SELECT @userid AS userid;
END
GO
