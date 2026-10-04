CREATE OR ALTER PROCEDURE dbo.sp_department_save
    @tenantid      UNIQUEIDENTIFIER,
    @departmentid  UNIQUEIDENTIFIER = NULL,
    @name          NVARCHAR(150),
    @actorid       UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @trimmed NVARCHAR(150) = LTRIM(RTRIM(@name));

    IF @trimmed IS NULL OR @trimmed = N''
        THROW 50400, 'Department name is required.', 1;

    IF LEN(@trimmed) > 150
        THROW 50400, 'Department name cannot exceed 150 characters.', 1;

    IF @departmentid IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM dbo.departments d
           WHERE d.tenantid = @tenantid AND d.departmentid = @departmentid)
        THROW 50404, 'Department not found.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.departments d
        WHERE d.tenantid = @tenantid
          AND LOWER(d.name) = LOWER(@trimmed)
          AND (@departmentid IS NULL OR d.departmentid <> @departmentid))
        THROW 50409, 'A department with this name already exists.', 1;

    IF @departmentid IS NULL
    BEGIN
        SET @departmentid = NEWID();
        INSERT INTO dbo.departments (
            departmentid, tenantid, name, departmentstatus, createdby, updatedby)
        VALUES (
            @departmentid, @tenantid, @trimmed, 1, @actorid, @actorid);
    END
    ELSE
    BEGIN
        UPDATE dbo.departments
        SET name = @trimmed,
            updatedby = @actorid,
            updatedat = SYSUTCDATETIME()
        WHERE tenantid = @tenantid
          AND departmentid = @departmentid;
    END

    SELECT @departmentid AS departmentid;
END
GO
