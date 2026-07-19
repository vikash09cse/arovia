CREATE OR ALTER PROCEDURE dbo.sp_common_file_get_by_id
    @tenantid     UNIQUEIDENTIFIER,
    @commonfileid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cf.commonfileid,
        cf.displayname,
        cf.storedfilename,
        cf.createdat,
        cf.createdby
    FROM dbo.common_files cf
    WHERE cf.tenantid = @tenantid
      AND cf.commonfileid = @commonfileid
      AND cf.isdeleted = 0;
END
GO
