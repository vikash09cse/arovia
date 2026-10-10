-- Deploy new invoice numbering SPs, then convert legacy INV-####### values.

IF COL_LENGTH('dbo.admissions', 'invoicenumber') IS NOT NULL
BEGIN
    ALTER TABLE dbo.admissions ALTER COLUMN invoicenumber NVARCHAR(50) NULL;
END
GO

UPDATE ts
SET patientidprefix = N'JUC'
FROM dbo.tenant_settings ts
INNER JOIN dbo.tenants t ON t.tenantid = ts.tenantid
WHERE t.isdeleted = 0
  AND (
        UPPER(REPLACE(REPLACE(ISNULL(ts.patientidprefix, N''), N'-', N''), N' ', N'')) IN (N'JU', N'JUC')
     OR t.hospitalname LIKE N'%Janak%Uro%'
  );
GO

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

    SET @datePart = CONVERT(NVARCHAR(8), @billdate, 112);

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

CREATE OR ALTER PROCEDURE dbo.sp_admission_ensure_invoice_number
    @tenantid       UNIQUEIDENTIFIER,
    @admissionid    UNIQUEIDENTIFIER,
    @actorid        UNIQUEIDENTIFIER,
    @invoicenumber  NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status TINYINT;
    DECLARE @existing NVARCHAR(50);
    DECLARE @admittedat DATETIME2;
    DECLARE @procedurechargedon DATETIME2;
    DECLARE @proceduredesc NVARCHAR(500);
    DECLARE @procedurecode NVARCHAR(20);
    DECLARE @billdate DATE;

    SELECT
        @status = a.admissionstatus,
        @existing = a.invoicenumber,
        @admittedat = a.admittedat
    FROM dbo.admissions a WITH (UPDLOCK, ROWLOCK)
    WHERE a.tenantid = @tenantid
      AND a.admissionid = @admissionid
      AND a.isdeleted = 0;

    IF @status IS NULL
        THROW 50404, 'Admission not found.', 1;

    IF @status NOT IN (1, 2)
        THROW 50400, 'Invoice is available only for admitted or discharged stays.', 1;

    IF @existing IS NOT NULL AND LTRIM(RTRIM(@existing)) <> N''
       AND UPPER(LTRIM(RTRIM(@existing))) NOT LIKE N'INV-%'
    BEGIN
        SET @invoicenumber = @existing;
        RETURN;
    END

    SELECT TOP (1)
        @procedurechargedon = c.chargedon,
        @proceduredesc = c.description
    FROM dbo.admission_charges c
    WHERE c.tenantid = @tenantid
      AND c.admissionid = @admissionid
      AND c.chargecategory = 2
    ORDER BY c.chargedon ASC, c.admissionchargeid ASC;

    SET @procedurecode = N'IPD';
    IF @proceduredesc IS NOT NULL
    BEGIN
        IF @proceduredesc LIKE N'%UROFLOW%' SET @procedurecode = N'UROFLOW';
        ELSE IF @proceduredesc LIKE N'%CYSTO%' SET @procedurecode = N'CYSTO';
        ELSE IF @proceduredesc LIKE N'%PCNL%' SET @procedurecode = N'PCNL';
        ELSE IF @proceduredesc LIKE N'%RIRS%' SET @procedurecode = N'RIRS';
        ELSE IF @proceduredesc LIKE N'%URSL%' SET @procedurecode = N'URSL';
        ELSE IF @proceduredesc LIKE N'%TURP%' SET @procedurecode = N'TURP';
        ELSE IF @proceduredesc LIKE N'%HOLEP%' SET @procedurecode = N'HOLEP';
        ELSE IF @proceduredesc LIKE N'%LAPARO%' SET @procedurecode = N'LAP';
        ELSE IF @proceduredesc LIKE N'%DJ %' OR @proceduredesc LIKE N'%D-J%' OR @proceduredesc LIKE N'%DJ STENT%'
            SET @procedurecode = N'DJ';
    END

    SET @billdate = CAST(COALESCE(@procedurechargedon, @admittedat, SYSUTCDATETIME()) AS DATE);

    BEGIN TRAN;

    EXEC dbo.sp_allocate_invoice_number
        @tenantid = @tenantid,
        @procedurecode = @procedurecode,
        @billdate = @billdate,
        @invoicenumber = @invoicenumber OUTPUT;

    UPDATE dbo.admissions
    SET invoicenumber = @invoicenumber,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND admissionid = @admissionid
      AND isdeleted = 0
      AND (
            invoicenumber IS NULL
         OR LTRIM(RTRIM(invoicenumber)) = N''
         OR UPPER(LTRIM(RTRIM(invoicenumber))) LIKE N'INV-%'
      );

    SELECT @invoicenumber = a.invoicenumber
    FROM dbo.admissions a
    WHERE a.tenantid = @tenantid
      AND a.admissionid = @admissionid;

    COMMIT TRAN;
END
GO

DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @tenantid UNIQUEIDENTIFIER;
DECLARE @admissionid UNIQUEIDENTIFIER;
DECLARE @invoicenumber NVARCHAR(50);

DECLARE legacy_invoices CURSOR LOCAL FAST_FORWARD FOR
SELECT a.tenantid, a.admissionid
FROM dbo.admissions a
WHERE a.isdeleted = 0
  AND a.invoicenumber IS NOT NULL
  AND LTRIM(RTRIM(a.invoicenumber)) <> N''
  AND UPPER(LTRIM(RTRIM(a.invoicenumber))) LIKE N'INV-%'
ORDER BY a.admittedat ASC, a.createdat ASC;

OPEN legacy_invoices;
FETCH NEXT FROM legacy_invoices INTO @tenantid, @admissionid;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        EXEC dbo.sp_admission_ensure_invoice_number
            @tenantid = @tenantid,
            @admissionid = @admissionid,
            @actorid = @seedActor,
            @invoicenumber = @invoicenumber OUTPUT;
    END TRY
    BEGIN CATCH
        PRINT CONCAT(
            N'Skipped admission ', CONVERT(NVARCHAR(36), @admissionid),
            N': ', ERROR_MESSAGE());
    END CATCH;

    FETCH NEXT FROM legacy_invoices INTO @tenantid, @admissionid;
END

CLOSE legacy_invoices;
DEALLOCATE legacy_invoices;
GO
