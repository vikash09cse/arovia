CREATE OR ALTER PROCEDURE dbo.sp_admission_get_by_id
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

    -- 1) Header
    SELECT
        a.admissionid,
        a.admissioncode,
        a.admittedat,
        a.dischargedat,
        a.ward,
        a.bed,
        a.roomclass,
        a.admissionstatus,
        a.estimatedamount,
        a.notes,
        a.discountamount,
        a.discountreason,
        a.invoicenumber,
        a.fromvisitid,
        fv.visitcode AS fromvisitcode,
        a.departmentid,
        dep.name AS departmentname,
        a.patientid,
        pt.patientcode,
        pt.firstname AS patientfirstname,
        pt.lastname AS patientlastname,
        a.attendingdoctorid,
        d.firstname AS doctorfirstname,
        d.lastname AS doctorlastname,
        @chargestotal AS chargestotal,
        CASE WHEN @chargestotal - @discount < 0 THEN 0 ELSE @chargestotal - @discount END AS billtotal,
        @paidtotal AS paidtotal,
        (CASE WHEN @chargestotal - @discount < 0 THEN 0 ELSE @chargestotal - @discount END) - @paidtotal AS balancedue,
        a.createdat
    FROM dbo.admissions a
    INNER JOIN dbo.patients pt
        ON pt.patientid = a.patientid
       AND pt.tenantid = a.tenantid
       AND pt.isdeleted = 0
    INNER JOIN dbo.departments dep
        ON dep.departmentid = a.departmentid
       AND dep.tenantid = a.tenantid
    INNER JOIN dbo.users d ON d.userid = a.attendingdoctorid
    LEFT JOIN dbo.visits fv
        ON fv.visitid = a.fromvisitid
       AND fv.tenantid = a.tenantid
    WHERE a.tenantid = @tenantid
      AND a.admissionid = @admissionid
      AND a.isdeleted = 0;

    -- 2) Charges
    SELECT
        c.admissionchargeid,
        c.chargecategory,
        c.description,
        c.amount,
        c.chargedon,
        c.createdby AS createdbyuserid,
        u.firstname AS creatorfirstname,
        u.lastname AS creatorlastname
    FROM dbo.admission_charges c
    INNER JOIN dbo.users u ON u.userid = c.createdby
    WHERE c.tenantid = @tenantid
      AND c.admissionid = @admissionid
    ORDER BY c.chargedon DESC;

    -- 3) Payments
    SELECT
        p.admissionpaymentid,
        p.amount,
        p.paymentmethod,
        p.paymentkind,
        p.receiptnumber,
        p.notes,
        p.collectiondatetime,
        p.collectedby AS collectedbyuserid,
        c.firstname AS collectorfirstname,
        c.lastname AS collectorlastname
    FROM dbo.admission_payments p
    INNER JOIN dbo.users c ON c.userid = p.collectedby
    WHERE p.tenantid = @tenantid
      AND p.admissionid = @admissionid
    ORDER BY p.collectiondatetime DESC;
END
GO
