-- Image-1 letterhead refresh (dual logos, lime URO, light-blue title bar). Re-applies full invoice HTML.

DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @invoiceId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
DECLARE @invoiceHtml NVARCHAR(MAX) = N'<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8"/>
<title>Bill of Supply {{InvoiceNumber}}</title>
<style>
  @page { size: A4; margin: 10mm; }
  * { box-sizing: border-box; }
  body {
    font-family: Arial, Helvetica, sans-serif;
    color: #1a2744;
    margin: 0;
    padding: 8px 12px 12px;
    font-size: 11.5px;
    line-height: 1.35;
    background: #fff;
  }
  .cdc-letterhead {
    display: grid;
    grid-template-columns: 110px 1fr 110px;
    gap: 8px;
    align-items: start;
  }
  .cdc-logo-cell { text-align: center; }
  .cdc-logo, .logo {
    width: 100px; height: 100px; object-fit: contain;
    display: block; margin: 0 auto;
  }
  .cdc-logo-fallback, .logo-fallback {
    width: 100px; height: 100px; border: 2px solid #7CB342; border-radius: 8px;
    background: #f7fbe9; color: #7CB342;
    font-weight: 800; font-size: 22px;
    display: flex; align-items: center; justify-content: center;
    margin: 0 auto;
  }
  .cdc-brand-center { text-align: center; padding-top: 2px; }
  .cdc-brand-title {
    font-size: 30px; font-weight: 800; letter-spacing: 0.4px; line-height: 1.05;
  }
  .cdc-brand-black { color: #111; }
  .cdc-brand-uro { color: #7CB342; }
  .cdc-brand-rule {
    width: 72%; max-width: 280px; margin: 6px auto 8px;
    border: 0; border-top: 1.5px solid #111;
  }
  .cdc-doctor-name { margin: 0; font-size: 13px; font-weight: 700; color: #111; }
  .cdc-doctor-cred { margin-top: 2px; font-size: 11px; color: #222; line-height: 1.35; }
  .cdc-title-bar {
    margin-top: 10px; background: #5BA3D9; color: #fff; text-align: center;
    font-size: 15px; font-weight: 800; letter-spacing: 1px; padding: 7px 10px;
    text-transform: uppercase;
  }
  .meta-wrap {
    border-top: 1px solid #1e3a6e; border-bottom: 1px solid #1e3a6e;
    padding: 10px 0; margin: 0 0 12px;
  }
  .meta-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; }
  .meta-row { display: grid; grid-template-columns: 110px 10px 1fr; gap: 4px; margin-bottom: 4px; }
  .meta-label { color: #334155; font-weight: 600; }
  .meta-colon { color: #334155; }
  .meta-value { color: #0f172a; font-weight: 500; }
  table.charges { width: 100%; border-collapse: collapse; margin: 0; }
  table.charges th {
    background: #1e3a6e; color: #fff; font-size: 11px; font-weight: 700;
    padding: 7px 8px; text-align: left; border: 1px solid #1e3a6e;
  }
  table.charges th.amt, table.charges td.amt { text-align: right; white-space: nowrap; }
  table.charges th.sno, table.charges td.sno { width: 42px; text-align: center; }
  table.charges th.part, table.charges td.part { width: 26%; }
  table.charges td {
    padding: 6px 8px; border: 1px solid #d7deea; vertical-align: top; color: #1f2937;
  }
  table.charges tbody tr:nth-child(even) td { background: #eef3fb; }
  table.charges tbody tr:nth-child(odd) td { background: #fff; }
  .totals { margin-top: 10px; display: flex; flex-direction: column; align-items: flex-end; gap: 4px; }
  .gross { font-weight: 700; font-size: 12.5px; }
  .discount { color: #15803d; font-weight: 700; font-size: 12px; }
  .payable-box {
    margin-top: 6px; width: 280px; background: #1e3a6e; color: #fff;
    border-radius: 2px; padding: 10px 12px;
  }
  .payable-top { display: flex; justify-content: space-between; font-size: 11px; font-weight: 700; letter-spacing: 0.4px; }
  .payable-amt { margin-top: 4px; font-size: 22px; font-weight: 700; text-align: right; letter-spacing: 0.5px; }
  .words { margin-top: 6px; width: 100%; text-align: right; color: #64748b; font-size: 10px; font-family: Georgia, serif; text-transform: uppercase; }
  .lower {
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
  .paid-stamp.hidden { visibility: hidden; }
  .thanks { margin-top: 18px; text-align: center; font-style: italic; color: #334155; font-size: 12px; }
  .signs { margin-top: 22px; display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
  .sign-block { text-align: center; }
  .sign-line { border-top: 1px solid #94a3b8; margin: 36px 24px 6px; }
  .sign-name { font-weight: 700; font-size: 12px; }
  .sign-meta { font-size: 10px; color: #64748b; margin-top: 2px; }
  .cdc-page-footer { margin-top: 18px; text-align: center; line-height: 1.45; }
  .cdc-phone { font-size: 13px; font-weight: 700; color: #111; }
  .cdc-phone-icon { color: #c62828; margin-right: 4px; }
  .cdc-address { margin-top: 4px; font-size: 11px; color: #333; }
  .cdc-copy { margin-top: 4px; font-size: 10px; color: #c62828; font-weight: 600; }
  @media print {
    body { padding: 0; }
    .paid-stamp, table.charges th, .payable-box {
      -webkit-print-color-adjust: exact; print-color-adjust: exact;
    }
  }
</style>
</head>
<body>
  <header>
    <div class="cdc-letterhead">
      <div class="cdc-logo-cell">{{LogoHtml}}</div>
      <div class="cdc-brand-center">
        <div class="cdc-brand-title">{{BrandTitleHtml}}</div>
        <hr class="cdc-brand-rule" />
        <div class="cdc-doctor-name">{{DoctorName}}</div>
        {{DoctorCredentialsHtml}}
      </div>
      <div class="cdc-logo-cell">{{LogoHtml}}</div>
    </div>
    <div class="cdc-title-bar">BILL OF SUPPLY</div>
  </header>

  <div class="meta-wrap">
    <div class="meta-grid">
      <div>
        <div class="meta-row"><span class="meta-label">Patient Name</span><span class="meta-colon">:</span><span class="meta-value">{{PatientName}}</span></div>
        <div class="meta-row"><span class="meta-label">Age / Sex</span><span class="meta-colon">:</span><span class="meta-value">{{Age}} / {{Gender}}</span></div>
        <div class="meta-row"><span class="meta-label">Address</span><span class="meta-colon">:</span><span class="meta-value">{{Address}}</span></div>
        <div class="meta-row"><span class="meta-label">Mobile No.</span><span class="meta-colon">:</span><span class="meta-value">{{Phone}}</span></div>
      </div>
      <div>
        <div class="meta-row"><span class="meta-label">Bill No.</span><span class="meta-colon">:</span><span class="meta-value">{{InvoiceNumber}}</span></div>
        <div class="meta-row"><span class="meta-label">Bill Date</span><span class="meta-colon">:</span><span class="meta-value">{{BillDate}}</span></div>
        <div class="meta-row"><span class="meta-label">Admission Date</span><span class="meta-colon">:</span><span class="meta-value">{{AdmissionDate}}</span></div>
        <div class="meta-row"><span class="meta-label">Procedure Date</span><span class="meta-colon">:</span><span class="meta-value">{{ProcedureDate}}</span></div>
        <div class="meta-row"><span class="meta-label">Discharge Date</span><span class="meta-colon">:</span><span class="meta-value">{{DischargeDate}}</span></div>
      </div>
    </div>
  </div>

  <table class="charges">
    <thead>
      <tr>
        <th class="sno">S.No</th>
        <th class="part">Particulars</th>
        <th>Description</th>
        <th class="amt">Amount (₹)</th>
      </tr>
    </thead>
    <tbody>
      {{ChargeRows}}
    </tbody>
  </table>

  <div class="totals">
    <div class="gross">GROSS TOTAL &nbsp;&nbsp; ₹{{GrossTotal}}</div>
    {{DiscountLine}}
    <div class="payable-box">
      <div class="payable-top"><span>TOTAL AMOUNT PAYABLE</span><span>₹</span></div>
      <div class="payable-amt">{{PayableAmount}}</div>
    </div>
    <div class="words">({{AmountInWords}})</div>
  </div>

  <div class="lower">
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
  </div>

  <div class="thanks">Thank you for choosing {{HospitalName}}</div>

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

  <footer class="cdc-page-footer">
    <div class="cdc-phone"><span class="cdc-phone-icon">☎</span>{{HospitalPhone}}</div>
    <div class="cdc-address">{{HospitalAddress}}</div>
    <div class="cdc-copy">{{CopyrightLine}}</div>
  </footer>
</body>
</html>';

UPDATE dbo.global_document_templates
SET bodyhtml = @invoiceHtml,
    name = N'Default Discharge Invoice',
    templatetype = 3,
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
WHERE globaldocumenttemplateid = @invoiceId
  AND isdeleted = 0;

UPDATE dt
SET bodyhtml = g.bodyhtml,
    name = g.name,
    templatetype = g.templatetype,
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
FROM dbo.document_templates dt
INNER JOIN dbo.global_document_templates g ON g.globaldocumenttemplateid = dt.globaldocumenttemplateid
WHERE g.globaldocumenttemplateid = @invoiceId
  AND dt.isdeleted = 0;
GO
