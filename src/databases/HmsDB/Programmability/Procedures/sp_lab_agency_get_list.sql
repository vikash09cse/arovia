CREATE OR ALTER PROCEDURE dbo.sp_lab_agency_get_list
    @tenantid     UNIQUEIDENTIFIER,
    @page         INT = 1,
    @pagesize     INT = 10,
    @filter       NVARCHAR(100) = NULL,
    @agencystatus TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page - 1) * @pagesize;
    DECLARE @like NVARCHAR(102) = NULL;

    IF @filter IS NOT NULL AND LTRIM(RTRIM(@filter)) <> ''
        SET @like = '%' + LTRIM(RTRIM(@filter)) + '%';

    SELECT
        la.labagencyid,
        la.name,
        la.contactperson,
        la.phone,
        la.email,
        la.address,
        la.notes,
        la.agencystatus,
        la.createdat,
        la.updatedat,
        COUNT(*) OVER() AS totalcount
    FROM dbo.lab_agencies la
    WHERE la.tenantid = @tenantid
      AND (@agencystatus IS NULL OR la.agencystatus = @agencystatus)
      AND (@like IS NULL
           OR la.name LIKE @like
           OR la.contactperson LIKE @like
           OR la.phone LIKE @like
           OR la.email LIKE @like)
    ORDER BY la.name
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
