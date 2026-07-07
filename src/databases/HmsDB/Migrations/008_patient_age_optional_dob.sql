IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.patients') AND name = 'age'
)
BEGIN
    ALTER TABLE dbo.patients ADD age INT NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.patients')
      AND name = 'dateofbirth'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.patients ALTER COLUMN dateofbirth DATE NULL;
END
GO
