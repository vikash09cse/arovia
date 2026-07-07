CREATE OR ALTER PROCEDURE dbo.sp_lab_agency_set_status
    @tenantid     UNIQUEIDENTIFIER,
    @labagencyid  UNIQUEIDENTIFIER,
    @agencystatus TINYINT,
    @actorid      UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF @agencystatus NOT IN (1, 2)
        THROW 50400, 'Invalid agency status.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.lab_agencies la
        WHERE la.tenantid = @tenantid AND la.labagencyid = @labagencyid)
        THROW 50404, 'Lab agency not found.', 1;

    IF @agencystatus = 1
    BEGIN
        DECLARE @name NVARCHAR(200);
        SELECT @name = la.name
        FROM dbo.lab_agencies la
        WHERE la.tenantid = @tenantid AND la.labagencyid = @labagencyid;

        IF EXISTS (
            SELECT 1 FROM dbo.lab_agencies la
            WHERE la.tenantid = @tenantid
              AND la.agencystatus = 1
              AND la.labagencyid <> @labagencyid
              AND LOWER(LTRIM(RTRIM(la.name))) = LOWER(LTRIM(RTRIM(@name))))
            THROW 50409, 'An active lab agency with this name already exists.', 1;
    END

    UPDATE dbo.lab_agencies
    SET agencystatus = @agencystatus,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND labagencyid = @labagencyid;
END
GO
