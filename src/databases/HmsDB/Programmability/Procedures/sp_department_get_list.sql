CREATE OR ALTER PROCEDURE dbo.sp_department_get_list
    @tenantid          UNIQUEIDENTIFIER,
    @page              INT = 1,
    @pagesize          INT = 10,
    @filter            NVARCHAR(150) = NULL,
    @departmentstatus  TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page - 1) * @pagesize;
    DECLARE @like NVARCHAR(152) = NULL;

    IF @filter IS NOT NULL AND LTRIM(RTRIM(@filter)) <> N''
        SET @like = N'%' + LTRIM(RTRIM(@filter)) + N'%';

    SELECT
        d.departmentid,
        d.name,
        d.departmentstatus,
        d.createdat,
        d.updatedat,
        COUNT(*) OVER() AS totalcount
    FROM dbo.departments d
    WHERE d.tenantid = @tenantid
      AND (@departmentstatus IS NULL OR d.departmentstatus = @departmentstatus)
      AND (@like IS NULL OR d.name LIKE @like)
    ORDER BY d.name
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
