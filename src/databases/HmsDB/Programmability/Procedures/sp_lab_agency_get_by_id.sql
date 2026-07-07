CREATE OR ALTER PROCEDURE dbo.sp_lab_agency_get_by_id
    @tenantid    UNIQUEIDENTIFIER,
    @labagencyid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        la.labagencyid,
        la.name,
        la.contactperson,
        la.phone,
        la.email,
        la.address,
        la.notes,
        la.agencystatus,
        la.createdat,
        la.updatedat
    FROM dbo.lab_agencies la
    WHERE la.tenantid = @tenantid
      AND la.labagencyid = @labagencyid;
END
GO
