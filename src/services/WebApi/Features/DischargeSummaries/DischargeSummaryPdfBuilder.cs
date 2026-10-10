using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebApi.Features.Shared;

namespace WebApi.Features.DischargeSummaries;

public static class DischargeSummaryPdfBuilder
{
    private static readonly Color Navy = Color.FromHex("#1E4E79");
    private static readonly Color Line = Color.FromHex("#C5D0DE");
    private static readonly Color Soft = Color.FromHex("#E8EEF6");

    static DischargeSummaryPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(DischargeSummaryResponse data, byte[]? logoBytes)
    {
        var model = DischargeSummaryPrintModel.From(data, logoBytes);

        // Single continuous page definition so content flows naturally (no blank
        // overflow pages from a hardcoded 2-page split).
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginTop(18);
                page.MarginBottom(18);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken4));

                // Branding repeats; "DISCHARGE SUMMARY" title bar only on page 1 (in content).
                page.Header().Element(c => ComposeLetterhead(c, model, includeDocumentTitle: false));
                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Spacing(4);
                    col.Item().Element(c => ClinicalDocumentChrome.ComposeDocumentTitleBar(c, "DISCHARGE SUMMARY"));
                    ComposePatientDetails(col, model);
                    SectionText(col, "2. FINAL DIAGNOSIS", model.FinalDiagnosis);
                    SectionText(col, "3. CHIEF COMPLAINTS", model.ChiefComplaints);
                    SectionText(col, "4. HISTORY OF PRESENT ILLNESS", model.BriefHistory);
                    ComposePastHistory(col, model);
                    ComposeExamination(col, model);
                    ComposeInvestigations(col, model);
                    ComposeTreatment(col, model);
                    ComposeProcedure(col, model);
                    SectionText(col, "10. OPERATIVE FINDINGS", model.Findings);
                    SectionText(col, "11. PROCEDURE DETAILS", model.ProcedureDetails);
                    ComposeIntraPost(col, model);
                    ComposeCondition(col, model);
                    ComposeMedicines(col, model);
                    ComposeAdviceFollowUp(col, model);
                    ComposeSignatures(col, model);
                });
                page.Footer().Element(c => ComposePageFooter(c, model));
            });
        }).GeneratePdf();
    }

    private static void ComposeLetterhead(
        IContainer container, DischargeSummaryPrintModel m, bool includeDocumentTitle)
    {
        ClinicalDocumentChrome.ComposeLetterhead(
            container,
            m.HospitalName,
            m.DoctorName,
            m.CredentialLines,
            "DISCHARGE SUMMARY",
            m.LogoBytes,
            m.LogoLetters,
            includeDocumentTitle);
    }

    private static void ComposePageFooter(IContainer container, DischargeSummaryPrintModel m)
    {
        container.Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(m.HospitalPhone))
            {
                col.Item().AlignCenter().Text(text =>
                {
                    text.Span("☎ ").FontColor(Color.FromHex(ClinicalDocumentChrome.FooterRed)).FontSize(9);
                    text.Span(m.HospitalPhone.Trim()).Bold().FontSize(9).FontColor(Colors.Black);
                });
            }

            if (!string.IsNullOrWhiteSpace(m.HospitalAddress))
            {
                col.Item().PaddingTop(2).AlignCenter()
                    .Text(m.HospitalAddress.Trim()).FontSize(8).FontColor(Colors.Grey.Darken3);
            }

            col.Item().PaddingTop(2).AlignCenter()
                .Text(m.CopyrightLine).FontSize(7.5f)
                .FontColor(Color.FromHex(ClinicalDocumentChrome.FooterRed));

            col.Item().PaddingTop(3).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    private static void SectionBar(ColumnDescriptor col, string title) =>
        col.Item().Background(Navy).PaddingVertical(3).PaddingHorizontal(6)
            .Text(title).Bold().FontSize(8.5f).FontColor(Colors.White);

    private static void SectionBody(ColumnDescriptor col, Action<IContainer> content) =>
        col.Item().Border(1).BorderColor(Line).BorderTop(0).Padding(6)
            .EnsureSpace(40).Element(content);

    private static void SectionText(ColumnDescriptor col, string title, string text)
    {
        // Keep heading + body together so we don't leave orphan section bars.
        col.Item().EnsureSpace(48).Column(block =>
        {
            SectionBar(block, title);
            block.Item().Border(1).BorderColor(Line).BorderTop(0).Padding(6)
                .Text(Dash(text)).FontSize(9);
        });
    }

    private static void ComposePatientDetails(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "1. PATIENT DETAILS");
        SectionBody(col, body => body.Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                Kv(left, "Patient Name", m.PatientName);
                Kv(left, "Age / Sex", m.AgeSex);
                Kv(left, "Father's Name", m.FatherName);
                Kv(left, "Address", m.PatientAddress);
                Kv(left, "Date & Time of Admission", m.AdmittedAt);
                Kv(left, "Date & Time of Discharge", m.DischargedAt);
            });
            row.RelativeItem().PaddingLeft(12).Column(right =>
            {
                Kv(right, "IPD No.", m.AdmissionCode);
                Kv(right, "UHID", m.PatientCode);
                Kv(right, "Ward / Bed", m.WardBed);
                Kv(right, "Mobile No.", m.PatientPhone);
                Kv(right, "Date & Time of Surgery", m.SurgeryDate);
                Kv(right, "Treating Consultant", m.DoctorName);
            });
        }));
    }

    private static void ComposePastHistory(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "5. PAST HISTORY");
        SectionBody(col, body => body.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });
            void Head(string h) => t.Cell().Border(0.5f).BorderColor(Line).Background(Soft)
                .Padding(4).Text(h).Bold().FontSize(7.5f).FontColor(Navy);
            void Cell(string v) => t.Cell().Border(0.5f).BorderColor(Line).Padding(4).MinHeight(28)
                .Text(Dash(v)).FontSize(8);
            Head("Comorbidities");
            Head("Past Surgical History");
            Head("Allergy");
            Head("Addiction");
            Cell(m.Comorbidities);
            Cell(m.PastSurgery);
            Cell(m.Allergy);
            Cell(m.Addiction);
        }));
    }

    private static void ComposeExamination(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "6. EXAMINATION AT ADMISSION");
        SectionBody(col, body => body.Column(c =>
        {
            Kv(c, "General Examination", m.ExamGeneral);
            Kv(c, "Vitals", m.ExamVitals);
            Kv(c, "Systemic Examination", m.ExamSystemic);
            Kv(c, "Per Abdomen", m.ExamPerAbdomen);
            Kv(c, "Genitalia", m.ExamGenitalia);
        }));
    }

    private static void ComposeInvestigations(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "7. INVESTIGATIONS");
        SectionBody(col, body => body.Row(row =>
        {
            row.RelativeItem().Element(c => InvTable(c, m.InvLeft));
            row.ConstantItem(8);
            row.RelativeItem().Element(c => InvTable(c, m.InvRight));
        }));
    }

    private static void InvTable(IContainer container, IReadOnlyList<(string Test, string Result, string Date)> rows)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.2f);
                c.RelativeColumn();
                c.RelativeColumn(0.9f);
            });
            t.Header(h =>
            {
                void H(string x) => h.Cell().Background(Soft).Border(0.5f).BorderColor(Line)
                    .Padding(3).Text(x).Bold().FontSize(7.5f).FontColor(Navy);
                H("Test"); H("Result"); H("Date");
            });
            foreach (var (test, result, date) in rows)
            {
                t.Cell().Border(0.5f).BorderColor(Line).Padding(3).Text(test).FontSize(7.5f);
                t.Cell().Border(0.5f).BorderColor(Line).Padding(3).Text(Dash(result)).FontSize(7.5f);
                t.Cell().Border(0.5f).BorderColor(Line).Padding(3).Text(Dash(date)).FontSize(7.5f);
            }
        });
    }

    private static void ComposeTreatment(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "8. TREATMENT DURING ADMISSION");
        SectionBody(col, body =>
        {
            if (m.TreatmentLines.Count == 0)
                body.Text("—");
            else
                body.Column(c =>
                {
                    foreach (var line in m.TreatmentLines)
                        c.Item().Text($"• {line}").FontSize(8.5f);
                });
        });
    }

    private static void ComposeProcedure(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "9. PROCEDURE / SURGERY DETAILS");
        SectionBody(col, body => body.Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                Kv(left, "Procedure", m.ProcedureKey);
                Kv(left, "Date of Surgery", m.SurgeryDate);
                Kv(left, "Anaesthesia", m.Anaesthesia);
            });
            row.RelativeItem().PaddingLeft(12).Column(right =>
            {
                Kv(right, "Operating Surgeon", m.DoctorName);
                Kv(right, "Assistant Surgeon", m.AssistantSurgeon);
                Kv(right, "Indication", m.Indication);
            });
        }));
    }

    private static void ComposeIntraPost(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "12. INTRAOPERATIVE / POSTOPERATIVE COURSE");
        SectionBody(col, body => body.Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                Kv(left, "Specimen Sent", m.Specimen);
                Kv(left, "Drain / Catheter", m.Catheter);
            });
            row.RelativeItem().PaddingLeft(12).Column(right =>
            {
                Kv(right, "Intraoperative Complications", m.IntraopComplications);
                Kv(right, "Postoperative Course", m.PostopCourse);
            });
        }));
    }

    private static void ComposeCondition(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "13. CONDITION AT DISCHARGE");
        SectionBody(col, body => body.Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                Kv(left, "General Condition", m.GeneralCondition);
                Kv(left, "Vitals", m.DischargeVitals);
                Kv(left, "Urine Output", m.UrineOutput);
                Kv(left, "Oral Intake", m.OralIntake);
            });
            row.RelativeItem().PaddingLeft(12).Column(right =>
            {
                Kv(right, "Ambulation", m.Ambulation);
                Kv(right, "PUC", m.Catheter);
                Kv(right, "Discharge Type", m.DischargeType);
            });
        }));
    }

    private static void ComposeMedicines(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        SectionBar(col, "14. DISCHARGE MEDICATIONS");
        SectionBody(col, body =>
        {
            if (m.Medicines.Count == 0)
            {
                body.Text("—");
                return;
            }

            body.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(28);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn(0.8f);
                    c.RelativeColumn(0.8f);
                    c.RelativeColumn();
                    c.RelativeColumn(1.2f);
                });
                t.Header(h =>
                {
                    void H(string x) => h.Cell().Background(Navy).Border(0.5f).BorderColor(Navy)
                        .Padding(3).Text(x).Bold().FontSize(7).FontColor(Colors.White);
                    H("S.No."); H("Medicine"); H("Strength"); H("Dose");
                    H("Route"); H("Freq"); H("Duration"); H("Timing / Instructions");
                });
                var i = 0;
                foreach (var med in m.Medicines)
                {
                    i++;
                    void C(string x) => t.Cell().Border(0.5f).BorderColor(Line).Padding(3).Text(Dash(x)).FontSize(7);
                    C(i.ToString());
                    C(med.Name);
                    C(med.Strength);
                    C(med.Dose);
                    C(med.Route);
                    C(med.Frequency);
                    C(med.Duration);
                    C(med.Timing);
                }
            });
        });
    }

    private static void ComposeAdviceFollowUp(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                SectionBar(left, "15. ADVICE / INSTRUCTIONS");
                SectionBody(left, body =>
                {
                    if (m.AdviceLines.Count == 0)
                        body.Text("—");
                    else
                        body.Column(c =>
                        {
                            foreach (var line in m.AdviceLines)
                                c.Item().Text($"• {line}").FontSize(8.5f);
                        });
                });
            });
            row.ConstantItem(8);
            row.RelativeItem().Column(right =>
            {
                SectionBar(right, "16. FOLLOW-UP");
                SectionBody(right, body => body.Column(c =>
                {
                    Kv(c, "Follow-up Date", m.FollowUpDate);
                    Kv(c, "Follow-up Department", m.FollowUpDepartment);
                    Kv(c, "Planned Procedure", m.FollowUpPlan);
                    Kv(c, "Investigations (if any)", m.FollowUpInvestigations);
                    Kv(c, "Additional Advice", m.FollowUpNotes);
                }));
            });
        });
    }

    private static void ComposeSignatures(ColumnDescriptor col, DischargeSummaryPrintModel m)
    {
        col.Item().PaddingTop(18).Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                left.Item().PaddingTop(28).BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(4)
                    .Text("Patient / Attendant Signature").SemiBold().FontSize(8);
                left.Item().PaddingTop(6).Text("Name: ____________________").FontSize(8);
                left.Item().Text("Date: ____________________").FontSize(8);
            });
            row.RelativeItem().AlignRight().Column(right =>
            {
                right.Item().PaddingTop(28).Width(180).AlignRight().BorderTop(1).BorderColor(Colors.Grey.Darken2)
                    .PaddingTop(4).AlignRight()
                    .Text("Treating Doctor Signature & Stamp").SemiBold().FontSize(8);
                right.Item().PaddingTop(6).AlignRight().Text(m.DoctorName).Bold().FontSize(9);
                if (!string.IsNullOrWhiteSpace(m.DoctorDesignation))
                    right.Item().AlignRight().Text(m.DoctorDesignation).FontSize(7.5f);
                right.Item().AlignRight().Text(m.HospitalName).FontSize(7.5f);
            });
        });
    }

    private static void Kv(ColumnDescriptor col, string label, string? value) =>
        col.Item().Text(t =>
        {
            t.Span($"{label}: ").Bold().FontSize(8.5f).FontColor(Navy);
            t.Span(Dash(value)).FontSize(8.5f);
        });

    private static string Dash(string? v) => string.IsNullOrWhiteSpace(v) ? "—" : v.Trim();
}

