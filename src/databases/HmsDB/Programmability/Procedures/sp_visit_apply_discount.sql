CREATE OR ALTER PROCEDURE dbo.sp_visit_apply_discount
    @tenantid        UNIQUEIDENTIFIER,
    @visitid         UNIQUEIDENTIFIER,
    @discountamount  DECIMAL(18, 2),
    @discountreason  NVARCHAR(500) = NULL,
    @actorid         UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @discountamount IS NULL OR @discountamount < 0
        THROW 50400, 'Invalid discount amount.', 1;

    DECLARE @subtotal DECIMAL(18, 2);
    DECLARE @totalcollected DECIMAL(18, 2);
    DECLARE @net DECIMAL(18, 2);
    DECLARE @storedDiscount DECIMAL(18, 2);
    DECLARE @storedReason NVARCHAR(500);

    SELECT
        @subtotal = ISNULL(v.feeamount, 0)
            + ISNULL(v.procedurechargeamount, 0)
            + ISNULL((SELECT SUM(l.amount) FROM dbo.visit_addon_lines l WHERE l.visitid = v.visitid AND l.tenantid = v.tenantid), 0)
    FROM dbo.visits v
    WHERE v.visitid = @visitid
      AND v.tenantid = @tenantid
      AND v.visitstatus = 1
      AND v.isdeleted = 0;

    IF @subtotal IS NULL
        THROW 50404, 'Visit not found or cancelled.', 1;

    IF @discountamount = 0
    BEGIN
        SET @storedDiscount = NULL;
        SET @storedReason = NULL;
    END
    ELSE
    BEGIN
        IF @discountreason IS NULL OR LTRIM(RTRIM(@discountreason)) = ''
            THROW 50400, 'Discount reason is required when applying a discount.', 1;

        IF @discountamount > @subtotal
            THROW 50400, 'Discount amount cannot exceed the visit subtotal.', 1;

        SET @storedDiscount = @discountamount;
        SET @storedReason = LTRIM(RTRIM(@discountreason));
    END

    SET @net = @subtotal - ISNULL(@storedDiscount, 0);

    SELECT @totalcollected = ISNULL(SUM(p.amountpaid), 0)
    FROM dbo.payments p
    WHERE p.tenantid = @tenantid
      AND p.visitid = @visitid
      AND p.paymentstatus = 2;

    IF @net < @totalcollected
        THROW 50400, 'Discount would make the visit total less than amount already collected.', 1;

    UPDATE dbo.visits
    SET discountamount = @storedDiscount,
        discountreason = @storedReason,
        totalchargeamount = NULLIF(@net, 0),
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE visitid = @visitid
      AND tenantid = @tenantid;
END
GO
