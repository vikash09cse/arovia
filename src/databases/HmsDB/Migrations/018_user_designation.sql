-- Optional designation on users (shown in portal header when set)

IF COL_LENGTH('dbo.users', 'designation') IS NULL
BEGIN
    ALTER TABLE dbo.users
        ADD designation NVARCHAR(100) NULL;
END
GO
