CREATE OR ALTER PROCEDURE dbo.sp_user_document_delete
    @tenantid        UNIQUEIDENTIFIER,
    @userid          UNIQUEIDENTIFIER,
    @userdocumentid  UNIQUEIDENTIFIER,
    @actorid         UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.user_documents d
        WHERE d.tenantid = @tenantid
          AND d.userid = @userid
          AND d.userdocumentid = @userdocumentid
          AND d.isdeleted = 0)
        THROW 50404, 'Document not found.', 1;

    UPDATE dbo.user_documents
    SET isdeleted = 1
    WHERE tenantid = @tenantid
      AND userid = @userid
      AND userdocumentid = @userdocumentid
      AND isdeleted = 0;
END
GO
