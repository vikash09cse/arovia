-- Receipt templates: prefer {{FeeRows}} for dynamic add-on line names.
-- App also rewrites <tbody> when this placeholder is absent.
DECLARE @seedActor UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';

UPDATE dbo.global_document_templates
SET bodyhtml = REPLACE(
        bodyhtml,
        N'<tr><td class="sno">1</td><td>Consultation Fee</td><td class="amt">{{ConsultationFee}}</td></tr>
      <tr><td class="sno">2</td><td>Procedure Charges</td><td class="amt">{{ProcedureCharge}}</td></tr>
      <tr><td class="sno">3</td><td>Add-on Charges</td><td class="amt">{{AddonCharges}}</td></tr>
      <tr><td class="sno">4</td><td>Discount</td><td class="amt">{{Discount}}</td></tr>',
        N'{{FeeRows}}'),
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
WHERE templatetype = 1
  AND isdeleted = 0
  AND bodyhtml LIKE N'%Add-on Charges%'
  AND bodyhtml NOT LIKE N'%{{FeeRows}}%';

UPDATE dbo.document_templates
SET bodyhtml = REPLACE(
        bodyhtml,
        N'<tr><td class="sno">1</td><td>Consultation Fee</td><td class="amt">{{ConsultationFee}}</td></tr>
      <tr><td class="sno">2</td><td>Procedure Charges</td><td class="amt">{{ProcedureCharge}}</td></tr>
      <tr><td class="sno">3</td><td>Add-on Charges</td><td class="amt">{{AddonCharges}}</td></tr>
      <tr><td class="sno">4</td><td>Discount</td><td class="amt">{{Discount}}</td></tr>',
        N'{{FeeRows}}'),
    updatedby = @seedActor,
    updatedat = SYSUTCDATETIME()
WHERE templatetype = 1
  AND isdeleted = 0
  AND bodyhtml LIKE N'%Add-on Charges%'
  AND bodyhtml NOT LIKE N'%{{FeeRows}}%';
GO
