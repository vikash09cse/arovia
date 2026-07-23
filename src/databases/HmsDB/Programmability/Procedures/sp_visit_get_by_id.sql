CREATE OR ALTER PROCEDURE dbo.sp_visit_get_by_id

    @tenantid UNIQUEIDENTIFIER,

    @visitid  UNIQUEIDENTIFIER

AS

BEGIN

    SET NOCOUNT ON;



    SELECT

        v.visitid,

        v.visitcode,

        v.visitdatetime,

        v.visittype,

        v.purpose,

        v.visitnotes,

        v.scheduledsurgerydate,

        v.feestatus,

        v.feeamount,

        v.procedurechargeamount,

        v.discountamount,

        v.discountreason,

        v.totalchargeamount,

        v.visitstatus,

        v.isfeeoverridden,

        v.feeoverridereason,

        v.cancellationreason,

        v.freevisitwindowdayssnapshot,

        v.dayssincelastcharged,

        v.lastchargedvisitdatetime,

        v.createdat,

        p.patientid,

        p.patientcode,

        p.firstname AS patientfirstname,

        p.lastname AS patientlastname,

        d.userid AS consultingdoctorid,

        d.firstname AS doctorfirstname,

        d.lastname AS doctorlastname,

        ISNULL(v.totalchargeamount, 0) AS totaldue,

        ISNULL(col.totalcollected, 0) AS totalcollected,

        CASE

            WHEN v.totalchargeamount IS NULL OR v.totalchargeamount = 0 THEN 0

            ELSE v.totalchargeamount - ISNULL(col.totalcollected, 0)

        END AS balancedue

    FROM dbo.visits v

    INNER JOIN dbo.patients p
        ON p.patientid = v.patientid
       AND p.tenantid = v.tenantid
       AND p.isdeleted = 0

    INNER JOIN dbo.users d ON d.userid = v.consultingdoctorid

    OUTER APPLY (

        SELECT SUM(pay.amountpaid) AS totalcollected

        FROM dbo.payments pay

        WHERE pay.tenantid = v.tenantid

          AND pay.visitid = v.visitid

          AND pay.paymentstatus = 2

    ) col

    WHERE v.tenantid = @tenantid

      AND v.visitid = @visitid

      AND v.isdeleted = 0;



    SELECT

        pay.paymentid,

        pay.paymentlinetype,

        pay.feeamount,

        pay.paymentstatus,

        pay.receiptnumber,

        pay.amountpaid,

        pay.paymentmethod,

        pay.collectiondatetime,

        pay.notes,

        pay.collectedby AS collectedbyuserid,

        c.firstname AS collectorfirstname,

        c.lastname AS collectorlastname

    FROM dbo.payments pay

    LEFT JOIN dbo.users c ON c.userid = pay.collectedby

    WHERE pay.tenantid = @tenantid

      AND pay.visitid = @visitid

      AND pay.paymentstatus = 2

    ORDER BY pay.collectiondatetime, pay.createdat;



    SELECT

        vla.visitlabagencyid,

        vla.labagencyid,

        la.name AS agencyname,

        vla.assignedat,

        vla.assignedby AS assignedbyuserid,

        u.firstname AS assignerfirstname,

        u.lastname AS assignerlastname,

        vla.testname,

        vla.notes

    FROM dbo.visit_lab_agencies vla

    INNER JOIN dbo.lab_agencies la ON la.labagencyid = vla.labagencyid AND la.tenantid = vla.tenantid

    INNER JOIN dbo.users u ON u.userid = vla.assignedby

    WHERE vla.tenantid = @tenantid

      AND vla.visitid = @visitid

    ORDER BY vla.assignedat, vla.visitlabagencyid;

    SELECT
        val.visitaddonlineid,
        val.visitaddonid,
        val.addonname,
        val.amount,
        val.createdat
    FROM dbo.visit_addon_lines val
    WHERE val.tenantid = @tenantid
      AND val.visitid = @visitid
    ORDER BY val.addonname, val.visitaddonlineid;

END

GO


