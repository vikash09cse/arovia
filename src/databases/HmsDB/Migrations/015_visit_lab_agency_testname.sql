-- Add test name to visit lab agency assignments

IF COL_LENGTH('dbo.visit_lab_agencies', 'testname') IS NULL
BEGIN
    ALTER TABLE dbo.visit_lab_agencies
        ADD testname NVARCHAR(250) NULL;
END
GO
