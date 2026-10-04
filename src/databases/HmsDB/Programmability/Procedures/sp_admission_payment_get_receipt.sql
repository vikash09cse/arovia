CREATE OR ALTER PROCEDURE dbo.sp_admission_payment_get_receipt
    @tenantid            UNIQUEIDENTIFIER,
    @admissionpaymentid  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ap.admissionpaymentid AS paymentid,
        ap.receiptnumber,
        ap.amount AS amountpaid,
        ap.paymentmethod,
        ap.paymentkind,
        ap.collectiondatetime,
        ap.notes,
        collector.firstname AS collectorfirstname,
        collector.lastname AS collectorlastname,
        a.admissionid AS visitid,
        a.admissioncode AS visitcode,
        a.admittedat AS visitdatetime,
        CAST(0 AS DECIMAL(18, 2)) AS consultationfee,
        CAST(0 AS DECIMAL(18, 2)) AS procedurecharge,
        CAST(0 AS DECIMAL(18, 2)) AS discount,
        CAST(0 AS DECIMAL(18, 2)) AS addoncharges,
        ap.amount AS totaldue,
        CAST(2 AS TINYINT) AS sourcetype,
        pt.patientid,
        pt.patientcode,
        pt.firstname AS patientfirstname,
        pt.lastname AS patientlastname,
        pt.age AS patientage,
        pt.gender AS patientgender,
        pt.phonecipher,
        pt.addresscipher,
        d.firstname AS doctorfirstname,
        d.lastname AS doctorlastname,
        d.designation AS doctordesignation,
        t.hospitalname,
        t.tenantaddress AS hospitaladdress,
        t.primarycontactphone AS hospitalphone,
        t.logourl,
        t.website,
        ts.receiptheadertext,
        ts.receiptfootertext,
        ts.gsttaxnumber,
        tpl.documenttemplateid,
        tpl.bodyhtml AS templatebodyhtml
    FROM dbo.admission_payments ap
    INNER JOIN dbo.admissions a
        ON a.admissionid = ap.admissionid AND a.tenantid = ap.tenantid
    INNER JOIN dbo.patients pt
        ON pt.patientid = ap.patientid AND pt.tenantid = ap.tenantid AND pt.isdeleted = 0
    INNER JOIN dbo.users d ON d.userid = a.attendingdoctorid
    LEFT JOIN dbo.users collector ON collector.userid = ap.collectedby
    INNER JOIN dbo.tenants t ON t.tenantid = ap.tenantid AND t.isdeleted = 0
    LEFT JOIN dbo.tenant_settings ts ON ts.tenantid = ap.tenantid
    OUTER APPLY (
        SELECT TOP (1) dt.documenttemplateid, dt.bodyhtml
        FROM dbo.document_templates dt
        WHERE dt.tenantid = ap.tenantid
          AND dt.templatetype = 1
          AND dt.isdeleted = 0
          AND dt.isdefault = 1
        ORDER BY dt.updatedat DESC
    ) tpl
    WHERE ap.tenantid = @tenantid
      AND ap.admissionpaymentid = @admissionpaymentid;

    -- Empty addon resultset (shape matches visit receipt second RS)
    SELECT
        CAST(NULL AS NVARCHAR(200)) AS addonname,
        CAST(NULL AS DECIMAL(18, 2)) AS amount
    WHERE 1 = 0;
END
GO
