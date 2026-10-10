CREATE OR ALTER PROCEDURE dbo.sp_admission_get_final_invoice
    @tenantid    UNIQUEIDENTIFIER,
    @admissionid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @chargestotal DECIMAL(18, 2);
    DECLARE @paidtotal DECIMAL(18, 2);
    DECLARE @discount DECIMAL(18, 2);

    SELECT @chargestotal = ISNULL(SUM(c.amount), 0)
    FROM dbo.admission_charges c
    WHERE c.tenantid = @tenantid
      AND c.admissionid = @admissionid;

    SELECT @paidtotal = ISNULL(SUM(p.amount), 0)
    FROM dbo.admission_payments p
    WHERE p.tenantid = @tenantid
      AND p.admissionid = @admissionid;

    SELECT @discount = a.discountamount
    FROM dbo.admissions a
    WHERE a.tenantid = @tenantid
      AND a.admissionid = @admissionid
      AND a.isdeleted = 0;

    SET @discount = ISNULL(@discount, 0);

    -- 1) Header / patient / hospital / template
    SELECT
        a.admissionid,
        a.admissioncode,
        a.invoicenumber,
        a.admittedat,
        a.dischargedat,
        a.admissionstatus,
        a.discountamount,
        a.discountreason,
        a.ward,
        a.bed,
        a.roomclass,
        a.patientid,
        pt.patientcode,
        pt.firstname AS patientfirstname,
        pt.lastname AS patientlastname,
        pt.age AS patientage,
        pt.gender AS patientgender,
        pt.dateofbirth,
        pt.phonecipher,
        pt.addresscipher,
        a.attendingdoctorid,
        d.firstname AS doctorfirstname,
        d.lastname AS doctorlastname,
        d.designation AS doctordesignation,
        @chargestotal AS chargestotal,
        CASE WHEN @chargestotal - @discount < 0 THEN 0 ELSE @chargestotal - @discount END AS billtotal,
        @paidtotal AS paidtotal,
        (CASE WHEN @chargestotal - @discount < 0 THEN 0 ELSE @chargestotal - @discount END) - @paidtotal AS balancedue,
        t.hospitalname,
        t.tenantaddress AS hospitaladdress,
        t.primarycontactphone AS hospitalphone,
        t.logourl,
        t.website,
        t.hospitalregistrationno AS registrationnumber,
        ts.receiptheadertext,
        ts.receiptfootertext,
        tpl.documenttemplateid,
        tpl.bodyhtml AS templatebodyhtml,
        procinfo.procedurechargedon
    FROM dbo.admissions a
    INNER JOIN dbo.patients pt
        ON pt.patientid = a.patientid
       AND pt.tenantid = a.tenantid
       AND pt.isdeleted = 0
    INNER JOIN dbo.users d ON d.userid = a.attendingdoctorid
    INNER JOIN dbo.tenants t ON t.tenantid = a.tenantid AND t.isdeleted = 0
    LEFT JOIN dbo.tenant_settings ts ON ts.tenantid = a.tenantid
    OUTER APPLY (
        SELECT TOP (1) dt.documenttemplateid, dt.bodyhtml
        FROM dbo.document_templates dt
        WHERE dt.tenantid = a.tenantid
          AND dt.templatetype = 3
          AND dt.isdeleted = 0
          AND dt.isdefault = 1
        ORDER BY dt.updatedat DESC
    ) tpl
    OUTER APPLY (
        SELECT TOP (1) c.chargedon AS procedurechargedon
        FROM dbo.admission_charges c
        WHERE c.tenantid = a.tenantid
          AND c.admissionid = a.admissionid
          AND c.chargecategory = 2 -- Procedure
        ORDER BY c.chargedon ASC
    ) procinfo
    WHERE a.tenantid = @tenantid
      AND a.admissionid = @admissionid
      AND a.isdeleted = 0;

    -- 2) Charges (oldest first for invoice S.No)
    SELECT
        c.admissionchargeid,
        c.chargecategory,
        c.description,
        c.amount,
        c.chargedon
    FROM dbo.admission_charges c
    WHERE c.tenantid = @tenantid
      AND c.admissionid = @admissionid
    ORDER BY c.chargedon ASC, c.admissionchargeid ASC;

    -- 3) Payments (for mode / paid date summary)
    SELECT
        p.admissionpaymentid,
        p.amount,
        p.paymentmethod,
        p.paymentkind,
        p.receiptnumber,
        p.collectiondatetime
    FROM dbo.admission_payments p
    WHERE p.tenantid = @tenantid
      AND p.admissionid = @admissionid
    ORDER BY p.collectiondatetime DESC;
END
GO
