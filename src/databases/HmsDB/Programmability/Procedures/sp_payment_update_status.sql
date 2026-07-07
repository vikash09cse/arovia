CREATE OR ALTER PROCEDURE dbo.sp_payment_update_status
    @tenantid       UNIQUEIDENTIFIER,
    @paymentid      UNIQUEIDENTIFIER,
    @paymentstatus  TINYINT,
    @actorid        UNIQUEIDENTIFIER,
    @collectedby    UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @paymentstatus <> 3
        THROW 50400, 'Only void/refund is supported. Use add collection for new payments.', 1;

    EXEC dbo.sp_payment_void_collection
        @tenantid = @tenantid,
        @paymentid = @paymentid,
        @reason = NULL,
        @actorid = @actorid;
END
GO
