-- Make visit purpose optional

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.visits')
      AND name = N'purpose'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.visits
        ALTER COLUMN purpose NVARCHAR(300) NULL;
END
GO
