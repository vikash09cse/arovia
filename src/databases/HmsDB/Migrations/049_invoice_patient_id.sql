-- Add Patient Id row before Patient Name on Discharge Invoice template.

DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @invoiceId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

UPDATE dbo.global_document_templates
SET bodyhtml = REPLACE(
        bodyhtml,
        N'<div class="meta-row"><span class="meta-label">Patient Name</span><span class="meta-colon">:</span><span class="meta-value">{{PatientName}}</span></div>',
        N'<div class="meta-row"><span class="meta-label">Patient Id</span><span class="meta-colon">:</span><span class="meta-value">{{PatientId}}</span></div>
        <div class="meta-row"><span class="meta-label">Patient Name</span><span class="meta-colon">:</span><span class="meta-value">{{PatientName}}</span></div>'),
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
WHERE globaldocumenttemplateid = @invoiceId
  AND isdeleted = 0
  AND bodyhtml LIKE N'%{{PatientName}}%'
  AND bodyhtml NOT LIKE N'%{{PatientId}}%';

UPDATE dt
SET bodyhtml = g.bodyhtml,
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
FROM dbo.document_templates dt
INNER JOIN dbo.global_document_templates g ON g.globaldocumenttemplateid = dt.globaldocumenttemplateid
WHERE g.globaldocumenttemplateid = @invoiceId
  AND dt.isdeleted = 0;
GO
