CREATE OR ALTER PROCEDURE dbo.sp_patient_document_delete
    @tenantid          UNIQUEIDENTIFIER,
    @patientid         UNIQUEIDENTIFIER,
    @patientdocumentid UNIQUEIDENTIFIER,
    @actorid           UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.patient_documents pd
        WHERE pd.tenantid = @tenantid
          AND pd.patientid = @patientid
          AND pd.patientdocumentid = @patientdocumentid
          AND pd.isdeleted = 0)
        THROW 50404, 'Document not found.', 1;

    UPDATE dbo.patient_documents
    SET isdeleted = 1
    WHERE tenantid = @tenantid
      AND patientid = @patientid
      AND patientdocumentid = @patientdocumentid
      AND isdeleted = 0;
END
GO
