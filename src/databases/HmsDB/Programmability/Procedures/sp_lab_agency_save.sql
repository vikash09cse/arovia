CREATE OR ALTER PROCEDURE dbo.sp_lab_agency_save
    @tenantid      UNIQUEIDENTIFIER,
    @labagencyid   UNIQUEIDENTIFIER = NULL,
    @name          NVARCHAR(200),
    @contactperson NVARCHAR(100) = NULL,
    @phone         NVARCHAR(20) = NULL,
    @email         NVARCHAR(256) = NULL,
    @address       NVARCHAR(500) = NULL,
    @notes         NVARCHAR(500) = NULL,
    @actorid       UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @trimmedname NVARCHAR(200) = LTRIM(RTRIM(@name));

    IF @trimmedname IS NULL OR @trimmedname = ''
        THROW 50400, 'Agency name is required.', 1;

    IF @labagencyid IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM dbo.lab_agencies la
           WHERE la.tenantid = @tenantid AND la.labagencyid = @labagencyid)
        THROW 50404, 'Lab agency not found.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.lab_agencies la
        WHERE la.tenantid = @tenantid
          AND la.agencystatus = 1
          AND LOWER(LTRIM(RTRIM(la.name))) = LOWER(@trimmedname)
          AND (@labagencyid IS NULL OR la.labagencyid <> @labagencyid))
        THROW 50409, 'An active lab agency with this name already exists.', 1;

    IF @labagencyid IS NULL
    BEGIN
        SET @labagencyid = NEWID();

        INSERT INTO dbo.lab_agencies (
            labagencyid, tenantid, name, contactperson, phone, email, address, notes,
            agencystatus, createdby, updatedby)
        VALUES (
            @labagencyid, @tenantid, @trimmedname,
            NULLIF(LTRIM(RTRIM(@contactperson)), ''),
            NULLIF(LTRIM(RTRIM(@phone)), ''),
            NULLIF(LTRIM(RTRIM(@email)), ''),
            NULLIF(LTRIM(RTRIM(@address)), ''),
            NULLIF(LTRIM(RTRIM(@notes)), ''),
            1, @actorid, @actorid);
    END
    ELSE
    BEGIN
        UPDATE dbo.lab_agencies
        SET name = @trimmedname,
            contactperson = NULLIF(LTRIM(RTRIM(@contactperson)), ''),
            phone = NULLIF(LTRIM(RTRIM(@phone)), ''),
            email = NULLIF(LTRIM(RTRIM(@email)), ''),
            address = NULLIF(LTRIM(RTRIM(@address)), ''),
            notes = NULLIF(LTRIM(RTRIM(@notes)), ''),
            updatedby = @actorid,
            updatedat = SYSUTCDATETIME()
        WHERE tenantid = @tenantid
          AND labagencyid = @labagencyid;
    END

    SELECT @labagencyid AS labagencyid;
END
GO
