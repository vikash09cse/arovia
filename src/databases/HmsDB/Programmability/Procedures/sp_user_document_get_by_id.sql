CREATE OR ALTER PROCEDURE dbo.sp_user_document_get_by_id
    @tenantid        UNIQUEIDENTIFIER,
    @userid          UNIQUEIDENTIFIER,
    @userdocumentid  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.userdocumentid,
        d.userid,
        d.documenttype,
        d.displayname,
        d.storedfilename,
        d.createdat,
        d.createdby
    FROM dbo.user_documents d
    WHERE d.tenantid = @tenantid
      AND d.userid = @userid
      AND d.userdocumentid = @userdocumentid
      AND d.isdeleted = 0;
END
GO
