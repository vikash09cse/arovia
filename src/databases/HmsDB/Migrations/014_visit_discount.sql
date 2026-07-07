-- Visit discount columns

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.visits') AND name = 'discountamount')
BEGIN
    ALTER TABLE dbo.visits
        ADD discountamount DECIMAL(18, 2) NULL,
            discountreason NVARCHAR(500) NULL;
END
GO
