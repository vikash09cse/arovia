CREATE OR ALTER PROCEDURE dbo.sp_patient_document_get_list
    @tenantid  UNIQUEIDENTIFIER,
    @patientid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pd.patientdocumentid,
        pd.patientid,
        pd.displayname,
        pd.storedfilename,
        pd.createdat,
        pd.createdby
    FROM dbo.patient_documents pd
    WHERE pd.tenantid = @tenantid
      AND pd.patientid = @patientid
      AND pd.isdeleted = 0
    ORDER BY pd.createdat DESC, pd.displayname;
END
GO
