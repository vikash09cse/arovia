CREATE OR ALTER PROCEDURE dbo.sp_admission_add_charge
    @tenantid       UNIQUEIDENTIFIER,
    @admissionid    UNIQUEIDENTIFIER,
    @chargecategory TINYINT,
    @description    NVARCHAR(250),
    @amount         DECIMAL(18, 2),
    @actorid        UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.admissions a
        WHERE a.tenantid = @tenantid
          AND a.admissionid = @admissionid
          AND a.admissionstatus = 1)
        THROW 50400, 'Admission not found or not admitted.', 1;

    IF @chargecategory NOT BETWEEN 1 AND 6
        THROW 50400, 'Invalid charge category.', 1;

    IF @description IS NULL OR LTRIM(RTRIM(@description)) = N''
        THROW 50400, 'Charge description is required.', 1;

    IF @amount IS NULL OR @amount < 0
        THROW 50400, 'Charge amount cannot be negative.', 1;

    DECLARE @admissionchargeid UNIQUEIDENTIFIER = NEWID();
    DECLARE @chargedon DATETIME2 = SYSUTCDATETIME();

    INSERT INTO dbo.admission_charges (
        admissionchargeid, tenantid, admissionid, chargecategory, description, amount, chargedon, createdby)
    VALUES (
        @admissionchargeid, @tenantid, @admissionid, @chargecategory,
        LTRIM(RTRIM(@description)), @amount, @chargedon, @actorid);

    UPDATE dbo.admissions
    SET updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND admissionid = @admissionid;

    SELECT @admissionchargeid AS admissionchargeid;
END
GO
