CREATE OR ALTER PROCEDURE dbo.sp_allocate_invoice_number
    @tenantid       UNIQUEIDENTIFIER,
    @procedurecode  NVARCHAR(20),
    @billdate       DATE,
    @invoicenumber  NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @prefix NVARCHAR(10);
    DECLARE @proc NVARCHAR(20);
    DECLARE @datePart NVARCHAR(8);
    DECLARE @seq INT;
    DECLARE @seqText NVARCHAR(4);
    DECLARE @hospitalname NVARCHAR(200);
    DECLARE @patientidprefix NVARCHAR(10);
    DECLARE @name NVARCHAR(200);
    DECLARE @p NVARCHAR(10);
    DECLARE @i INT;
    DECLARE @len INT;
    DECLARE @prev NCHAR(1);
    DECLARE @ch NCHAR(1);

    SELECT
        @hospitalname = t.hospitalname,
        @patientidprefix = ts.patientidprefix
    FROM dbo.tenants t
    LEFT JOIN dbo.tenant_settings ts ON ts.tenantid = t.tenantid
    WHERE t.tenantid = @tenantid
      AND t.isdeleted = 0;

    SET @prefix = UPPER(LTRIM(RTRIM(REPLACE(REPLACE(ISNULL(@patientidprefix, N''), N'-', N''), N' ', N''))));
    IF @prefix IS NULL OR LEN(@prefix) < 2
    BEGIN
        -- First letters of up to 3 words from hospital name (Janak Uro Care → JUC)
        SET @name = LTRIM(RTRIM(ISNULL(@hospitalname, N'HMS')));
        SET @p = N'';
        SET @i = 1;
        SET @len = LEN(@name);
        SET @prev = N' ';

        WHILE @i <= @len AND LEN(@p) < 3
        BEGIN
            SET @ch = SUBSTRING(@name, @i, 1);
            IF @prev = N' ' AND @ch <> N' '
                SET @p = @p + UPPER(@ch);
            SET @prev = @ch;
            SET @i = @i + 1;
        END

        SET @prefix = CASE WHEN LEN(@p) >= 2 THEN @p ELSE N'HMS' END;
    END

    SET @proc = UPPER(LTRIM(RTRIM(ISNULL(NULLIF(@procedurecode, N''), N'IPD'))));
    SET @proc = REPLACE(@proc, N' ', N'');
    IF LEN(@proc) < 2
        SET @proc = N'IPD';
    IF LEN(@proc) > 12
        SET @proc = LEFT(@proc, 12);

    SET @datePart = CONVERT(NVARCHAR(8), @billdate, 112); -- yyyyMMdd

    -- Daily sequence for this tenant + date part
    SELECT @seq = COUNT(1) + 1
    FROM dbo.admissions a
    WHERE a.tenantid = @tenantid
      AND a.isdeleted = 0
      AND a.invoicenumber LIKE N'%/' + @datePart + N'-%';

    IF @seq < 1 SET @seq = 1;

    SET @seqText = CASE
        WHEN @seq < 100 THEN RIGHT(REPLICATE(N'0', 2) + CAST(@seq AS NVARCHAR(10)), 2)
        ELSE CAST(@seq AS NVARCHAR(10))
    END;

    SET @invoicenumber = @prefix + N'/' + @proc + N'/' + @datePart + N'-' + @seqText;

    -- Keep sequence table advancing for diagnostics / compatibility
    IF NOT EXISTS (
        SELECT 1 FROM dbo.invoice_sequences WITH (UPDLOCK, ROWLOCK)
        WHERE tenantid = @tenantid)
    BEGIN
        INSERT INTO dbo.invoice_sequences (tenantid, nextsequencenumber)
        VALUES (@tenantid, 1);
    END

    UPDATE dbo.invoice_sequences
    SET nextsequencenumber = nextsequencenumber + 1,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid;
END
GO
