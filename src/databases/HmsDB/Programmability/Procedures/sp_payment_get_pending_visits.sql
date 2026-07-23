CREATE OR ALTER PROCEDURE dbo.sp_payment_get_pending_visits
    @tenantid           UNIQUEIDENTIFIER,
    @page               INT = 1,
    @pagesize           INT = 10,
    @patientcode        NVARCHAR(20) = NULL,
    @datefrom           DATE = NULL,
    @dateto             DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page - 1) * @pagesize;

    ;WITH visit_balances AS (
        SELECT
            v.visitid,
            v.visitcode,
            v.visitdatetime,
            v.visitstatus,
            v.patientid,
            ISNULL(v.totalchargeamount, 0) AS totaldue,
            ISNULL(SUM(CASE WHEN p.paymentstatus = 2 THEN p.amountpaid ELSE 0 END), 0) AS totalcollected
        FROM dbo.visits v
        INNER JOIN dbo.patients pt
            ON pt.patientid = v.patientid
           AND pt.tenantid = v.tenantid
           AND pt.isdeleted = 0
        LEFT JOIN dbo.payments p
            ON p.visitid = v.visitid
           AND p.tenantid = v.tenantid
        WHERE v.tenantid = @tenantid
          AND v.isdeleted = 0
          AND v.visitstatus = 1
          AND ISNULL(v.totalchargeamount, 0) > 0
          AND (@patientcode IS NULL OR pt.patientcode = @patientcode)
          AND (@datefrom IS NULL OR CAST(v.visitdatetime AS DATE) >= @datefrom)
          AND (@dateto IS NULL OR CAST(v.visitdatetime AS DATE) <= @dateto)
        GROUP BY
            v.visitid,
            v.visitcode,
            v.visitdatetime,
            v.visitstatus,
            v.patientid,
            v.totalchargeamount
    )
    SELECT
        vb.visitid,
        vb.visitcode,
        vb.visitdatetime,
        vb.visitstatus,
        pt.patientid,
        pt.patientcode,
        pt.firstname AS patientfirstname,
        pt.lastname AS patientlastname,
        vb.totaldue,
        vb.totalcollected,
        vb.totaldue - vb.totalcollected AS balancedue,
        d.userid AS consultingdoctorid,
        d.firstname AS doctorfirstname,
        d.lastname AS doctorlastname,
        COUNT(*) OVER() AS totalcount
    FROM visit_balances vb
    INNER JOIN dbo.patients pt
        ON pt.patientid = vb.patientid
    INNER JOIN dbo.visits v
        ON v.visitid = vb.visitid
    INNER JOIN dbo.users d
        ON d.userid = v.consultingdoctorid
    WHERE vb.totaldue > vb.totalcollected
    ORDER BY vb.visitdatetime DESC
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
