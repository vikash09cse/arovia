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
        INNER JOIN dbo.patients pt
            ON pt.patientid = v.patientid
           AND pt.tenantid = v.tenantid
           AND pt.isdeleted = 0
        WHERE v.tenantid = @tenantid
          AND v.isdeleted = 0
          AND v.visitstatus = 1
        GROUP BY v.visitid, v.totalchargeamount
    ),
    admission_balances AS (
        SELECT
            a.admissionid,
            CASE
                WHEN ISNULL(SUM(c.amount), 0) - a.discountamount < 0 THEN 0
                ELSE ISNULL(SUM(c.amount), 0) - a.discountamount
            END AS totaldue,
            ISNULL((
                SELECT SUM(ap.amount)
                FROM dbo.admission_payments ap
                WHERE ap.tenantid = a.tenantid
                  AND ap.admissionid = a.admissionid
            ), 0) AS totalcollected
        FROM dbo.admissions a
        INNER JOIN dbo.patients pt
            ON pt.patientid = a.patientid
           AND pt.tenantid = a.tenantid
           AND pt.isdeleted = 0
        LEFT JOIN dbo.admission_charges c
            ON c.admissionid = a.admissionid
           AND c.tenantid = a.tenantid
        WHERE a.tenantid = @tenantid
          AND a.isdeleted = 0
        GROUP BY a.admissionid, a.tenantid, a.discountamount
    ),
    unified AS (
        SELECT
            CAST(1 AS TINYINT) AS sourcetype,
            p.paymentid,
            p.visitid AS referenceid,
            v.visitcode AS referencecode,
            v.visitdatetime AS referencedatetime,
            v.visitstatus AS referencestatus,
            p.patientid,
            pt.patientcode,
            pt.firstname AS patientfirstname,
            pt.lastname AS patientlastname,
            p.amountpaid AS amount,
            p.collectiondatetime,
            p.receiptnumber,
            p.notes,
            p.createdat,
            vb.totaldue,
            vb.totalcollected,
            vb.totaldue - vb.totalcollected AS balancedue,
            c.userid AS collectedbyuserid,
            c.firstname AS collectorfirstname,
            c.lastname AS collectorlastname
        FROM dbo.payments p
        INNER JOIN dbo.visits v
            ON v.visitid = p.visitid AND v.tenantid = p.tenantid AND v.isdeleted = 0
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

        UNION ALL

        SELECT
            CAST(2 AS TINYINT) AS sourcetype,
            ap.admissionpaymentid AS paymentid,
            ap.admissionid AS referenceid,
            a.admissioncode AS referencecode,
            a.admittedat AS referencedatetime,
            a.admissionstatus AS referencestatus,
            ap.patientid,
            pt.patientcode,
            pt.firstname AS patientfirstname,
            pt.lastname AS patientlastname,
            ap.amount,
            ap.collectiondatetime,
            ap.receiptnumber,
            ap.notes,
            ap.createdat,
            ab.totaldue,
            ab.totalcollected,
            ab.totaldue - ab.totalcollected AS balancedue,
            c.userid AS collectedbyuserid,
            c.firstname AS collectorfirstname,
            c.lastname AS collectorlastname
        FROM dbo.admission_payments ap
        INNER JOIN dbo.admissions a
            ON a.admissionid = ap.admissionid AND a.tenantid = ap.tenantid AND a.isdeleted = 0
        INNER JOIN dbo.patients pt
            ON pt.patientid = ap.patientid AND pt.tenantid = ap.tenantid AND pt.isdeleted = 0
        INNER JOIN admission_balances ab ON ab.admissionid = ap.admissionid
        LEFT JOIN dbo.users c ON c.userid = ap.collectedby
        WHERE ap.tenantid = @tenantid
          AND (@patientcode IS NULL OR pt.patientcode = @patientcode)
          AND (@openvisitsonly = 0 OR (a.admissionstatus = 1 AND ab.totaldue > ab.totalcollected))
          AND (@datefrom IS NULL OR CAST(ap.collectiondatetime AS DATE) >= @datefrom)
          AND (@dateto IS NULL OR CAST(ap.collectiondatetime AS DATE) <= @dateto)
    )
    SELECT
        u.sourcetype,
        u.paymentid,
        u.referenceid,
        u.referencecode,
        u.referencedatetime,
        u.referencestatus,
        u.patientid,
        u.patientcode,
        u.patientfirstname,
        u.patientlastname,
        u.amount,
        u.collectiondatetime,
        u.receiptnumber,
        u.notes,
        u.createdat,
        u.totaldue,
        u.totalcollected,
        u.balancedue,
        u.collectedbyuserid,
        u.collectorfirstname,
        u.collectorlastname,
        COUNT(*) OVER() AS totalcount
    FROM unified u
    ORDER BY u.collectiondatetime DESC, u.createdat DESC
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
