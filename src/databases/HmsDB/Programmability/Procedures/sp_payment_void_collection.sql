CREATE OR ALTER PROCEDURE dbo.sp_payment_void_collection
    @tenantid   UNIQUEIDENTIFIER,
    @paymentid  UNIQUEIDENTIFIER,
    @reason     NVARCHAR(500) = NULL,
    @actorid    UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @visitstatus TINYINT;

    SELECT @visitstatus = v.visitstatus
    FROM dbo.payments p
    INNER JOIN dbo.visits v ON v.visitid = p.visitid AND v.tenantid = p.tenantid
    WHERE p.paymentid = @paymentid
      AND p.tenantid = @tenantid
      AND p.paymentstatus = 2;

    IF @visitstatus IS NULL
        THROW 50404, 'Payment collection not found.', 1;

    IF @visitstatus <> 1
        THROW 50400, 'Cannot void payment for a cancelled visit.', 1;

    UPDATE dbo.payments
    SET paymentstatus = 3,
        notes = CASE
            WHEN @reason IS NOT NULL AND LTRIM(RTRIM(@reason)) <> ''
                THEN COALESCE(notes + N' | ', N'') + N'Voided: ' + LTRIM(RTRIM(@reason))
            ELSE notes
        END,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE paymentid = @paymentid
      AND tenantid = @tenantid
      AND paymentstatus = 2;

    IF @@ROWCOUNT = 0
        THROW 50404, 'Payment collection not found.', 1;
END
GO
