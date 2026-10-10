-- Larger logo (+20%), space before hospital name, ₹ on amounts (right-aligned).

DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @invoiceId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

UPDATE dbo.global_document_templates
SET bodyhtml = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        bodyhtml,
        N'  .letterhead {
    display: grid;
    grid-template-columns: 78px 1fr 132px;
    gap: 12px;
    align-items: center;
    margin-bottom: 10px;
  }
  .logo, .cdc-logo {
    width: 74px; height: 74px; object-fit: contain;
    display: block;
  }
  .logo-fallback, .cdc-logo-fallback {
    width: 74px; height: 74px; border-radius: 6px;
    border: 2px solid #7CB342; display: flex; align-items: center; justify-content: center;
    color: #7CB342; font-weight: 800; font-size: 20px; background: #f7fbe9;
  }
  .brand-center { padding-top: 0; text-align: left; }',
        N'  .letterhead {
    display: grid;
    grid-template-columns: 94px 1fr 132px;
    gap: 8px;
    align-items: center;
    margin-bottom: 10px;
  }
  .logo, .cdc-logo {
    width: 89px; height: 89px; object-fit: contain;
    display: block;
  }
  .logo-fallback, .cdc-logo-fallback {
    width: 89px; height: 89px; border-radius: 6px;
    border: 2px solid #7CB342; display: flex; align-items: center; justify-content: center;
    color: #7CB342; font-weight: 800; font-size: 22px; background: #f7fbe9;
  }
  .brand-center { padding-top: 0; padding-left: 26px; text-align: left; }'),
        N'  table.charges th.amt, table.charges td.amt { text-align: right; white-space: nowrap; }',
        N'  table.charges th.amt, table.charges td.amt {
    text-align: right !important; white-space: nowrap; font-variant-numeric: tabular-nums;
  }'),
        N'  .payable-top { display: flex; justify-content: space-between; font-size: 10px; font-weight: 700; letter-spacing: 0.3px; }
  .payable-amt { margin-top: 3px; font-size: 20px; font-weight: 700; text-align: center; letter-spacing: 0.4px; }',
        N'  .payable-top { font-size: 10px; font-weight: 700; letter-spacing: 0.3px; text-align: left; }
  .payable-amt {
    margin-top: 4px; font-size: 20px; font-weight: 700; text-align: right;
    letter-spacing: 0.4px; font-variant-numeric: tabular-nums;
  }'),
        N'    <div class="payable-box">
      <div class="payable-top"><span>TOTAL AMOUNT PAYABLE</span><span>₹</span></div>
      <div class="payable-amt">{{PayableAmount}}</div>
    </div>',
        N'    <div class="payable-box">
      <div class="payable-top">TOTAL AMOUNT PAYABLE</div>
      <div class="payable-amt">₹ {{PayableAmount}}</div>
    </div>'),
        N'    <div class="gross">GROSS TOTAL &nbsp;&nbsp; ₹ {{GrossTotal}}</div>',
        N'    <div class="gross">GROSS TOTAL &nbsp;&nbsp; ₹{{GrossTotal}}</div>'),
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
WHERE globaldocumenttemplateid = @invoiceId
  AND isdeleted = 0;

-- Also match logo sizes if already updated by 047-style columns (78px path already covered).
-- Fallback: older 72/68 logo block from earlier templates.
UPDATE dbo.global_document_templates
SET bodyhtml = REPLACE(REPLACE(
        bodyhtml,
        N'grid-template-columns: 72px 1fr 140px;',
        N'grid-template-columns: 94px 1fr 132px;'),
        N'width: 68px; height: 68px;',
        N'width: 89px; height: 89px;'),
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
WHERE globaldocumenttemplateid = @invoiceId
  AND isdeleted = 0
  AND bodyhtml LIKE N'%width: 68px; height: 68px;%';

UPDATE dt
SET bodyhtml = g.bodyhtml,
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
FROM dbo.document_templates dt
INNER JOIN dbo.global_document_templates g ON g.globaldocumenttemplateid = dt.globaldocumenttemplateid
WHERE g.globaldocumenttemplateid = @invoiceId
  AND dt.isdeleted = 0;
GO
