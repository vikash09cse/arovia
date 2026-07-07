CREATE OR ALTER PROCEDURE dbo.sp_visit_addon_catalog_set_status
    @tenantid     UNIQUEIDENTIFIER,
    @visitaddonid UNIQUEIDENTIFIER,
    @addonstatus  TINYINT,
    @actorid      UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF @addonstatus NOT IN (1, 2)
        THROW 50400, 'Invalid addon status.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.visit_addon_catalog c
        WHERE c.tenantid = @tenantid AND c.visitaddonid = @visitaddonid)
        THROW 50404, 'Visit addon not found.', 1;

    IF @addonstatus = 1
    BEGIN
        DECLARE @name NVARCHAR(200);
        DECLARE @code NVARCHAR(50);

        SELECT @name = c.name, @code = c.code
        FROM dbo.visit_addon_catalog c
        WHERE c.tenantid = @tenantid AND c.visitaddonid = @visitaddonid;

        IF EXISTS (
            SELECT 1 FROM dbo.visit_addon_catalog c
            WHERE c.tenantid = @tenantid
              AND c.addonstatus = 1
              AND c.visitaddonid <> @visitaddonid
              AND LOWER(LTRIM(RTRIM(c.name))) = LOWER(LTRIM(RTRIM(@name))))
            THROW 50409, 'An active visit addon with this name already exists.', 1;

        IF @code IS NOT NULL
           AND EXISTS (
               SELECT 1 FROM dbo.visit_addon_catalog c
               WHERE c.tenantid = @tenantid
                 AND c.addonstatus = 1
                 AND c.visitaddonid <> @visitaddonid
                 AND LOWER(c.code) = LOWER(@code))
            THROW 50409, 'An active visit addon with this code already exists.', 1;
    END

    UPDATE dbo.visit_addon_catalog
    SET addonstatus = @addonstatus,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND visitaddonid = @visitaddonid;
END
GO
