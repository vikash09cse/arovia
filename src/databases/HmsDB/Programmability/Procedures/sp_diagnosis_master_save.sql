CREATE OR ALTER PROCEDURE dbo.sp_diagnosis_master_save
    @tenantid           UNIQUEIDENTIFIER,
    @diagnosismasterid  UNIQUEIDENTIFIER = NULL,
    @code               NVARCHAR(50) = NULL,
    @name               NVARCHAR(300),
    @packjson           NVARCHAR(MAX) = NULL,
    @sortorder          INT = 0,
    @isactive           BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @trimmed NVARCHAR(300) = LTRIM(RTRIM(@name));
    IF @trimmed IS NULL OR @trimmed = N''
        THROW 50400, 'Diagnosis name is required.', 1;

    IF LEN(@trimmed) > 300
        THROW 50400, 'Diagnosis name cannot exceed 300 characters.', 1;

    IF @packjson IS NOT NULL AND ISJSON(@packjson) = 0
        THROW 50400, 'Pack JSON is invalid.', 1;

    IF @diagnosismasterid IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM dbo.diagnosis_masters dm
           WHERE dm.tenantid = @tenantid
             AND dm.diagnosismasterid = @diagnosismasterid)
        THROW 50404, 'Diagnosis not found.', 1;

    DECLARE @codeTrimmed NVARCHAR(50) = NULLIF(LTRIM(RTRIM(@code)), N'');

    IF @diagnosismasterid IS NULL
    BEGIN
        IF @codeTrimmed IS NULL OR @codeTrimmed = N''
        BEGIN
            -- Slug from name: lowercase alphanumeric + underscore
            DECLARE @slug NVARCHAR(50) = LOWER(
                REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    @trimmed, N' ', N'_'), N'+', N''), N'/', N'_'), N'-', N'_'), N'.', N''));
            -- Keep only safe chars roughly
            SET @slug = LEFT(@slug, 40);
            IF @slug IS NULL OR @slug = N''
                SET @slug = N'dx';

            SET @codeTrimmed = @slug;
            DECLARE @suffix INT = 1;
            WHILE EXISTS (
                SELECT 1 FROM dbo.diagnosis_masters dm
                WHERE dm.tenantid = @tenantid AND dm.code = @codeTrimmed)
            BEGIN
                SET @codeTrimmed = LEFT(@slug, 40) + N'_' + CAST(@suffix AS NVARCHAR(10));
                SET @suffix += 1;
                IF @suffix > 999
                    THROW 50409, 'Unable to generate a unique diagnosis code.', 1;
            END
        END
        ELSE IF EXISTS (
            SELECT 1 FROM dbo.diagnosis_masters dm
            WHERE dm.tenantid = @tenantid AND LOWER(dm.code) = LOWER(@codeTrimmed))
            THROW 50409, 'A diagnosis with this code already exists.', 1;

        SET @diagnosismasterid = NEWID();
        INSERT INTO dbo.diagnosis_masters (
            diagnosismasterid, tenantid, globaldiagnosismasterid, code, name,
            packjson, sortorder, isactive)
        VALUES (
            @diagnosismasterid, @tenantid, NULL, @codeTrimmed, @trimmed,
            @packjson, @sortorder, @isactive);
    END
    ELSE
    BEGIN
        UPDATE dbo.diagnosis_masters
        SET name = @trimmed,
            packjson = @packjson,
            sortorder = @sortorder,
            isactive = @isactive,
            updatedat = SYSUTCDATETIME()
        WHERE tenantid = @tenantid
          AND diagnosismasterid = @diagnosismasterid;
    END

    SELECT @diagnosismasterid AS diagnosismasterid;
END
GO
