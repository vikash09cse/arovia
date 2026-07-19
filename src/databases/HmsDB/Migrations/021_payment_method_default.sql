-- Default payment method to Cash (1) for collections
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF COL_LENGTH(N'dbo.payments', N'paymentmethod') IS NOT NULL
BEGIN
    UPDATE dbo.payments
    SET paymentmethod = 1
    WHERE paymentmethod IS NULL
      AND paymentlinetype = 3;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.payments')
          AND c.name = N'paymentmethod'
    )
    BEGIN
        ALTER TABLE dbo.payments
            ADD CONSTRAINT DF_payments_paymentmethod DEFAULT (1) FOR paymentmethod;
    END
END
GO
