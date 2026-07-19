CREATE OR ALTER PROCEDURE dbo.sp_common_file_save
    @tenantid       UNIQUEIDENTIFIER,
    @displayname    NVARCHAR(260),
    @storedfilename NVARCHAR(260),
    @actorid        UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @trimmeddisplay NVARCHAR(260) = LTRIM(RTRIM(@displayname));
    DECLARE @trimmedstored NVARCHAR(260) = LTRIM(RTRIM(@storedfilename));
    DECLARE @commonfileid UNIQUEIDENTIFIER = NEWID();

    IF @trimmeddisplay IS NULL OR @trimmeddisplay = ''
        THROW 50400, 'File name is required.', 1;

    IF @trimmedstored IS NULL OR @trimmedstored = ''
        THROW 50400, 'Stored file name is required.', 1;

    INSERT INTO dbo.common_files (
        commonfileid, tenantid, displayname, storedfilename, isdeleted, createdby)
    VALUES (
        @commonfileid, @tenantid, @trimmeddisplay, @trimmedstored, 0, @actorid);

    SELECT
        cf.commonfileid,
        cf.displayname,
        cf.storedfilename,
        cf.createdat,
        cf.createdby
    FROM dbo.common_files cf
    WHERE cf.commonfileid = @commonfileid;
END
GO
