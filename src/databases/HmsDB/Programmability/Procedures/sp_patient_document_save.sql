CREATE OR ALTER PROCEDURE dbo.sp_patient_document_save
    @tenantid        UNIQUEIDENTIFIER,
    @patientid       UNIQUEIDENTIFIER,
    @displayname     NVARCHAR(260),
    @storedfilename  NVARCHAR(260),
    @actorid         UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @trimmeddisplay NVARCHAR(260) = LTRIM(RTRIM(@displayname));
    DECLARE @trimmedstored NVARCHAR(260) = LTRIM(RTRIM(@storedfilename));
    DECLARE @patientdocumentid UNIQUEIDENTIFIER = NEWID();

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.patients p
        WHERE p.tenantid = @tenantid
          AND p.patientid = @patientid
          AND p.isdeleted = 0)
        THROW 50404, 'Patient not found.', 1;

    IF @trimmeddisplay IS NULL OR @trimmeddisplay = ''
        THROW 50400, 'File name is required.', 1;

    IF @trimmedstored IS NULL OR @trimmedstored = ''
        THROW 50400, 'Stored file name is required.', 1;

    INSERT INTO dbo.patient_documents (
        patientdocumentid, tenantid, patientid, displayname, storedfilename, isdeleted, createdby)
    VALUES (
        @patientdocumentid, @tenantid, @patientid, @trimmeddisplay, @trimmedstored, 0, @actorid);

    SELECT
        pd.patientdocumentid,
        pd.patientid,
        pd.displayname,
        pd.storedfilename,
        pd.createdat,
        pd.createdby
    FROM dbo.patient_documents pd
    WHERE pd.patientdocumentid = @patientdocumentid;
END
GO
