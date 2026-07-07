CREATE OR ALTER PROCEDURE dbo.sp_get_patients
    @tenantid       UNIQUEIDENTIFIER,
    @page           INT = 1,
    @pagesize       INT = 10,
    @patientcode        NVARCHAR(20) = NULL,
    @phoneblindindex    BINARY(32) = NULL,
    @patientstatus      TINYINT = NULL,
    @gender         TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page - 1) * @pagesize;

    SELECT
        p.patientid,
        p.tenantid,
        p.patientcode,
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
        p.updatedat,
        COUNT(*) OVER() AS totalcount
    FROM dbo.patients p
    WHERE p.tenantid = @tenantid
      AND p.isdeleted = 0
      AND (@patientcode IS NULL OR p.patientcode = @patientcode)
      AND (@phoneblindindex IS NULL OR p.phoneblindindex = @phoneblindindex)
      AND (@patientstatus IS NULL OR p.patientstatus = @patientstatus)
      AND (@gender IS NULL OR p.gender = @gender)
    ORDER BY p.createdat DESC
    OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY;
END
GO
