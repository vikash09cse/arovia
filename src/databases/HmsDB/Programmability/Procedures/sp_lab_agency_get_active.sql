CREATE OR ALTER PROCEDURE dbo.sp_lab_agency_get_active
    @tenantid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        la.labagencyid,
        la.name,
        la.contactperson,
        la.phone
    FROM dbo.lab_agencies la
    WHERE la.tenantid = @tenantid
      AND la.agencystatus = 1
    ORDER BY la.name;
END
GO
