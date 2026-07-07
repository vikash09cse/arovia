CREATE OR ALTER PROCEDURE dbo.sp_payment_collect_visit_pending

    @tenantid       UNIQUEIDENTIFIER,

    @visitid        UNIQUEIDENTIFIER,

    @actorid        UNIQUEIDENTIFIER,

    @collectedby    UNIQUEIDENTIFIER = NULL

AS

BEGIN

    SET NOCOUNT ON;



    DECLARE @totaldue DECIMAL(18, 2);

    DECLARE @totalcollected DECIMAL(18, 2);

    DECLARE @balancedue DECIMAL(18, 2);

    DECLARE @collector UNIQUEIDENTIFIER = ISNULL(@collectedby, @actorid);



    SELECT @totaldue = ISNULL(v.totalchargeamount, 0)

    FROM dbo.visits v

    WHERE v.visitid = @visitid

      AND v.tenantid = @tenantid

      AND v.visitstatus = 1;



    IF @totaldue IS NULL

        THROW 50400, 'Visit not found or not active.', 1;



    SELECT @totalcollected = ISNULL(SUM(p.amountpaid), 0)

    FROM dbo.payments p

    WHERE p.tenantid = @tenantid

      AND p.visitid = @visitid

      AND p.paymentstatus = 2;



    SET @balancedue = @totaldue - @totalcollected;



    IF @balancedue <= 0

        RETURN;



    EXEC dbo.sp_payment_add_collection

        @tenantid = @tenantid,

        @visitid = @visitid,

        @amount = @balancedue,

        @collectedby = @collector,

        @notes = NULL,

        @paymentmethod = NULL,

        @actorid = @actorid;

END

GO


