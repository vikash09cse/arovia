CREATE OR ALTER PROCEDURE dbo.sp_diagnosis_master_get_list
    @tenantid   UNIQUEIDENTIFIER,
    @page       INT = 1,
    @pagesize   INT = 50,
    @filter     NVARCHAR(200) = NULL,
    @isactive   BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @page < 1 SET @page = 1;
    IF @pagesize < 1 SET @pagesize = 50;
    IF @pagesize > 200 SET @pagesize = 200;

    DECLARE @like NVARCHAR(202) = NULL;
    IF @filter IS NOT NULL AND LTRIM(RTRIM(@filter)) <> N''
        SET @like = N'%' + LTRIM(RTRIM(@filter)) + N'%';

    ;WITH filtered AS (
        SELECT
            dm.diagnosismasterid,
            dm.code,
            dm.name,
            dm.packjson,
            dm.sortorder,
            dm.isactive,
            dm.createdat,
            dm.updatedat,
            COUNT(*) OVER () AS totalcount
        FROM dbo.diagnosis_masters dm
        WHERE dm.tenantid = @tenantid
          AND (@isactive IS NULL OR dm.isactive = @isactive)
          AND (@like IS NULL OR dm.name LIKE @like OR dm.code LIKE @like)
    )
    SELECT *
    FROM filtered
    ORDER BY sortorder, name
    OFFSET (@page - 1) * @pagesize ROWS
    FETCH NEXT @pagesize ROWS ONLY;
END
GO
