CREATE OR ALTER PROCEDURE dbo.sp_common_file_delete
    @tenantid     UNIQUEIDENTIFIER,
    @commonfileid UNIQUEIDENTIFIER,
    @actorid      UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.common_files cf
        WHERE cf.tenantid = @tenantid
          AND cf.commonfileid = @commonfileid
          AND cf.isdeleted = 0)
        THROW 50404, 'File not found.', 1;

    UPDATE dbo.common_files
    SET isdeleted = 1
    WHERE tenantid = @tenantid
      AND commonfileid = @commonfileid
      AND isdeleted = 0;
END
GO
