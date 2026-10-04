CREATE OR ALTER PROCEDURE dbo.sp_discharge_summary_get
    @tenantid    UNIQUEIDENTIFIER,
    @admissionid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.admissions a
        WHERE a.admissionid = @admissionid
          AND a.tenantid = @tenantid)
    BEGIN
        THROW 50401, 'Admission not found.', 1;
    END;

    SELECT
        a.admissionid,
        a.admissioncode,
        a.admittedat,
        a.dischargedat,
        a.ward,
        a.bed,
        a.roomclass,
        a.admissionstatus,
        a.patientid,
        pt.patientcode,
        pt.firstname AS patientfirstname,
        pt.lastname AS patientlastname,
        pt.age AS patientage,
        pt.gender AS patientgender,
        pt.phonecipher AS patientphonecipher,
        pt.addresscipher AS patientaddresscipher,
        a.attendingdoctorid,
        doc.firstname AS doctorfirstname,
        doc.lastname AS doctorlastname,
        doc.designation AS doctordesignation,
        dep.departmentid,
        dep.name AS departmentname,
        t.hospitalname,
        t.tenantaddress AS hospitaladdress,
        t.primarycontactphone AS hospitalphone,
        t.logourl AS hospitallogourl,
        t.website AS hospitalwebsite,
        ds.dischargesummaryid,
        ds.dateofsurgery,
        ds.dateofdischarge,
        ds.finaldiagnosis,
        ds.diagnosiskey,
        ds.formschemaversion,
        ds.formjson,
        ds.createdat AS summarycreatedat,
        ds.updatedat AS summaryupdatedat
    FROM dbo.admissions a
    INNER JOIN dbo.patients pt
        ON pt.patientid = a.patientid
       AND pt.tenantid = a.tenantid
       AND pt.isdeleted = 0
    INNER JOIN dbo.users doc
        ON doc.userid = a.attendingdoctorid
       AND doc.tenantid = a.tenantid
    INNER JOIN dbo.tenants t
        ON t.tenantid = a.tenantid
       AND t.isdeleted = 0
    LEFT JOIN dbo.departments dep
        ON dep.departmentid = a.departmentid
       AND dep.tenantid = a.tenantid
    LEFT JOIN dbo.discharge_summaries ds
        ON ds.admissionid = a.admissionid
       AND ds.tenantid = a.tenantid
    WHERE a.admissionid = @admissionid
      AND a.tenantid = @tenantid;
END
GO