internal sealed class DischargeSummaryPrintModel
{
    public byte[]? LogoBytes { get; init; }
    public string LogoLetters { get; init; } = "JU";
    public string HospitalName { get; init; } = "";
    public string HospitalAddress { get; init; } = "";
    public string HospitalPhone { get; init; } = "";
    public string CopyrightLine { get; init; } = "";
    public string DoctorName { get; init; } = "";
    public string DoctorDesignation { get; init; } = "";
    public IReadOnlyList<string> CredentialLines { get; init; } = [];
    public string DepartmentName { get; init; } = "";
    public string PatientName { get; init; } = "";
    public string AgeSex { get; init; } = "";
    public string FatherName { get; init; } = "";
    public string PatientAddress { get; init; } = "";
    public string AdmittedAt { get; init; } = "";
    public string DischargedAt { get; init; } = "";
    public string AdmissionCode { get; init; } = "";
    public string PatientCode { get; init; } = "";
    public string WardBed { get; init; } = "";
    public string PatientPhone { get; init; } = "";
    public string SurgeryDate { get; init; } = "";
    public string FinalDiagnosis { get; init; } = "";
    public string ChiefComplaints { get; init; } = "";
    public string BriefHistory { get; init; } = "";
    public string Comorbidities { get; init; } = "";
    public string PastSurgery { get; init; } = "";
    public string Allergy { get; init; } = "";
    public string Addiction { get; init; } = "";
    public string ExamGeneral { get; init; } = "";
    public string ExamVitals { get; init; } = "";
    public string ExamSystemic { get; init; } = "";
    public string ExamPerAbdomen { get; init; } = "";
    public string ExamGenitalia { get; init; } = "";
    public IReadOnlyList<(string Test, string Result, string Date)> InvLeft { get; init; } = [];
    public IReadOnlyList<(string Test, string Result, string Date)> InvRight { get; init; } = [];
    public IReadOnlyList<string> TreatmentLines { get; init; } = [];
    public string ProcedureKey { get; init; } = "";
    public string ProcedureDetails { get; init; } = "";
    public string Findings { get; init; } = "";
    public string Anaesthesia { get; init; } = "";
    public string AssistantSurgeon { get; init; } = "";
    public string Indication { get; init; } = "";
    public string Specimen { get; init; } = "";
    public string Catheter { get; init; } = "";
    public string IntraopComplications { get; init; } = "";
    public string PostopCourse { get; init; } = "";
    public string GeneralCondition { get; init; } = "";
    public string DischargeVitals { get; init; } = "";
    public string UrineOutput { get; init; } = "";
    public string OralIntake { get; init; } = "";
    public string Ambulation { get; init; } = "";
    public string DischargeType { get; init; } = "";
    public IReadOnlyList<MedRow> Medicines { get; init; } = [];
    public IReadOnlyList<string> AdviceLines { get; init; } = [];
    public string FollowUpDate { get; init; } = "";
    public string FollowUpDepartment { get; init; } = "";
    public string FollowUpPlan { get; init; } = "";
    public string FollowUpInvestigations { get; init; } = "";
    public string FollowUpNotes { get; init; } = "";

