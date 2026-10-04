-- Allow the same lab agency to be assigned multiple times on one visit.
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_visit_lab_agencies_tenant_visit_agency'
      AND object_id = OBJECT_ID(N'dbo.visit_lab_agencies'))
BEGIN
    DROP INDEX UQ_visit_lab_agencies_tenant_visit_agency ON dbo.visit_lab_agencies;
END
GO
