-- Match invoice header to sample: mixed-case brand, bordered meta box, separate BILL OF SUPPLY bar.

DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @invoiceId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

UPDATE dbo.global_document_templates
SET bodyhtml = REPLACE(REPLACE(REPLACE(
        bodyhtml,
        N'  .letterhead {
    display: grid;
    grid-template-columns: 72px 1fr 140px;
    gap: 10px;
    align-items: start;
    margin-bottom: 8px;
  }
  .logo, .cdc-logo {
    width: 68px; height: 68px; object-fit: contain;
    display: block;
  }
  .logo-fallback, .cdc-logo-fallback {
    width: 68px; height: 68px; border-radius: 6px;
    border: 2px solid #7CB342; display: flex; align-items: center; justify-content: center;
    color: #7CB342; font-weight: 800; font-size: 20px; background: #f7fbe9;
  }
  .brand-center { padding-top: 2px; }
  .hospital, .cdc-brand-title {
    font-family: Georgia, "Times New Roman", serif;
    font-size: 24px; font-weight: 700; color: #1e3a6e; letter-spacing: 0.2px; line-height: 1.1;
  }
  .cdc-brand-black { color: #1e3a6e; }
  .cdc-brand-uro { color: #7CB342; }
  .tagline {
    margin-top: 3px; font-size: 9.5px; font-weight: 700; color: #243a66;
    letter-spacing: 0.5px; text-transform: uppercase;
  }
  .services { margin-top: 3px; font-size: 8.5px; color: #6b7280; }
  .address { margin-top: 3px; font-size: 9.5px; color: #6b7280; }
  .reg-box {
    border: 1px solid #94a3b8; border-radius: 3px; padding: 8px 8px; text-align: center;
  }
  .reg-label { font-size: 8.5px; color: #64748b; text-transform: uppercase; letter-spacing: 0.4px; }
  .reg-value { margin-top: 4px; font-size: 11px; font-weight: 700; color: #1e3a6e; word-break: break-word; }
  .meta-wrap {
    border-top: 1.5px solid #1e3a6e; border-bottom: 1.5px solid #1e3a6e;
    padding: 8px 0; margin: 6px 0 10px;
  }
  .meta-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
  .meta-row { display: grid; grid-template-columns: 105px 10px 1fr; gap: 4px; margin-bottom: 3px; }
  .meta-label { color: #334155; font-weight: 600; }
  .meta-colon { color: #334155; }
  .meta-value { color: #0f172a; font-weight: 500; }
  .bill-bar {
    background: #1e3a6e; color: #fff; text-align: center;
    font-size: 14px; font-weight: 700; letter-spacing: 1.2px;
    padding: 7px 10px; margin-bottom: 0;
  }
  table.charges { width: 100%; border-collapse: collapse; margin: 0; }
  table.charges th {
    background: #1e3a6e; color: #fff; font-size: 10.5px; font-weight: 700;
    padding: 6px 7px; text-align: left; border: 1px solid #1e3a6e;
  }',
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
  .brand-center { padding-top: 0; text-align: left; }
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
  }'),
        N'  <div class="meta-wrap">
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
  </div>',
        N'  <div class="meta-wrap">
    <div class="meta-grid">
      <div class="meta-col">
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
  </div>'),
        N'  <div class="reg-box">
      <div class="reg-label">Registration No.</div>
      <div class="reg-value">{{HospitalRegistrationNo}}</div>
    </div>',
        N'  <div class="reg-box">
      <div class="reg-label">REGISTRATION NO.</div>
      <div class="reg-value">{{HospitalRegistrationNo}}</div>
    </div>'),
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
