-- Evolve payments to collection (installment) model

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.payments') AND name = 'notes')
BEGIN
    ALTER TABLE dbo.payments ADD notes NVARCHAR(500) NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_payments_tenant_visit_linetype_active'
      AND object_id = OBJECT_ID('dbo.payments'))
BEGIN
    DROP INDEX UQ_payments_tenant_visit_linetype_active ON dbo.payments;
END
GO

-- Remove pending charge-line rows; balance is derived from visits.totalchargeamount
DELETE FROM dbo.payments WHERE paymentstatus = 1;
GO

-- Normalize paid rows as collections
UPDATE dbo.payments
SET amountpaid = feeamount,
    paymentstatus = 2
WHERE paymentstatus = 2
  AND amountpaid IS NULL;
GO
