CREATE OR ALTER PROCEDURE dbo.sp_visit_addon_catalog_save
    @tenantid      UNIQUEIDENTIFIER,
    @visitaddonid  UNIQUEIDENTIFIER = NULL,
    @name          NVARCHAR(200),
    @code          NVARCHAR(50) = NULL,
    @defaultamount DECIMAL(18, 2),
    @actorid       UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @trimmedname NVARCHAR(200) = LTRIM(RTRIM(@name));
    DECLARE @trimmedcode NVARCHAR(50) = NULLIF(LTRIM(RTRIM(@code)), '');

    IF @trimmedname IS NULL OR @trimmedname = ''
        THROW 50400, 'Addon name is required.', 1;

    IF @defaultamount IS NULL OR @defaultamount < 0
        THROW 50400, 'Addon amount cannot be negative.', 1;

    IF @visitaddonid IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM dbo.visit_addon_catalog c
           WHERE c.tenantid = @tenantid AND c.visitaddonid = @visitaddonid)
        THROW 50404, 'Visit addon not found.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.visit_addon_catalog c
        WHERE c.tenantid = @tenantid
          AND c.addonstatus = 1
          AND LOWER(LTRIM(RTRIM(c.name))) = LOWER(@trimmedname)
          AND (@visitaddonid IS NULL OR c.visitaddonid <> @visitaddonid))
        THROW 50409, 'An active visit addon with this name already exists.', 1;

    IF @trimmedcode IS NOT NULL
       AND EXISTS (
           SELECT 1 FROM dbo.visit_addon_catalog c
           WHERE c.tenantid = @tenantid
             AND c.addonstatus = 1
             AND LOWER(c.code) = LOWER(@trimmedcode)
             AND (@visitaddonid IS NULL OR c.visitaddonid <> @visitaddonid))
        THROW 50409, 'An active visit addon with this code already exists.', 1;

    IF @visitaddonid IS NULL
    BEGIN
        SET @visitaddonid = NEWID();

        INSERT INTO dbo.visit_addon_catalog (
            visitaddonid, tenantid, name, code, defaultamount,
            addonstatus, createdby, updatedby)
        VALUES (
            @visitaddonid, @tenantid, @trimmedname, @trimmedcode, @defaultamount,
            1, @actorid, @actorid);
    END
    ELSE
    BEGIN
        UPDATE dbo.visit_addon_catalog
        SET name = @trimmedname,
            code = @trimmedcode,
            defaultamount = @defaultamount,
            updatedby = @actorid,
            updatedat = SYSUTCDATETIME()
        WHERE tenantid = @tenantid
          AND visitaddonid = @visitaddonid;
    END

    SELECT @visitaddonid AS visitaddonid;
END
GO
