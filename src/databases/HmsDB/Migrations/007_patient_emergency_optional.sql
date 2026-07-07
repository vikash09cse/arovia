IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.patients')
      AND name = 'emergencynamecipher'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.patients ALTER COLUMN emergencynamecipher VARBINARY(512) NULL;
    ALTER TABLE dbo.patients ALTER COLUMN emergencyphonecipher VARBINARY(512) NULL;
END
GO