    public record MedRow(string Name, string Strength, string Dose, string Route, string Frequency, string Duration, string Timing);

    public static DischargeSummaryPrintModel From(DischargeSummaryResponse data, byte[]? logoBytes)
    {
        var a = data.Admission;
        var form = data.Form;
        string S(string p) => Get(form, p);
        string N(string parent, string p) => GetNested(form, parent, p);

        var diagnosis = First(data.FinalDiagnosis, Pick(S("diagnosisKey"), S("diagnosisOther"), S("finalDiagnosis")));
        var catheter = S("catheterKey") == "Other" ? First(S("catheterOther")) : First(S("catheterKey"));
        var general = S("generalCondition") == "Other" ? First(S("generalConditionOther"), "Other") : First(S("generalCondition"));
        var postop = S("postopCourse") == "Complicated"
            ? "Complicated" + (string.IsNullOrWhiteSpace(S("postopCourseDetails")) ? "" : $" — {S("postopCourseDetails")}")
            : First(S("postopCourse"));
        var followPlan = S("followUpKey") == "Other" ? First(S("followUpOther")) : First(S("followUpKey"));
        var procedureKey = !string.IsNullOrWhiteSpace(S("procedureKey")) && S("procedureKey") != "Other"
            ? S("procedureKey")
            : First(S("procedureDetails"));

        var dos = data.DateOfSurgery?.ToString("dd MMM yyyy") ?? FormatDate(S("dateOfSurgery"));
        var dod = data.DateOfDischarge?.ToString("dd MMM yyyy") ?? FormatDate(S("dateOfDischarge"));
        if (string.IsNullOrWhiteSpace(dod) && a.DischargedAt.HasValue)
            dod = a.DischargedAt.Value.ToString("dd MMM yyyy, HH:mm");

        var leftKeys = new (string Key, string Label)[]
        {
            ("cbc", "CBC"), ("kft", "KFT"), ("urine", "Urine C/S"), ("usgCt", "USG KUB"), ("lft", "LFT")
        };
        var rightKeys = new (string Key, string Label)[]
        {
            ("vm", "Viral Markers"), ("rbs", "RBS"), ("bg", "BG"), ("cxrEcg", "Chest X-ray / ECG")
        };

        form.TryGetProperty("investigations", out var inv);
        form.TryGetProperty("investigationDates", out var dates);

        List<(string, string, string)> MapInv((string Key, string Label)[] keys) =>
            keys.Select(k =>
            {
                var result = inv.ValueKind == JsonValueKind.Object && inv.TryGetProperty(k.Key, out var r)
                    ? r.GetString() ?? "" : "";
                var date = dates.ValueKind == JsonValueKind.Object && dates.TryGetProperty(k.Key, out var d)
                    ? FormatDate(d.GetString() ?? "") : "";
                return (k.Label, result, date);
            }).ToList();

        var meds = new List<MedRow>();
        if (form.TryGetProperty("medicines", out var medArr) && medArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var m in medArr.EnumerateArray())
            {
                var name = Get(m, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                meds.Add(new MedRow(name, Get(m, "strength"), Get(m, "dose"), Get(m, "route"),
                    Get(m, "frequency"), Get(m, "duration"), Get(m, "timing")));
            }
        }

        static string[] Lines(string text) =>
            text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.TrimStart('-', '•', ' ').Trim())
                .Where(l => l.Length > 0)
                .ToArray();

