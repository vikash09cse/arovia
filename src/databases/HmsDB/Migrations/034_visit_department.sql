-- Optional department on visits (required for new saves via SP; existing rows stay NULL).
IF OBJECT_ID(N'dbo.visits', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.departments', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.visits', N'departmentid') IS NULL
BEGIN
    ALTER TABLE dbo.visits ADD departmentid UNIQUEIDENTIFIER NULL;

    ALTER TABLE dbo.visits
        ADD CONSTRAINT FK_visits_department
        FOREIGN KEY (departmentid) REFERENCES dbo.departments (departmentid);
END
GO

IF OBJECT_ID(N'dbo.visits', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.visits', N'departmentid') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_visits_tenant_department'
          AND object_id = OBJECT_ID(N'dbo.visits'))
BEGIN
    CREATE INDEX IX_visits_tenant_department
        ON dbo.visits (tenantid, departmentid);
END
GO
