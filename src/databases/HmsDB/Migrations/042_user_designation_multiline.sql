-- Allow multiline doctor qualifications in users.designation (discharge / invoice letterhead).

IF COL_LENGTH('dbo.users', 'designation') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.columns c
       INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
       WHERE c.object_id = OBJECT_ID(N'dbo.users')
         AND c.name = N'designation'
         AND t.name = N'nvarchar'
         AND c.max_length > 0
         AND c.max_length < 1000) -- NVARCHAR(500) => 1000 bytes
BEGIN
    ALTER TABLE dbo.users ALTER COLUMN designation NVARCHAR(500) NULL;
END
GO
