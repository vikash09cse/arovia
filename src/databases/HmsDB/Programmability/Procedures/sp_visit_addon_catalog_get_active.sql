CREATE OR ALTER PROCEDURE dbo.sp_visit_addon_catalog_get_active
    @tenantid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.visitaddonid,
        c.name,
        c.code,
        c.defaultamount
    FROM dbo.visit_addon_catalog c
    WHERE c.tenantid = @tenantid
      AND c.addonstatus = 1
    ORDER BY c.name;
END
GO
