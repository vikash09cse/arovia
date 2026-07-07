CREATE OR ALTER PROCEDURE dbo.sp_visit_save

    @tenantid                   UNIQUEIDENTIFIER,

    @patientid                  UNIQUEIDENTIFIER,

    @consultingdoctorid         UNIQUEIDENTIFIER,

    @visittype                  TINYINT,

    @purpose                    NVARCHAR(300),

    @visitnotes                 NVARCHAR(1000) = NULL,

    @procedurechargeamount      DECIMAL(18, 2) = NULL,

    @scheduledsurgerydate       DATE = NULL,

    @overridefeestatus          TINYINT,

    @consultationfeeamount      DECIMAL(18, 2) = NULL,

    @feeoverridereason          NVARCHAR(500) = NULL,

    @initialcollectionamount    DECIMAL(18, 2) = NULL,

    @collectedby                UNIQUEIDENTIFIER = NULL,

    @actorid                    UNIQUEIDENTIFIER,

    @addonids                   NVARCHAR(MAX) = NULL,

    @discountamount             DECIMAL(18, 2) = NULL,

    @discountreason             NVARCHAR(500) = NULL

AS

BEGIN

    SET NOCOUNT ON;

    SET XACT_ABORT ON;



    IF NOT EXISTS (

        SELECT 1 FROM dbo.patients p

        WHERE p.patientid = @patientid

          AND p.tenantid = @tenantid

          AND p.isdeleted = 0

          AND p.patientstatus = 1)

    BEGIN

        THROW 50404, 'Patient not found or inactive.', 1;

    END



    IF NOT EXISTS (

        SELECT 1 FROM dbo.users u

        WHERE u.userid = @consultingdoctorid

          AND u.tenantid = @tenantid

          AND u.usertype = 3

          AND u.userstatus = 1

          AND u.isdeleted = 0)

    BEGIN

        THROW 50400, 'Invalid consulting doctor.', 1;

    END



    IF @procedurechargeamount IS NOT NULL AND @procedurechargeamount < 0

    BEGIN

        THROW 50400, 'Procedure charge cannot be negative.', 1;

    END



    IF @initialcollectionamount IS NOT NULL AND @initialcollectionamount <= 0

    BEGIN

        THROW 50400, 'Initial collection amount must be greater than zero.', 1;

    END



    DECLARE @collector UNIQUEIDENTIFIER = NULL;

    IF @initialcollectionamount IS NOT NULL

    BEGIN

        SET @collector = ISNULL(@collectedby, @actorid);



        IF NOT EXISTS (

            SELECT 1 FROM dbo.users u

            WHERE u.userid = @collector

              AND u.tenantid = @tenantid

              AND u.usertype IN (1, 2)

              AND u.userstatus = 1

              AND u.isdeleted = 0)

        BEGIN

            THROW 50400, 'Invalid payment collector.', 1;

        END

    END



    BEGIN TRANSACTION;



    DECLARE @visitid UNIQUEIDENTIFIER = NEWID();

    DECLARE @visitfeeamount DECIMAL(18, 2);

    DECLARE @freevisitwindowdays INT;

    DECLARE @timezone NVARCHAR(50);

    DECLARE @feestatus TINYINT;

    DECLARE @isfeeoverridden BIT = 0;

    DECLARE @lastchargedvisitdatetime DATETIME2;

    DECLARE @dayssince INT = NULL;

    DECLARE @visitUtc DATETIME2 = SYSUTCDATETIME();

    DECLARE @feeamount DECIMAL(18, 2) = NULL;

    DECLARE @totalchargeamount DECIMAL(18, 2) = NULL;

    DECLARE @addonsum DECIMAL(18, 2) = 0;

    DECLARE @subtotal DECIMAL(18, 2);

    DECLARE @seq INT;

    DECLARE @visitcode NVARCHAR(20);



    SELECT

        @visitfeeamount = ts.visitfeeamount,

        @freevisitwindowdays = ts.freevisitwindowdays

    FROM dbo.tenant_settings ts

    WHERE ts.tenantid = @tenantid;



    IF @visitfeeamount IS NULL

    BEGIN

        SET @visitfeeamount = 0;

        SET @freevisitwindowdays = 10;

    END



    SELECT @timezone = t.timezone

    FROM dbo.tenants t

    WHERE t.tenantid = @tenantid;



    IF @timezone IS NULL OR LTRIM(RTRIM(@timezone)) = ''

        SET @timezone = N'UTC';



    SET @timezone = dbo.fn_to_sql_timezone(@timezone);



    SELECT TOP 1 @lastchargedvisitdatetime = v.visitdatetime

    FROM dbo.visits v

    WHERE v.tenantid = @tenantid

      AND v.patientid = @patientid

      AND v.visitstatus = 1

      AND v.feestatus = 1

    ORDER BY v.visitdatetime DESC;



    IF @lastchargedvisitdatetime IS NULL

        SET @feestatus = 1;

    ELSE

    BEGIN

        DECLARE @lastLocalDate DATE = CAST(

            (@lastchargedvisitdatetime AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE);

        DECLARE @visitLocalDate DATE = CAST(

            (@visitUtc AT TIME ZONE 'UTC' AT TIME ZONE @timezone) AS DATE);



        SET @dayssince = DATEDIFF(day, @lastLocalDate, @visitLocalDate);



        IF @dayssince <= @freevisitwindowdays

            SET @feestatus = 2;

        ELSE

            SET @feestatus = 1;

    END



    IF @overridefeestatus NOT IN (1, 2)

    BEGIN

        ROLLBACK TRANSACTION;

        THROW 50400, 'Invalid consultation fee status.', 1;

    END



    DECLARE @autofeestatus TINYINT = @feestatus;

    DECLARE @autofeeamount DECIMAL(18, 2) = CASE WHEN @autofeestatus = 1 THEN @visitfeeamount ELSE NULL END;



    SET @feestatus = @overridefeestatus;



    IF @feestatus = 1

    BEGIN

        IF @consultationfeeamount IS NULL OR @consultationfeeamount < 0

        BEGIN

            ROLLBACK TRANSACTION;

            THROW 50400, 'Consultation fee amount is required when charged.', 1;

        END

        SET @feeamount = @consultationfeeamount;

    END

    ELSE

        SET @feeamount = NULL;



    SET @isfeeoverridden = CASE

        WHEN @feestatus <> @autofeestatus THEN 1

        WHEN @feestatus = 1 AND @feeamount <> ISNULL(@autofeeamount, 0) THEN 1

        WHEN @feeoverridereason IS NOT NULL AND LTRIM(RTRIM(@feeoverridereason)) <> '' THEN 1

        ELSE 0

    END;



    IF @addonids IS NOT NULL AND LTRIM(RTRIM(@addonids)) <> '' AND ISJSON(@addonids) = 1
    BEGIN
        DECLARE @addonRequestCount INT;
        DECLARE @addonMatchCount INT;

        SELECT @addonRequestCount = COUNT(*)
        FROM OPENJSON(@addonids) j
        WHERE TRY_CAST(j.[value] AS UNIQUEIDENTIFIER) IS NOT NULL;

        IF @addonRequestCount = 0 OR @addonRequestCount <> (SELECT COUNT(*) FROM OPENJSON(@addonids))
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50400, 'Invalid visit addon selection.', 1;
        END

        SELECT @addonMatchCount = COUNT(DISTINCT c.visitaddonid)
        FROM OPENJSON(@addonids) j
        INNER JOIN dbo.visit_addon_catalog c
            ON c.visitaddonid = TRY_CAST(j.[value] AS UNIQUEIDENTIFIER)
        WHERE c.tenantid = @tenantid
          AND c.addonstatus = 1;

        IF @addonMatchCount <> @addonRequestCount
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50400, 'One or more selected visit addons are invalid or inactive.', 1;
        END

        SELECT @addonsum = ISNULL(SUM(c.defaultamount), 0)
        FROM OPENJSON(@addonids) j
        INNER JOIN dbo.visit_addon_catalog c
            ON c.visitaddonid = TRY_CAST(j.[value] AS UNIQUEIDENTIFIER)
        WHERE c.tenantid = @tenantid
          AND c.addonstatus = 1;
    END

    SET @subtotal = ISNULL(@feeamount, 0) + ISNULL(@procedurechargeamount, 0) + ISNULL(@addonsum, 0);

    IF @discountamount IS NOT NULL AND @discountamount < 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50400, 'Invalid discount amount.', 1;
    END

    IF @discountamount IS NULL OR @discountamount = 0
    BEGIN
        SET @discountamount = NULL;
        SET @discountreason = NULL;
    END
    ELSE
    BEGIN
        IF @discountreason IS NULL OR LTRIM(RTRIM(@discountreason)) = ''
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50400, 'Discount reason is required when applying a discount.', 1;
        END

        IF @discountamount > @subtotal
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50400, 'Discount amount cannot exceed the visit subtotal.', 1;
        END

        SET @discountreason = LTRIM(RTRIM(@discountreason));
    END

    SET @totalchargeamount = @subtotal - ISNULL(@discountamount, 0);

    IF @totalchargeamount = 0

        SET @totalchargeamount = NULL;



    IF @initialcollectionamount IS NOT NULL AND @totalchargeamount IS NULL

    BEGIN

        ROLLBACK TRANSACTION;

        THROW 50400, 'Cannot collect payment when visit has no charges.', 1;

    END



    IF @initialcollectionamount IS NOT NULL AND @initialcollectionamount > ISNULL(@totalchargeamount, 0)

    BEGIN

        ROLLBACK TRANSACTION;

        THROW 50400, 'Initial collection amount exceeds the visit total.', 1;

    END



    IF NOT EXISTS (SELECT 1 FROM dbo.visit_sequences WITH (UPDLOCK, ROWLOCK) WHERE tenantid = @tenantid)

    BEGIN

        INSERT INTO dbo.visit_sequences (tenantid, nextsequencenumber)

        VALUES (@tenantid, 2);

        SET @seq = 1;

    END

    ELSE

    BEGIN

        SELECT @seq = nextsequencenumber

        FROM dbo.visit_sequences WITH (UPDLOCK, ROWLOCK)

        WHERE tenantid = @tenantid;



        UPDATE dbo.visit_sequences

        SET nextsequencenumber = @seq + 1,

            updatedat = SYSUTCDATETIME()

        WHERE tenantid = @tenantid;

    END



    SET @visitcode = N'VIS-' + RIGHT(REPLICATE(N'0', 7) + CAST(@seq AS NVARCHAR(10)), 7);



    INSERT INTO dbo.visits (

        visitid, tenantid, patientid, visitcode, sequencenumber,

        consultingdoctorid, visitdatetime, visittype, purpose, visitnotes, scheduledsurgerydate,

        feestatus, feeamount, procedurechargeamount, totalchargeamount,

        discountamount, discountreason,

        visitstatus, isfeeoverridden, feeoverridereason,

        freevisitwindowdayssnapshot, dayssincelastcharged, lastchargedvisitdatetime,

        createdby, updatedby)

    VALUES (

        @visitid, @tenantid, @patientid, @visitcode, @seq,

        @consultingdoctorid, @visitUtc, @visittype, @purpose, @visitnotes, @scheduledsurgerydate,

        @feestatus, @feeamount, @procedurechargeamount, @totalchargeamount,

        @discountamount, @discountreason,

        1, @isfeeoverridden, @feeoverridereason,

        @freevisitwindowdays, @dayssince, @lastchargedvisitdatetime,

        @actorid, @actorid);



    IF @addonids IS NOT NULL AND LTRIM(RTRIM(@addonids)) <> '' AND ISJSON(@addonids) = 1
    BEGIN
        INSERT INTO dbo.visit_addon_lines (
            visitaddonlineid, tenantid, visitid, visitaddonid, addonname, amount, createdby)
        SELECT
            NEWID(), @tenantid, @visitid, c.visitaddonid, c.name, c.defaultamount, @actorid
        FROM OPENJSON(@addonids) j
        INNER JOIN dbo.visit_addon_catalog c
            ON c.visitaddonid = TRY_CAST(j.[value] AS UNIQUEIDENTIFIER)
        WHERE c.tenantid = @tenantid
          AND c.addonstatus = 1;
    END

    SELECT @visitid AS visitid;



    IF @initialcollectionamount IS NOT NULL

    BEGIN

        EXEC dbo.sp_payment_add_collection

            @tenantid = @tenantid,

            @visitid = @visitid,

            @amount = @initialcollectionamount,

            @collectedby = @collector,

            @notes = NULL,

            @paymentmethod = NULL,

            @actorid = @actorid;

    END



    COMMIT TRANSACTION;

END

GO


