CREATE OR ALTER PROCEDURE dbo.sp_patient_phone_exists
    @tenantid           UNIQUEIDENTIFIER,
    @phoneblindindex    BINARY(32),
    @excludepatientid   UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        p.patientid,
        p.patientcode,
        p.firstname,
        p.lastname
    FROM dbo.patients p
    WHERE p.tenantid = @tenantid
      AND p.isdeleted = 0
      AND p.phoneblindindex = @phoneblindindex
      AND (@excludepatientid IS NULL OR p.patientid <> @excludepatientid);
END
GO
