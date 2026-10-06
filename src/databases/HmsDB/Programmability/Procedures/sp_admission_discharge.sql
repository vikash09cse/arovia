CREATE OR ALTER PROCEDURE dbo.sp_admission_discharge
    @tenantid    UNIQUEIDENTIFIER,
    @admissionid UNIQUEIDENTIFIER,
    @actorid     UNIQUEIDENTIFIER
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

    DECLARE @chargestotal DECIMAL(18, 2);
    DECLARE @paidtotal DECIMAL(18, 2);
    DECLARE @discount DECIMAL(18, 2);
    DECLARE @billtotal DECIMAL(18, 2);
    DECLARE @balance DECIMAL(18, 2);

    SELECT @discount = a.discountamount
    FROM dbo.admissions a
    WHERE a.tenantid = @tenantid
      AND a.admissionid = @admissionid;

    SELECT @chargestotal = ISNULL(SUM(c.amount), 0)
    FROM dbo.admission_charges c
    WHERE c.tenantid = @tenantid
      AND c.admissionid = @admissionid;

    SELECT @paidtotal = ISNULL(SUM(p.amount), 0)
    FROM dbo.admission_payments p
    WHERE p.tenantid = @tenantid
      AND p.admissionid = @admissionid;

    SET @billtotal = CASE WHEN @chargestotal - ISNULL(@discount, 0) < 0
        THEN 0 ELSE @chargestotal - ISNULL(@discount, 0) END;
    SET @balance = @billtotal - @paidtotal;

    IF @balance > 0
        THROW 50409, 'Cannot discharge while balance remains. Collect final payment first.', 1;

    UPDATE dbo.admissions
    SET admissionstatus = 2,
        dischargedat = SYSUTCDATETIME(),
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid
      AND admissionid = @admissionid;

    SELECT CAST(1 AS BIT) AS success;
END
GO
