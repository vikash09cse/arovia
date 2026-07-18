CREATE OR ALTER PROCEDURE dbo.sp_visit_get_list
    @tenantid              UNIQUEIDENTIFIER,
    @page                  INT = 1,
    @pagesize              INT = 10,
    @patientid             UNIQUEIDENTIFIER = NULL,
    @patientcode           NVARCHAR(20) = NULL,
    @visitcode             NVARCHAR(30) = NULL,
    @phoneblindindex       BINARY(32) = NULL,
    @consultingdoctorid    UNIQUEIDENTIFIER = NULL,
    @visittype             TINYINT = NULL,
    @feestatus             TINYINT = NULL,
    @visitstatus           TINYINT = NULL,
    @datefrom              DATE = NULL,
    @dateto                DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page - 1) * @pagesize;

    ;WITH collection_totals AS (
        SELECT
            p.visitid,
            SUM(p.amountpaid) AS totalcollected
        FROM dbo.payments p
        WHERE p.tenantid = @tenantid
          AND p.paymentstatus = 2
        GROUP BY p.visitid
    )
    SELECT
        v.visitid,
        v.visitcode,
        v.visitdatetime,
        v.visittype,
        v.purpose,
        v.feestatus,
        v.feeamount,
        v.procedurechargeamount,
        v.totalchargeamount,
        v.visitstatus,
        v.scheduledsurgerydate,
        p.patientid,
        p.patientcode,
        p.firstname AS patientfirstname,
        p.lastname AS patientlastname,
        d.userid AS consultingdoctorid,
        d.firstname AS doctorfirstname,
        d.lastname AS doctorlastname,
        CASE
            WHEN v.totalchargeamount IS NULL OR v.totalchargeamount = 0 THEN 0
            WHEN ISNULL(ct.totalcollected, 0) = 0 THEN 1
            WHEN ISNULL(ct.totalcollected, 0) >= v.totalchargeamount THEN 2
            ELSE 3
        END AS aggregatedpaymentstatus,
        COUNT(*) OVER() AS totalcount
    FROM dbo.visits v
    INNER JOIN dbo.patients p ON p.patientid = v.patientid AND p.tenantid = v.tenantid AND p.isdeleted = 0
    INNER JOIN dbo.users d ON d.userid = v.consultingdoctorid
    LEFT JOIN collection_totals ct ON ct.visitid = v.visitid
    WHERE v.tenantid = @tenantid
      AND v.isdeleted = 0
      AND (@patientid IS NULL OR v.patientid = @patientid)
      AND (@patientcode IS NULL OR p.patientcode = @patientcode)
      AND (@visitcode IS NULL OR v.visitcode = @visitcode)
      AND (@phoneblindindex IS NULL OR p.phoneblindindex = @phoneblindindex)
      AND (@consultingdoctorid IS NULL OR v.consultingdoctorid = @consultingdoctorid)
      AND (@visittype IS NULL OR v.visittype = @visittype)
      AND (@feestatus IS NULL OR v.feestatus = @feestatus)
      AND (@visitstatus IS NULL OR v.visitstatus = @visitstatus)
      AND (@datefrom IS NULL OR CAST(v.visitdatetime AS DATE) >= @datefrom)
      AND (@dateto IS NULL OR CAST(v.visitdatetime AS DATE) <= @dateto)
    ORDER BY v.visitdatetime DESC
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
