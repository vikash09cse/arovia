CREATE OR ALTER PROCEDURE dbo.sp_user_document_save
    @tenantid        UNIQUEIDENTIFIER,
    @userid          UNIQUEIDENTIFIER,
    @documenttype    TINYINT,
    @displayname     NVARCHAR(260),
    @storedfilename  NVARCHAR(260),
    @actorid         UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @trimmeddisplay NVARCHAR(260) = LTRIM(RTRIM(@displayname));
    DECLARE @trimmedstored NVARCHAR(260) = LTRIM(RTRIM(@storedfilename));
    DECLARE @userdocumentid UNIQUEIDENTIFIER = NEWID();
    DECLARE @usertype TINYINT;

    SELECT @usertype = u.usertype
    FROM dbo.users u
    WHERE u.tenantid = @tenantid
      AND u.userid = @userid
      AND u.isdeleted = 0;

    IF @usertype IS NULL
        THROW 50404, 'User not found.', 1;

    -- Staff (2) or Doctor (3) only
    IF @usertype NOT IN (2, 3)
        THROW 50400, 'Documents can only be uploaded for Staff or Doctor users.', 1;

    IF @documenttype NOT IN (1, 2, 3, 4)
        THROW 50400, 'Invalid document type.', 1;

    IF @trimmeddisplay IS NULL OR @trimmeddisplay = ''
        THROW 50400, 'File name is required.', 1;

    IF @trimmedstored IS NULL OR @trimmedstored = ''
        THROW 50400, 'Stored file name is required.', 1;

    INSERT INTO dbo.user_documents (
        userdocumentid, tenantid, userid, documenttype, displayname, storedfilename, isdeleted, createdby)
    VALUES (
        @userdocumentid, @tenantid, @userid, @documenttype, @trimmeddisplay, @trimmedstored, 0, @actorid);

    SELECT
        d.userdocumentid,
        d.userid,
        d.documenttype,
        d.displayname,
        d.storedfilename,
        d.createdat,
        d.createdby
    FROM dbo.user_documents d
    WHERE d.userdocumentid = @userdocumentid;
END
GO