        string JoinVitals(string parent) => string.Join(", ", new[]
        {
            Prefix("Pulse", N(parent, "pulse")),
            Prefix("BP", N(parent, "bp")),
            Prefix("Temp", N(parent, "temp")),
            Prefix("SpO₂", N(parent, "spo2"))
        }.Where(x => x != null)!);

        var parts = a.HospitalName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var letters = parts.Length >= 2
            ? $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}"
            : (parts.Length == 1 && parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : "JU");

        var genderShort = a.PatientGenderLabel switch
        {
            "Male" => "M",
            "Female" => "F",
            _ => a.PatientGenderLabel.Length > 0 ? a.PatientGenderLabel[0].ToString().ToUpperInvariant() : "—"
        };

        return new DischargeSummaryPrintModel
        {
            LogoBytes = logoBytes,
            LogoLetters = letters,
            HospitalName = a.HospitalName,
            HospitalAddress = a.HospitalAddress ?? "",
            HospitalPhone = a.HospitalPhone ?? "",
            CopyrightLine = ClinicalDocumentChrome.BuildCopyrightLine(a.HospitalName, a.HospitalAddress),
            DoctorName = ClinicalDocumentChrome.FormatDoctorName(a.DoctorName),
            DoctorDesignation = a.DoctorDesignation ?? "",
            CredentialLines = ClinicalDocumentChrome.CredentialLines(
                a.DoctorDesignation, a.DepartmentName ?? "Consultant"),
            DepartmentName = a.DepartmentName ?? "",
            PatientName = a.PatientFullName,
            AgeSex = a.PatientAge.HasValue ? $"{a.PatientAge}/{genderShort}" : $"—/{genderShort}",
            FatherName = S("fatherName"),
            PatientAddress = a.PatientAddress ?? "",
            AdmittedAt = a.AdmittedAt.ToString("dd MMM yyyy, HH:mm"),
            DischargedAt = dod,
            AdmissionCode = a.AdmissionCode,
            PatientCode = a.PatientCode,
            WardBed = $"{a.Ward} / {a.Bed ?? "—"}",
            PatientPhone = a.PatientPhone ?? "",
            SurgeryDate = dos,
            FinalDiagnosis = diagnosis,
            ChiefComplaints = S("chiefComplaints"),
            BriefHistory = S("briefHistory"),
            Comorbidities = N("pastHistory", "comorbidities"),
            PastSurgery = N("pastHistory", "pastSurgery"),
            Allergy = N("pastHistory", "allergy"),
            Addiction = N("pastHistory", "addiction"),
            ExamGeneral = N("examination", "general"),
            ExamVitals = JoinVitals("examination"),
            ExamSystemic = N("examination", "systemic"),
            ExamPerAbdomen = N("examination", "perAbdomen"),
            ExamGenitalia = N("examination", "genitalia"),
            InvLeft = MapInv(leftKeys),
            InvRight = MapInv(rightKeys),
            TreatmentLines = Lines(S("treatmentDuringAdmission")),
            ProcedureKey = procedureKey,
            ProcedureDetails = S("procedureDetails"),
            Findings = S("findings"),
            Anaesthesia = S("anaesthesia"),
            AssistantSurgeon = S("assistantSurgeon"),
            Indication = First(S("indication"), diagnosis),
            Specimen = S("specimen"),
            Catheter = catheter,
            IntraopComplications = S("intraopComplications"),
            PostopCourse = postop,
            GeneralCondition = general,
            DischargeVitals = JoinVitals("conditionExtras"),
            UrineOutput = N("conditionExtras", "urineOutput"),
            OralIntake = N("conditionExtras", "oralIntake"),
            Ambulation = N("conditionExtras", "ambulation"),
            DischargeType = S("dischargeType"),
            Medicines = meds,
            AdviceLines = Lines(S("advice")),
            FollowUpDate = FormatDate(S("followUpDate")),
            FollowUpDepartment = S("followUpDepartment"),
            FollowUpPlan = followPlan,
            FollowUpInvestigations = S("followUpInvestigations"),
            FollowUpNotes = First(S("followUpNotes"), S("followUp"))
        };
    }

    private static string Get(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v)
            ? v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()
            : "";

    private static string GetNested(JsonElement el, string parent, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(parent, out var p) ? Get(p, prop) : "";

    private static string First(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        return "";
    }

    private static string Pick(params string[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v) && !string.Equals(v, "Other", StringComparison.OrdinalIgnoreCase))
                return v.Trim();
        return First(values);
    }

    private static string? Prefix(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : $"{label} {value.Trim()}";

    private static string FormatDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        if (DateOnly.TryParse(raw, out var d)) return d.ToString("dd MMM yyyy");
        if (DateTime.TryParse(raw, out var dt)) return dt.ToString("dd MMM yyyy");
        return raw.Trim();
    }
}
