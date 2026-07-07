CREATE OR ALTER PROCEDURE dbo.sp_get_patient_by_id
    @tenantid   UNIQUEIDENTIFIER,
    @patientid  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.patientid,
        p.tenantid,
        p.patientcode,
        p.sequencenumber,
        p.firstname,
        p.lastname,
        p.dateofbirth,
        p.age,
        p.gender,
        p.bloodgroup,
        p.referredby,
        p.patientstatus,
        p.phonecipher,
        p.emailcipher,
        p.addresscipher,
        p.emergencynamecipher,
        p.emergencyphonecipher,
        p.registeredby,
        p.createdat,
        p.updatedat
    FROM dbo.patients p
    WHERE p.tenantid = @tenantid
      AND p.patientid = @patientid
      AND p.isdeleted = 0;
END
GO
