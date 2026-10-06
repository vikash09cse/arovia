CREATE OR ALTER PROCEDURE dbo.sp_admission_apply_discount
    @tenantid        UNIQUEIDENTIFIER,
    @admissionid     UNIQUEIDENTIFIER,
    @discountamount  DECIMAL(18, 2),
    @discountreason  NVARCHAR(500) = NULL,
    @actorid         UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.admissions a
        WHERE a.tenantid = @tenantid
          AND a.admissionid = @admissionid
          AND a.admissionstatus = 1
          AND a.isdeleted = 0)
        THROW 50400, 'Admission not found or not admitted.', 1;

    IF @discountamount IS NULL OR @discountamount < 0
        THROW 50400, 'Discount cannot be negative.', 1;

    IF @discountamount > 0
       AND (@discountreason IS NULL OR LTRIM(RTRIM(@discountreason)) = N'')
        THROW 50400, 'Discount reason is required when discount is greater than zero.', 1;

    DECLARE @chargestotal DECIMAL(18, 2) = (
        SELECT ISNULL(SUM(c.amount), 0)
        FROM dbo.admission_charges c
        WHERE c.tenantid = @tenantid
          AND c.admissionid = @admissionid);

    IF @discountamount > @chargestotal
        THROW 50400, 'Discount cannot exceed charges total.', 1;

    UPDATE dbo.admissions
    SET discountamount = @discountamount,
        discountreason = CASE
            WHEN @discountamount = 0 THEN NULL
            ELSE LTRIM(RTRIM(@discountreason))
        END,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND admissionid = @admissionid;

    SELECT CAST(1 AS BIT) AS success;
END
GO
