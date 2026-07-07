-- Split tenant primary contact name into first and last name columns (idempotent)

IF COL_LENGTH('dbo.tenants', 'primarycontactname') IS NOT NULL
   AND COL_LENGTH('dbo.tenants', 'primarycontactfirstname') IS NULL
BEGIN
    ALTER TABLE dbo.tenants ADD primarycontactfirstname NVARCHAR(100) NULL;
    ALTER TABLE dbo.tenants ADD primarycontactlastname NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.tenants', 'primarycontactname') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE dbo.tenants
        SET primarycontactfirstname = CASE
                WHEN CHARINDEX('' '', primarycontactname) > 0
                    THEN LEFT(primarycontactname, CHARINDEX('' '', primarycontactname) - 1)
                ELSE primarycontactname
            END,
            primarycontactlastname = CASE
                WHEN CHARINDEX('' '', primarycontactname) > 0
                    THEN LTRIM(SUBSTRING(primarycontactname, CHARINDEX('' '', primarycontactname) + 1, 100))
                ELSE ''''
            END
        WHERE primarycontactfirstname IS NULL;
    ');
END
GO

IF COL_LENGTH('dbo.tenants', 'primarycontactfirstname') IS NOT NULL
BEGIN
    UPDATE dbo.tenants SET primarycontactfirstname = '' WHERE primarycontactfirstname IS NULL;
    UPDATE dbo.tenants SET primarycontactlastname = '' WHERE primarycontactlastname IS NULL;

    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.tenants')
          AND name = N'primarycontactfirstname'
          AND is_nullable = 1)
    BEGIN
        ALTER TABLE dbo.tenants ALTER COLUMN primarycontactfirstname NVARCHAR(100) NOT NULL;
    END

    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.tenants')
          AND name = N'primarycontactlastname'
          AND is_nullable = 1)
    BEGIN
        ALTER TABLE dbo.tenants ALTER COLUMN primarycontactlastname NVARCHAR(100) NOT NULL;
    END
END
GO

IF COL_LENGTH('dbo.tenants', 'primarycontactname') IS NOT NULL
BEGIN
    ALTER TABLE dbo.tenants DROP COLUMN primarycontactname;
END
GO
