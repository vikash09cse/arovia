CREATE OR ALTER PROCEDURE dbo.sp_visit_override_fee
    @tenantid          UNIQUEIDENTIFIER,
    @visitid           UNIQUEIDENTIFIER,
    @feestatus         TINYINT,
    @feeoverridereason NVARCHAR(500),
    @actorid           UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @feestatus NOT IN (1, 2)
        THROW 50400, 'Invalid fee status.', 1;

    IF @feeoverridereason IS NULL OR LTRIM(RTRIM(@feeoverridereason)) = ''
        THROW 50400, 'Fee override reason is required.', 1;

    BEGIN TRANSACTION;

    DECLARE @patientid UNIQUEIDENTIFIER;
    DECLARE @oldfeestatus TINYINT;
    DECLARE @visitfeeamount DECIMAL(18, 2);

    SELECT
        @patientid = v.patientid,
        @oldfeestatus = v.feestatus
    FROM dbo.visits v
    WHERE v.visitid = @visitid
      AND v.tenantid = @tenantid
      AND v.visitstatus = 1;

    IF @patientid IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50404, 'Visit not found or cancelled.', 1;
    END

    SELECT @visitfeeamount = ts.visitfeeamount
    FROM dbo.tenant_settings ts
    WHERE ts.tenantid = @tenantid;

    IF @visitfeeamount IS NULL
        SET @visitfeeamount = 0;

    UPDATE dbo.visits
    SET feestatus = @feestatus,
        feeamount = CASE WHEN @feestatus = 1 THEN @visitfeeamount ELSE NULL END,
        totalchargeamount = CASE
            WHEN @feestatus = 1 THEN @visitfeeamount + ISNULL(procedurechargeamount, 0)
                + ISNULL((SELECT SUM(l.amount) FROM dbo.visit_addon_lines l WHERE l.visitid = @visitid AND l.tenantid = @tenantid), 0)
                - ISNULL(discountamount, 0)
            ELSE ISNULL(procedurechargeamount, 0)
                + ISNULL((SELECT SUM(l.amount) FROM dbo.visit_addon_lines l WHERE l.visitid = @visitid AND l.tenantid = @tenantid), 0)
                - ISNULL(discountamount, 0)
        END,
        isfeeoverridden = 1,
        feeoverridereason = @feeoverridereason,
        updatedby = @actorid,
        updatedat = SYSUTCDATETIME()
    WHERE visitid = @visitid
      AND tenantid = @tenantid;

    IF @feestatus = 1 AND @oldfeestatus = 2
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM dbo.payments
            WHERE tenantid = @tenantid AND visitid = @visitid AND paymentlinetype = 1 AND paymentstatus <> 3)
        BEGIN
            INSERT INTO dbo.payments (
                paymentid, tenantid, visitid, patientid, paymentlinetype, feeamount, paymentstatus,
                createdby, updatedby)
            VALUES (
                NEWID(), @tenantid, @visitid, @patientid, 1, @visitfeeamount, 1,
                @actorid, @actorid);
        END
    END
    ELSE IF @feestatus = 2 AND @oldfeestatus = 1
    BEGIN
        UPDATE dbo.payments
        SET paymentstatus = 3,
            updatedby = @actorid,
            updatedat = SYSUTCDATETIME()
        WHERE tenantid = @tenantid
          AND visitid = @visitid
          AND paymentlinetype = 1
          AND paymentstatus = 1;
    END

    UPDATE dbo.visits
    SET totalchargeamount = NULLIF(
        ISNULL(feeamount, 0) + ISNULL(procedurechargeamount, 0)
        + ISNULL((SELECT SUM(l.amount) FROM dbo.visit_addon_lines l WHERE l.visitid = @visitid AND l.tenantid = @tenantid), 0)
        - ISNULL(discountamount, 0), 0)
    WHERE visitid = @visitid
      AND tenantid = @tenantid;

    COMMIT TRANSACTION;
END
GO
