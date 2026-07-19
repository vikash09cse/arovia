CREATE OR ALTER PROCEDURE dbo.sp_common_file_get_list
    @tenantid UNIQUEIDENTIFIER
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
      AND cf.isdeleted = 0
    ORDER BY cf.createdat DESC, cf.displayname;
END
GO
