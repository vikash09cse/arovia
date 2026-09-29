CREATE OR ALTER PROCEDURE dbo.sp_user_document_get_list
    @tenantid UNIQUEIDENTIFIER,
    @userid   UNIQUEIDENTIFIER
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
      AND d.isdeleted = 0
    ORDER BY d.createdat DESC, d.displayname;
END
GO
