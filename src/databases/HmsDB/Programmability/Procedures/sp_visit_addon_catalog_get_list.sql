CREATE OR ALTER PROCEDURE dbo.sp_visit_addon_catalog_get_list
    @tenantid    UNIQUEIDENTIFIER,
    @page        INT = 1,
    @pagesize    INT = 10,
    @filter      NVARCHAR(100) = NULL,
    @addonstatus TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page - 1) * @pagesize;
    DECLARE @like NVARCHAR(102) = NULL;

    IF @filter IS NOT NULL AND LTRIM(RTRIM(@filter)) <> ''
        SET @like = '%' + LTRIM(RTRIM(@filter)) + '%';

    SELECT
        c.visitaddonid,
        c.name,
        c.code,
        c.defaultamount,
        c.addonstatus,
        c.createdat,
        c.updatedat,
        COUNT(*) OVER() AS totalcount
    FROM dbo.visit_addon_catalog c
    WHERE c.tenantid = @tenantid
      AND (@addonstatus IS NULL OR c.addonstatus = @addonstatus)
      AND (@like IS NULL
           OR c.name LIKE @like
           OR c.code LIKE @like)
    ORDER BY c.name
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
