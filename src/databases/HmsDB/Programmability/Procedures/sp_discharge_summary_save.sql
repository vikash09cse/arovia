CREATE OR ALTER PROCEDURE dbo.sp_discharge_summary_save
    @tenantid           UNIQUEIDENTIFIER,
    @admissionid        UNIQUEIDENTIFIER,
    @dateofsurgery      DATE             = NULL,
    @dateofdischarge    DATE             = NULL,
    @finaldiagnosis     NVARCHAR(500)    = NULL,
    @diagnosiskey       NVARCHAR(100)    = NULL,
    @formschemaversion  INT              = 1,
    @formjson           NVARCHAR(MAX),
    @actorid            UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.admissions a
        WHERE a.admissionid = @admissionid
          AND a.tenantid = @tenantid
          AND a.isdeleted = 0)
    BEGIN
        THROW 50401, 'Admission not found.', 1;
    END;

    IF @formjson IS NULL OR LTRIM(RTRIM(@formjson)) = N'' OR ISJSON(@formjson) <> 1
        THROW 50402, 'formjson must be valid JSON.', 1;

    IF @formschemaversion IS NULL OR @formschemaversion < 1
        THROW 50403, 'formschemaversion must be >= 1.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.users u
        WHERE u.userid = @actorid
          AND u.tenantid = @tenantid
          AND u.isdeleted = 0)
        THROW 50404, 'Actor user not found.', 1;

    DECLARE @dischargesummaryid UNIQUEIDENTIFIER;

    BEGIN TRAN;

    SELECT @dischargesummaryid = ds.dischargesummaryid
    FROM dbo.discharge_summaries ds WITH (UPDLOCK, HOLDLOCK)
    WHERE ds.tenantid = @tenantid
      AND ds.admissionid = @admissionid;

    IF @dischargesummaryid IS NULL
    BEGIN
        SET @dischargesummaryid = NEWID();

        INSERT INTO dbo.discharge_summaries (
            dischargesummaryid, tenantid, admissionid,
            dateofsurgery, dateofdischarge, finaldiagnosis, diagnosiskey,
            formschemaversion, formjson,
            createdby, updatedby)
        VALUES (
            @dischargesummaryid, @tenantid, @admissionid,
            @dateofsurgery, @dateofdischarge, @finaldiagnosis, @diagnosiskey,
            @formschemaversion, @formjson,
            @actorid, @actorid);
    END
    ELSE
    BEGIN
        UPDATE dbo.discharge_summaries
        SET dateofsurgery = @dateofsurgery,
            dateofdischarge = @dateofdischarge,
            finaldiagnosis = @finaldiagnosis,
            diagnosiskey = @diagnosiskey,
            formschemaversion = @formschemaversion,
            formjson = @formjson,
            updatedby = @actorid,
            updatedat = SYSUTCDATETIME()
        WHERE dischargesummaryid = @dischargesummaryid
          AND tenantid = @tenantid;
    END;

    COMMIT TRAN;

    SELECT @dischargesummaryid AS dischargesummaryid;
END
GO
