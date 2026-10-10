-- Reset Discharge Invoice HTML template to Bill of Supply sample layout.
-- PDF download substitutes {{placeholders}} then renders this HTML.

DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @invoiceId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
DECLARE @invoiceHtml NVARCHAR(MAX) = N'<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8"/>
<title>Bill of Supply {{InvoiceNumber}}</title>
<style>
  @page { size: A4; margin: 8mm; }
  * { box-sizing: border-box; }
  body {
    font-family: Arial, Helvetica, sans-serif;
    color: #1a2744;
    margin: 0;
    padding: 4px 6px 0;
    font-size: 11px;
    line-height: 1.35;
    background: #fff;
  }
  .letterhead {
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
  .brand-center { padding-top: 0; padding-left: 26px; text-align: left; }
  .hospital, .cdc-brand-title {
    font-family: Georgia, "Times New Roman", serif;
    font-size: 26px; font-weight: 700; color: #1e3a6e; letter-spacing: 0.15px; line-height: 1.05;
  }
  .cdc-brand-black { color: #1e3a6e; }
  .cdc-brand-uro { color: #7CB342; }
  .tagline {
    margin-top: 4px; font-size: 9.5px; font-weight: 700; color: #1e3a6e;
    letter-spacing: 0.45px; text-transform: uppercase;
  }
  .services {
    margin-top: 3px; font-size: 8.5px; color: #64748b;
    letter-spacing: 0.2px; text-transform: uppercase;
  }
  .address { margin-top: 3px; font-size: 9.5px; color: #64748b; }
  .reg-box {
    border: 1.5px solid #1e3a6e; border-radius: 2px; padding: 10px 8px; text-align: center;
    align-self: center;
  }
  .reg-label { font-size: 8px; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px; }
  .reg-value { margin-top: 5px; font-size: 12px; font-weight: 700; color: #1e3a6e; word-break: break-word; }
  .meta-wrap {
    border: 1px solid #9bb4d4;
    margin: 8px 0 12px;
    padding: 0;
  }
  .meta-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0; }
  .meta-col { padding: 10px 12px; }
  .meta-col + .meta-col { border-left: 1px solid #9bb4d4; }
  .meta-row { display: grid; grid-template-columns: 110px 10px 1fr; gap: 4px; margin-bottom: 4px; }
  .meta-label { color: #1e3a6e; font-weight: 700; }
  .meta-colon { color: #1e3a6e; font-weight: 700; }
  .meta-value { color: #111; font-weight: 400; }
  .bill-bar {
    background: #1e3a6e; color: #fff; text-align: center;
    font-size: 15px; font-weight: 700; letter-spacing: 1.4px;
    padding: 8px 10px; margin: 0 0 8px 0;
  }
  table.charges { width: 100%; border-collapse: collapse; margin: 0; }
  table.charges th {
    background: #1e3a6e; color: #fff; font-size: 10.5px; font-weight: 700;
    padding: 6px 7px; text-align: left; border: 1px solid #163057;
    text-transform: uppercase; letter-spacing: 0.3px;
  }
  table.charges th.amt, table.charges td.amt {
    text-align: right !important; white-space: nowrap; font-variant-numeric: tabular-nums;
  }
  table.charges th.sno, table.charges td.sno { width: 40px; text-align: center; }
  table.charges th.part, table.charges td.part { width: 26%; }
  table.charges td {
    padding: 5px 7px; border: 1px solid #d7deea; vertical-align: top; color: #1f2937; font-size: 10.5px;
  }
  table.charges tbody tr:nth-child(even) td { background: #eef3fb; }
  table.charges tbody tr:nth-child(odd) td { background: #fff; }
  .totals { margin-top: 8px; display: flex; flex-direction: column; align-items: flex-end; gap: 3px; }
  .gross { font-weight: 700; font-size: 12px; }
  .discount { color: #16a34a; font-weight: 700; font-size: 11px; }
  .payable-box {
    margin-top: 4px; width: 260px; background: #1e3a6e; color: #fff;
    padding: 8px 10px;
  }
  .payable-top { font-size: 10px; font-weight: 700; letter-spacing: 0.3px; text-align: left; }
  .payable-amt {
    margin-top: 4px; font-size: 20px; font-weight: 700; text-align: right;
    letter-spacing: 0.4px; font-variant-numeric: tabular-nums;
  }
  .words {
    margin-top: 4px; width: 260px; text-align: center; color: #64748b;
    font-size: 9.5px; font-family: Georgia, serif; text-transform: uppercase; font-style: italic;
  }
  .lower {
    margin-top: 12px; border: 1px solid #94a3b8; display: grid;
    grid-template-columns: 1fr 1fr; align-items: stretch;
  }
  .notes { padding: 8px 10px; }
  .notes-title { font-weight: 700; margin-bottom: 5px; color: #1e3a6e; }
  .notes ul { margin: 0; padding-left: 16px; color: #111; font-size: 10px; font-family: Georgia, serif; }
  .notes li { margin-bottom: 2px; }
  .pay-block {
    border-left: 1px solid #94a3b8; padding: 8px 10px; position: relative; min-height: 96px;
  }
  .pay-title { font-weight: 700; margin-bottom: 6px; color: #1e3a6e; }
  .pay-block .pay-row { display: grid; grid-template-columns: 95px 10px 1fr; gap: 4px; margin-bottom: 3px; }
  .pay-block .pay-row .meta-value.amt { font-weight: 700; }
  .paid-stamp {
    position: absolute; right: 10px; bottom: 8px;
    color: #16a34a; font-weight: 800; font-size: 12px;
    display: flex; align-items: center; gap: 4px; font-style: italic;
  }
  .paid-stamp .mark { font-size: 13px; color: #64748b; font-style: normal; }
  .paid-stamp.hidden { display: none; }
  .thanks {
    margin-top: 16px; text-align: center; font-style: italic; color: #1e3a6e; font-size: 12px;
  }
  .thanks strong { font-weight: 700; }
  .closing {
    margin-top: 18px; display: grid;
    grid-template-columns: 1.25fr 1fr 1fr; gap: 12px; align-items: start;
  }
  .sign-left { text-align: left; }
  .sign-center { text-align: center; padding-top: 2px; }
  .sign-right { text-align: right; }
  .sign-name { font-weight: 700; font-size: 12px; color: #1e3a6e; }
  .sign-meta { font-size: 9px; color: #64748b; margin-top: 3px; line-height: 1.4; }
  .sign-label { font-size: 10px; color: #64748b; margin-bottom: 3px; }
  .contact-item {
    margin-bottom: 5px; font-size: 10.5px; color: #64748b;
  }
  .contact-item .icon { margin-right: 4px; }
  .contact-item .icon-phone { color: #64748b; }
  .contact-item .icon-web { color: #2563eb; }
  .contact-item .web-link { color: #1e3a6e; text-decoration: underline; font-weight: 600; }
  .footer-bar {
    margin-top: 16px; background: #1e3a6e; color: #fff; text-align: center;
    font-size: 10px; font-weight: 700; letter-spacing: 1px; padding: 7px 10px;
  }
  @media print {
    body { padding: 0; }
    .paid-stamp, .bill-bar, table.charges th, .payable-box, .footer-bar {
      -webkit-print-color-adjust: exact; print-color-adjust: exact;
    }
  }
</style>
</head>
<body>
  <div class="letterhead">
    <div>{{LogoHtml}}</div>
    <div class="brand-center">
      <div class="hospital">{{BrandTitleHtml}}</div>
      <div class="tagline">{{ReceiptHeader}}</div>
      <div class="services">{{ProceduresLine}}</div>
      <div class="address">{{HospitalAddress}}</div>
    </div>
    <div class="reg-box">
      <div class="reg-label">REGISTRATION NO.</div>
      <div class="reg-value">{{HospitalRegistrationNo}}</div>
    </div>
  </div>

  <div class="meta-wrap">
    <div class="meta-grid">
      <div class="meta-col">
        <div class="meta-row"><span class="meta-label">Patient Id</span><span class="meta-colon">:</span><span class="meta-value">{{PatientId}}</span></div>
        <div class="meta-row"><span class="meta-label">Patient Name</span><span class="meta-colon">:</span><span class="meta-value">{{PatientName}}</span></div>
        <div class="meta-row"><span class="meta-label">Age / Sex</span><span class="meta-colon">:</span><span class="meta-value">{{Age}} / {{Gender}}</span></div>
        <div class="meta-row"><span class="meta-label">Address</span><span class="meta-colon">:</span><span class="meta-value">{{Address}}</span></div>
        <div class="meta-row"><span class="meta-label">Mobile No.</span><span class="meta-colon">:</span><span class="meta-value">{{Phone}}</span></div>
      </div>
      <div class="meta-col">
        <div class="meta-row"><span class="meta-label">Bill No.</span><span class="meta-colon">:</span><span class="meta-value">{{InvoiceNumber}}</span></div>
        <div class="meta-row"><span class="meta-label">Bill Date</span><span class="meta-colon">:</span><span class="meta-value">{{BillDate}}</span></div>
        <div class="meta-row"><span class="meta-label">Admission Date</span><span class="meta-colon">:</span><span class="meta-value">{{AdmissionDate}}</span></div>
        <div class="meta-row"><span class="meta-label">Procedure Date</span><span class="meta-colon">:</span><span class="meta-value">{{ProcedureDate}}</span></div>
        <div class="meta-row"><span class="meta-label">Discharge Date</span><span class="meta-colon">:</span><span class="meta-value">{{DischargeDate}}</span></div>
      </div>
    </div>
  </div>

  <div class="bill-bar">BILL OF SUPPLY</div>
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
      <div class="payable-top">TOTAL AMOUNT PAYABLE</div>
      <div class="payable-amt">₹ {{PayableAmount}}</div>
    </div>
    <div class="words">({{AmountInWords}})</div>
  </div>

  <div class="lower">
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
  </div>

  <div class="thanks">Thank you for choosing <strong>{{HospitalName}}</strong></div>

  <div class="closing">
    <div class="sign-left">
      <div class="sign-name">{{DoctorName}}</div>
      <div class="sign-meta">{{DoctorDesignation}}</div>
    </div>
    <div class="sign-center">
      <div class="contact-item"><span class="icon icon-phone">☎</span>{{HospitalPhone}}</div>
      <div class="contact-item"><span class="icon icon-web">◎</span><span class="web-link">{{Website}}</span></div>
    </div>
    <div class="sign-right">
      <div class="sign-label">Authorised Signatory</div>
      <div class="sign-name">{{HospitalName}}</div>
    </div>
  </div>

  <div class="footer-bar">QUALITY CARE | COMPASSION | TRUST</div>
</body>
</html>';

UPDATE dbo.global_document_templates
SET bodyhtml = @invoiceHtml,
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
