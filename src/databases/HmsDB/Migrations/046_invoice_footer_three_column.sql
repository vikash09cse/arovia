-- Align invoice closing footer with sample: doctor | phone/web | authorised signatory.

DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @invoiceId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

UPDATE dbo.global_document_templates
SET bodyhtml = REPLACE(REPLACE(
        bodyhtml,
        N'  .thanks {
    margin-top: 14px; text-align: center; font-style: italic; color: #1e3a6e; font-size: 12px;
  }
  .thanks strong { font-weight: 700; }
  .contact-line {
    margin-top: 6px; text-align: center; color: #111; font-size: 10.5px;
  }
  .contact-line .icon { color: #2563eb; margin-right: 2px; }
  .contact-gap { display: inline-block; width: 18px; }
  .signs {
    margin-top: 14px; display: grid; grid-template-columns: 1fr 1fr; gap: 20px;
  }
  .sign-left { text-align: left; }
  .sign-right { text-align: right; }
  .sign-name { font-weight: 700; font-size: 11.5px; color: #1e3a6e; }
  .sign-meta { font-size: 9px; color: #64748b; margin-top: 2px; line-height: 1.35; }
  .sign-label { font-size: 10px; color: #64748b; }
  .footer-bar {
    margin-top: 14px; background: #1e3a6e; color: #fff; text-align: center;
    font-size: 10px; font-weight: 700; letter-spacing: 1px; padding: 7px 10px;
  }',
        N'  .thanks {
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
  }'),
        N'  <div class="thanks">Thank you for choosing <strong>{{HospitalName}}</strong></div>
  <div class="contact-line">
    <span class="icon">☎</span>{{HospitalPhone}}<span class="contact-gap"></span><span class="icon">◎</span>{{Website}}
  </div>

  <div class="signs">
    <div class="sign-left">
      <div class="sign-name">{{DoctorName}}</div>
      <div class="sign-meta">{{DoctorDesignation}}</div>
    </div>
    <div class="sign-right">
      <div class="sign-label">Authorised Signatory</div>
      <div class="sign-name">{{HospitalName}}</div>
    </div>
  </div>

  <div class="footer-bar">QUALITY CARE | COMPASSION | TRUST</div>',
        N'  <div class="thanks">Thank you for choosing <strong>{{HospitalName}}</strong></div>

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

  <div class="footer-bar">QUALITY CARE | COMPASSION | TRUST</div>'),
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
