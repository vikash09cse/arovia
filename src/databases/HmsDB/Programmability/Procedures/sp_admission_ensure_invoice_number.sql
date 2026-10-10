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
    DECLARE @dischargedat DATETIME2;
    DECLARE @procedurechargedon DATETIME2;
    DECLARE @proceduredesc NVARCHAR(500);
    DECLARE @procedurecode NVARCHAR(20);
    DECLARE @billdate DATE;
    DECLARE @isOldFormat BIT = 0;

    SELECT
        @status = a.admissionstatus,
        @existing = a.invoicenumber,
        @admittedat = a.admittedat,
        @dischargedat = a.dischargedat
    FROM dbo.admissions a WITH (UPDLOCK, ROWLOCK)
    WHERE a.tenantid = @tenantid
      AND a.admissionid = @admissionid
      AND a.isdeleted = 0;

    IF @status IS NULL
        THROW 50404, 'Admission not found.', 1;

    -- 1 = Admitted, 2 = Discharged
    IF @status NOT IN (1, 2)
        THROW 50400, 'Invoice is available only for admitted or discharged stays.', 1;

    -- Keep new-format numbers; regenerate legacy INV-###### codes
    IF @existing IS NOT NULL AND LTRIM(RTRIM(@existing)) <> N''
    BEGIN
        IF UPPER(LTRIM(RTRIM(@existing))) LIKE N'INV-%'
            SET @isOldFormat = 1;
        ELSE
        BEGIN
            SET @invoicenumber = @existing;
            RETURN;
        END
    END

    SELECT TOP (1)
        @procedurechargedon = c.chargedon,
        @proceduredesc = c.description
    FROM dbo.admission_charges c
    WHERE c.tenantid = @tenantid
      AND c.admissionid = @admissionid
      AND c.chargecategory = 2 -- Procedure
    ORDER BY c.chargedon ASC, c.admissionchargeid ASC;

    -- Known procedure short codes from charge description (sample: JUC/URSL/…)
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

    -- Prefer procedure date, else admission date (local calendar date)
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
