-- Refresh invoice totals / Notes / Payment Details to match sample Bill of Supply layout.

DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @invoiceId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
DECLARE @html NVARCHAR(MAX);

SELECT @html = bodyhtml
FROM dbo.global_document_templates
WHERE globaldocumenttemplateid = @invoiceId
  AND isdeleted = 0;

IF @html IS NULL
BEGIN
    RAISERROR(N'Invoice global document template not found.', 16, 1);
END

SET @html = REPLACE(@html,
    N'.payable-top { display: flex; justify-content: space-between; font-size: 11px; font-weight: 700; letter-spacing: 0.4px; }
  .payable-amt { margin-top: 4px; font-size: 22px; font-weight: 700; text-align: right; letter-spacing: 0.5px; }',
    N'.payable-top { text-align: center; font-size: 11px; font-weight: 700; letter-spacing: 0.4px; }
  .payable-amt { margin-top: 4px; font-size: 22px; font-weight: 700; text-align: center; letter-spacing: 0.5px; }');

SET @html = REPLACE(@html,
    N'.lower {
    margin-top: 16px; display: grid; grid-template-columns: 1.2fr 1fr auto; gap: 16px; align-items: start;
  }
  .notes-title { font-weight: 700; margin-bottom: 4px; }
  .notes ul { margin: 0; padding-left: 16px; color: #475569; font-size: 10.5px; }
  .pay-block .pay-row { display: grid; grid-template-columns: 100px 10px 1fr; gap: 4px; margin-bottom: 4px; }
  .paid-stamp {
    border: 3px solid #16a34a; color: #16a34a; border-radius: 8px;
    width: 88px; height: 66px; display: flex; flex-direction: column;
    align-items: center; justify-content: center; font-weight: 800;
    transform: rotate(-12deg); opacity: 0.92;
  }
  .paid-stamp .mark { font-size: 22px; line-height: 1; }
  .paid-stamp .label { font-size: 14px; letter-spacing: 1px; }
  .paid-stamp.hidden { visibility: hidden; }',
    N'.lower {
    margin-top: 16px; border: 1px solid #1e3a6e; display: grid;
    grid-template-columns: 1fr 1fr; align-items: stretch;
  }
  .notes { padding: 10px 12px; }
  .notes-title { font-weight: 700; margin-bottom: 6px; color: #1e3a6e; }
  .notes ul { margin: 0; padding-left: 16px; color: #111; font-size: 10.5px; font-family: Georgia, serif; }
  .notes li { margin-bottom: 2px; }
  .pay-block {
    border-left: 1px solid #1e3a6e; padding: 10px 12px; position: relative; min-height: 110px;
  }
  .pay-title { font-weight: 700; margin-bottom: 8px; color: #1e3a6e; }
  .pay-block .pay-row { display: grid; grid-template-columns: 100px 10px 1fr; gap: 4px; margin-bottom: 4px; }
  .pay-block .pay-row .meta-value.amt { font-weight: 700; }
  .paid-stamp {
    position: absolute; right: 12px; bottom: 10px;
    color: #16a34a; font-weight: 800; font-size: 13px; letter-spacing: 0.5px;
    display: flex; align-items: center; gap: 4px;
  }
  .paid-stamp .mark { font-size: 14px; line-height: 1; }
  .paid-stamp .label { font-size: 13px; }
  .paid-stamp.hidden { display: none; }');

SET @html = REPLACE(@html,
    N'    <div class="payable-box">
      <div class="payable-top"><span>TOTAL AMOUNT PAYABLE</span><span>₹</span></div>
      <div class="payable-amt">{{PayableAmount}}</div>
    </div>',
    N'    <div class="payable-box">
      <div class="payable-top">TOTAL AMOUNT PAYABLE</div>
      <div class="payable-amt">₹ {{PayableAmount}}</div>
    </div>');

SET @html = REPLACE(@html,
    N'  <div class="lower">
    <div class="notes">
      <div class="notes-title">Notes :</div>
      <ul>
        <li>This bill is computer generated and valid without signature if paid.</li>
        <li>Please retain this bill for future reference and insurance claims.</li>
        <li>Room / package charges are as per hospital policy for the stay period.</li>
        <li>In case of emergency after discharge, contact hospital reception immediately.</li>
      </ul>
    </div>
    <div class="pay-block">
      <div class="pay-row"><span class="meta-label">Amount Paid</span><span class="meta-colon">:</span><span class="meta-value">₹{{AmountPaid}}</span></div>
      <div class="pay-row"><span class="meta-label">Payment Mode</span><span class="meta-colon">:</span><span class="meta-value">{{PaymentMode}}</span></div>
      <div class="pay-row"><span class="meta-label">Payment Date</span><span class="meta-colon">:</span><span class="meta-value">{{PaymentDate}}</span></div>
    </div>
    <div class="paid-stamp {{PaidStampClass}}"><div class="mark">✓</div><div class="label">PAID</div></div>
  </div>',
    N'  <div class="lower">
    <div class="notes">
      <div class="notes-title">Notes :</div>
      <ul>
        <li>This is a package bill for the procedure including OPD to Discharge.</li>
        <li>Bill is valid subject to full &amp; final payment.</li>
        <li>No further charges will be applicable under this package.</li>
        <li>In case of any emergency re-admission, standard charges will apply.</li>
      </ul>
    </div>
    <div class="pay-block">
      <div class="pay-title">Payment Details</div>
      <div class="pay-row"><span class="meta-label">Amount Paid</span><span class="meta-colon">:</span><span class="meta-value amt">₹ {{AmountPaid}}</span></div>
      <div class="pay-row"><span class="meta-label">Payment Mode</span><span class="meta-colon">:</span><span class="meta-value">{{PaymentMode}}</span></div>
      <div class="pay-row"><span class="meta-label">Payment Date</span><span class="meta-colon">:</span><span class="meta-value">{{PaymentDate}}</span></div>
      <div class="paid-stamp {{PaidStampClass}}"><span class="mark">✓</span><span class="label">PAID</span></div>
    </div>
  </div>');

SET @html = REPLACE(@html,
    N'.paid-stamp, table.charges th, .payable-box {',
    N'table.charges th, .payable-box {');

SET @html = REPLACE(@html,
    N'
  <div class="signs">
    <div class="sign-block">
      <div class="sign-line"></div>
      <div class="sign-name">{{DoctorName}}</div>
      <div class="sign-meta">{{DoctorDesignation}}</div>
    </div>
    <div class="sign-block">
      <div class="sign-line"></div>
      <div class="sign-name">Authorised Signatory</div>
      <div class="sign-meta">{{HospitalName}}</div>
    </div>
  </div>
',
    N'');

UPDATE dbo.global_document_templates
SET bodyhtml = @html,
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
WHERE globaldocumenttemplateid = @invoiceId
  AND isdeleted = 0;

UPDATE dt
SET bodyhtml = g.bodyhtml,
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
FROM dbo.document_templates dt
INNER JOIN dbo.global_document_templates g ON g.globaldocumenttemplateid = dt.globaldocumenttemplateid
WHERE g.globaldocumenttemplateid = @invoiceId
  AND dt.isdeleted = 0;
GO
