CREATE OR ALTER PROCEDURE dbo.sp_patient_document_get_by_id
    @tenantid           UNIQUEIDENTIFIER,
    @patientid          UNIQUEIDENTIFIER,
    @patientdocumentid  UNIQUEIDENTIFIER
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
      AND pd.patientdocumentid = @patientdocumentid
      AND pd.isdeleted = 0;
END
GO
