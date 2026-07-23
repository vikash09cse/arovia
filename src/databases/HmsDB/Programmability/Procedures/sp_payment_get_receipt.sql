CREATE OR ALTER PROCEDURE dbo.sp_payment_get_receipt
    @tenantid  UNIQUEIDENTIFIER,
    @paymentid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @visitid UNIQUEIDENTIFIER;

    SELECT
        p.paymentid,
        p.receiptnumber,
        p.amountpaid,
        p.paymentmethod,
        p.collectiondatetime,
        p.notes,
        collector.firstname AS collectorfirstname,
        collector.lastname AS collectorlastname,
        v.visitid,
        v.visitcode,
        v.visitdatetime,
        v.feeamount AS consultationfee,
        v.procedurechargeamount AS procedurecharge,
        v.discountamount AS discount,
        ISNULL(addons.addoncharges, 0) AS addoncharges,
        v.totalchargeamount AS totaldue,
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
    FROM dbo.payments p
    INNER JOIN dbo.visits v
        ON v.visitid = p.visitid AND v.tenantid = p.tenantid AND v.isdeleted = 0
    INNER JOIN dbo.patients pt
        ON pt.patientid = p.patientid AND pt.tenantid = p.tenantid AND pt.isdeleted = 0
    INNER JOIN dbo.users d ON d.userid = v.consultingdoctorid
    LEFT JOIN dbo.users collector ON collector.userid = p.collectedby
    INNER JOIN dbo.tenants t ON t.tenantid = p.tenantid AND t.isdeleted = 0
    LEFT JOIN dbo.tenant_settings ts ON ts.tenantid = p.tenantid
    OUTER APPLY (
        SELECT SUM(al.amount) AS addoncharges
        FROM dbo.visit_addon_lines al
        WHERE al.tenantid = v.tenantid AND al.visitid = v.visitid
    ) addons
    OUTER APPLY (
        SELECT TOP (1) dt.documenttemplateid, dt.bodyhtml
        FROM dbo.document_templates dt
        WHERE dt.tenantid = p.tenantid
          AND dt.templatetype = 1
          AND dt.isdeleted = 0
          AND dt.isdefault = 1
        ORDER BY dt.updatedat DESC
    ) tpl
    WHERE p.tenantid = @tenantid
      AND p.paymentid = @paymentid
      AND p.paymentstatus = 2
      AND p.paymentlinetype = 3;

    SELECT @visitid = v.visitid
    FROM dbo.payments p
    INNER JOIN dbo.visits v
        ON v.visitid = p.visitid AND v.tenantid = p.tenantid AND v.isdeleted = 0
    INNER JOIN dbo.patients pt
        ON pt.patientid = p.patientid AND pt.tenantid = p.tenantid AND pt.isdeleted = 0
    WHERE p.tenantid = @tenantid
      AND p.paymentid = @paymentid
      AND p.paymentstatus = 2
      AND p.paymentlinetype = 3;

    SELECT
        al.addonname,
        al.amount
    FROM dbo.visit_addon_lines al
    WHERE al.tenantid = @tenantid
      AND al.visitid = @visitid
    ORDER BY al.addonname, al.visitaddonlineid;
END
GO
