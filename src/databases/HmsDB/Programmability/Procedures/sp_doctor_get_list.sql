CREATE OR ALTER PROCEDURE dbo.sp_doctor_get_list
    @tenantid UNIQUEIDENTIFIER,
    @page     INT = 1,
    @pagesize INT = 10,
    @filter   NVARCHAR(100) = NULL,
    @userstatus TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page - 1) * @pagesize;
    DECLARE @like NVARCHAR(102) = NULL;

    IF @filter IS NOT NULL AND LTRIM(RTRIM(@filter)) <> ''
        SET @like = '%' + LTRIM(RTRIM(@filter)) + '%';

    SELECT
        u.userid,
        u.email,
        u.firstname,
        u.lastname,
        u.usertype AS role,
        u.userstatus AS status,
        u.lastloginat,
        u.createdat,
        COUNT(*) OVER() AS totalcount
    FROM dbo.users u
    WHERE u.tenantid = @tenantid
      AND u.usertype = 3
      AND u.isdeleted = 0
      AND (@userstatus IS NULL OR u.userstatus = @userstatus)
      AND (@like IS NULL OR u.email LIKE @like OR u.firstname LIKE @like OR u.lastname LIKE @like)
    ORDER BY u.lastname, u.firstname
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
