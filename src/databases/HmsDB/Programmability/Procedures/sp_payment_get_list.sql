CREATE OR ALTER PROCEDURE dbo.sp_payment_get_list

    @tenantid           UNIQUEIDENTIFIER,

    @page               INT = 1,

    @pagesize           INT = 10,

    @patientcode        NVARCHAR(20) = NULL,

    @openvisitsonly     BIT = 0,

    @datefrom           DATE = NULL,

    @dateto             DATE = NULL

AS

BEGIN

    SET NOCOUNT ON;



    DECLARE @offset INT = (@page - 1) * @pagesize;



    ;WITH visit_balances AS (

        SELECT

            v.visitid,

            ISNULL(v.totalchargeamount, 0) AS totaldue,

            ISNULL(SUM(CASE WHEN p.paymentstatus = 2 THEN p.amountpaid ELSE 0 END), 0) AS totalcollected

        FROM dbo.visits v

        LEFT JOIN dbo.payments p ON p.visitid = v.visitid AND p.tenantid = v.tenantid

        WHERE v.tenantid = @tenantid

        GROUP BY v.visitid, v.totalchargeamount

    )

    SELECT

        p.paymentid,

        p.visitid,

        v.visitcode,

        v.visitdatetime,

        v.visitstatus,

        p.patientid,

        pt.patientcode,

        pt.firstname AS patientfirstname,

        pt.lastname AS patientlastname,

        p.paymentlinetype,

        p.feeamount,

        p.paymentstatus,

        p.amountpaid,

        p.collectiondatetime,

        p.receiptnumber,

        p.notes,

        p.createdat,

        vb.totaldue,

        vb.totalcollected,

        vb.totaldue - vb.totalcollected AS balancedue,

        c.userid AS collectedbyuserid,

        c.firstname AS collectorfirstname,

        c.lastname AS collectorlastname,

        COUNT(*) OVER() AS totalcount

    FROM dbo.payments p

    INNER JOIN dbo.visits v

        ON v.visitid = p.visitid AND v.tenantid = p.tenantid

    INNER JOIN dbo.patients pt

        ON pt.patientid = p.patientid AND pt.tenantid = p.tenantid AND pt.isdeleted = 0

    INNER JOIN visit_balances vb ON vb.visitid = p.visitid

    LEFT JOIN dbo.users c ON c.userid = p.collectedby

    WHERE p.tenantid = @tenantid

      AND p.paymentstatus = 2

      AND (@patientcode IS NULL OR pt.patientcode = @patientcode)

      AND (@openvisitsonly = 0 OR (vb.totaldue > 0 AND vb.totalcollected < vb.totaldue))

      AND (@datefrom IS NULL OR CAST(COALESCE(p.collectiondatetime, p.createdat) AS DATE) >= @datefrom)

      AND (@dateto IS NULL OR CAST(COALESCE(p.collectiondatetime, p.createdat) AS DATE) <= @dateto)

    ORDER BY p.collectiondatetime DESC, p.createdat DESC

    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;

END

GO


