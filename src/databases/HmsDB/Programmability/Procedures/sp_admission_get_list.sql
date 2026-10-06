CREATE OR ALTER PROCEDURE dbo.sp_admission_get_list
    @tenantid            UNIQUEIDENTIFIER,
    @page                INT = 1,
    @pagesize            INT = 10,
    @admissionstatus     TINYINT = NULL,
    @patientid           UNIQUEIDENTIFIER = NULL,
    @admissioncode       NVARCHAR(30) = NULL,
    @datefrom            DATE = NULL,
    @dateto              DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page - 1) * @pagesize;

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
        a.departmentid,
        dep.name AS departmentname,
        a.patientid,
        pt.patientcode,
        pt.firstname AS patientfirstname,
        pt.lastname AS patientlastname,
        a.attendingdoctorid,
        d.firstname AS doctorfirstname,
        d.lastname AS doctorlastname,
        COUNT(*) OVER() AS totalcount
    FROM dbo.admissions a
    INNER JOIN dbo.patients pt
        ON pt.patientid = a.patientid
       AND pt.tenantid = a.tenantid
       AND pt.isdeleted = 0
    INNER JOIN dbo.departments dep
        ON dep.departmentid = a.departmentid
       AND dep.tenantid = a.tenantid
    INNER JOIN dbo.users d
        ON d.userid = a.attendingdoctorid
    WHERE a.tenantid = @tenantid
      AND a.isdeleted = 0
      AND (@admissionstatus IS NULL OR a.admissionstatus = @admissionstatus)
      AND (@patientid IS NULL OR a.patientid = @patientid)
      AND (@admissioncode IS NULL OR a.admissioncode = @admissioncode)
      AND (@datefrom IS NULL OR CAST(a.admittedat AS DATE) >= @datefrom)
      AND (@dateto IS NULL OR CAST(a.admittedat AS DATE) <= @dateto)
    ORDER BY a.admittedat DESC
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
