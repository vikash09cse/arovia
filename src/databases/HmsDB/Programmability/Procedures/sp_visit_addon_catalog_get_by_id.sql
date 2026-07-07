CREATE OR ALTER PROCEDURE dbo.sp_visit_addon_catalog_get_by_id
    @tenantid     UNIQUEIDENTIFIER,
    @visitaddonid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.visitaddonid,
        c.name,
        c.code,
        c.defaultamount,
        c.addonstatus,
        c.createdat,
        c.updatedat
    FROM dbo.visit_addon_catalog c
    WHERE c.tenantid = @tenantid
      AND c.visitaddonid = @visitaddonid;
END
GO
