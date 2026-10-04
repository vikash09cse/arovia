-- Department master + link to admissions and doctors.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'departments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.departments (
        departmentid       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_departments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        tenantid           UNIQUEIDENTIFIER NOT NULL,
        name               NVARCHAR(150)    NOT NULL,
        departmentstatus   TINYINT          NOT NULL CONSTRAINT DF_departments_status DEFAULT (1),
        createdby          UNIQUEIDENTIFIER NOT NULL,
        createdat          DATETIME2        NOT NULL CONSTRAINT DF_departments_createdat DEFAULT (SYSUTCDATETIME()),
        updatedby          UNIQUEIDENTIFIER NOT NULL,
        updatedat          DATETIME2        NOT NULL CONSTRAINT DF_departments_updatedat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_departments_tenant FOREIGN KEY (tenantid) REFERENCES dbo.tenants (tenantid),
        CONSTRAINT FK_departments_createdby FOREIGN KEY (createdby) REFERENCES dbo.users (userid),
        CONSTRAINT FK_departments_updatedby FOREIGN KEY (updatedby) REFERENCES dbo.users (userid),
        CONSTRAINT CK_departments_status CHECK (departmentstatus IN (1, 2))
    );

    CREATE UNIQUE INDEX UQ_departments_tenant_name
        ON dbo.departments (tenantid, name);

    CREATE INDEX IX_departments_tenant_status
        ON dbo.departments (tenantid, departmentstatus, name);
END
GO

IF COL_LENGTH(N'dbo.users', N'departmentid') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD departmentid UNIQUEIDENTIFIER NULL;
END
GO

IF COL_LENGTH(N'dbo.users', N'departmentid') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_users_department')
BEGIN
    ALTER TABLE dbo.users
        ADD CONSTRAINT FK_users_department
        FOREIGN KEY (departmentid) REFERENCES dbo.departments (departmentid);
END
GO

-- Make bed optional
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.admissions')
      AND name = N'bed'
      AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.admissions ALTER COLUMN bed NVARCHAR(50) NULL;
END
GO

-- Add departmentid to admissions (nullable first, backfill, then NOT NULL)
IF COL_LENGTH(N'dbo.admissions', N'departmentid') IS NULL
BEGIN
    ALTER TABLE dbo.admissions ADD departmentid UNIQUEIDENTIFIER NULL;
END
GO

-- Backfill: ensure each tenant with admissions has a General department, then set FK
DECLARE @tenantid UNIQUEIDENTIFIER;
DECLARE @actorid UNIQUEIDENTIFIER;
DECLARE @deptid UNIQUEIDENTIFIER;

DECLARE tenant_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT a.tenantid
    FROM dbo.admissions a
    WHERE a.departmentid IS NULL;

OPEN tenant_cursor;
FETCH NEXT FROM tenant_cursor INTO @tenantid;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT TOP 1 @actorid = u.userid
    FROM dbo.users u
    WHERE u.tenantid = @tenantid
      AND u.isdeleted = 0
      AND u.usertype = 1
    ORDER BY u.createdat;

    IF @actorid IS NULL
        SELECT TOP 1 @actorid = u.userid
        FROM dbo.users u
        WHERE u.tenantid = @tenantid
          AND u.isdeleted = 0
        ORDER BY u.createdat;

    IF @actorid IS NULL
        SET @actorid = '00000000-0000-0000-0000-000000000001';

    SELECT TOP 1 @deptid = d.departmentid
    FROM dbo.departments d
    WHERE d.tenantid = @tenantid
      AND d.name = N'General';

    IF @deptid IS NULL
    BEGIN
        SET @deptid = NEWID();
        INSERT INTO dbo.departments (
            departmentid, tenantid, name, departmentstatus, createdby, updatedby)
        VALUES (
            @deptid, @tenantid, N'General', 1, @actorid, @actorid);
    END

    UPDATE dbo.admissions
    SET departmentid = @deptid
    WHERE tenantid = @tenantid
      AND departmentid IS NULL;

    SET @deptid = NULL;
    SET @actorid = NULL;
    FETCH NEXT FROM tenant_cursor INTO @tenantid;
END

CLOSE tenant_cursor;
DEALLOCATE tenant_cursor;
GO

IF COL_LENGTH(N'dbo.admissions', N'departmentid') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.admissions')
         AND name = N'departmentid'
         AND is_nullable = 1)
   AND NOT EXISTS (SELECT 1 FROM dbo.admissions WHERE departmentid IS NULL)
BEGIN
    ALTER TABLE dbo.admissions ALTER COLUMN departmentid UNIQUEIDENTIFIER NOT NULL;
END
GO

IF COL_LENGTH(N'dbo.admissions', N'departmentid') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_admissions_department')
BEGIN
    ALTER TABLE dbo.admissions
        ADD CONSTRAINT FK_admissions_department
        FOREIGN KEY (departmentid) REFERENCES dbo.departments (departmentid);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_admissions_tenant_department'
      AND object_id = OBJECT_ID(N'dbo.admissions'))
BEGIN
    CREATE INDEX IX_admissions_tenant_department
        ON dbo.admissions (tenantid, departmentid);
END
GO
