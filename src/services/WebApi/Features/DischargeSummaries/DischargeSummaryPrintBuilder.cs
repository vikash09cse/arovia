using System.Net;
using System.Text;
using System.Text.Json;
using WebApi.Features.Shared;

namespace WebApi.Features.DischargeSummaries;

internal static class DischargeSummaryPrintBuilder
{
    public static string Build(DischargeSummaryResponse data, byte[]? logoBytes = null)
    {
        var a = data.Admission;
        var form = data.Form;
        string S(string prop) => GetString(form, prop);
        string Nested(string parent, string prop) => GetNestedString(form, parent, prop);

        var name = a.PatientFullName;
        var ageSex = FormatAgeSex(a.PatientAge, a.PatientGenderLabel);
        var wardBed = $"{a.Ward} / {a.Bed ?? "—"}";
        var doa = a.AdmittedAt.ToString("dd MMM yyyy, HH:mm");
        var dosRaw = data.DateOfSurgery?.ToString("dd MMM yyyy")
            ?? FormatDate(S("dateOfSurgery"));
        var dodRaw = data.DateOfDischarge?.ToString("dd MMM yyyy")
            ?? FormatDate(S("dateOfDischarge"));
        if (string.IsNullOrWhiteSpace(dodRaw) && a.DischargedAt.HasValue)
            dodRaw = a.DischargedAt.Value.ToString("dd MMM yyyy, HH:mm");

        var diagnosis = FirstNonEmpty(data.FinalDiagnosis, Pick(S("diagnosisKey"), S("diagnosisOther"), S("finalDiagnosis")));
        var general = S("generalCondition") == "Other"
            ? FirstNonEmpty(S("generalConditionOther"), "Other")
            : FirstNonEmpty(S("generalCondition"), "—");
        var catheter = S("catheterKey") == "Other"
            ? FirstNonEmpty(S("catheterOther"), "—")
            : FirstNonEmpty(S("catheterKey"), "—");
        var postop = S("postopCourse") == "Complicated"
            ? "Complicated" + (string.IsNullOrWhiteSpace(S("postopCourseDetails")) ? "" : $" — {S("postopCourseDetails")}")
            : FirstNonEmpty(S("postopCourse"), "—");
        var followPlan = S("followUpKey") == "Other"
            ? FirstNonEmpty(S("followUpOther"), "—")
            : FirstNonEmpty(S("followUpKey"), "—");

        var vitalsAdmit = JoinNonEmpty(
            Prefix("Pulse", Nested("examination", "pulse")),
            Prefix("BP", Nested("examination", "bp")),
            Prefix("Temp", Nested("examination", "temp")),
            Prefix("SpO₂", Nested("examination", "spo2")));
        var vitalsDischarge = JoinNonEmpty(
            Prefix("Pulse", Nested("conditionExtras", "pulse")),
            Prefix("BP", Nested("conditionExtras", "bp")),
            Prefix("Temp", Nested("conditionExtras", "temp")),
            Prefix("SpO₂", Nested("conditionExtras", "spo2")));

        var adviceLines = SplitLines(S("advice"));
        var treatmentLines = SplitLines(S("treatmentDuringAdmission"))
            .Select(l => l.TrimStart('-', '•', ' ').Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        var doctor = ClinicalDocumentChrome.FormatDoctorName(a.DoctorName);
        var credentialLines = ClinicalDocumentChrome.CredentialLines(
            a.DoctorDesignation, a.DepartmentName ?? "Consultant");
        var copyright = ClinicalDocumentChrome.BuildCopyrightLine(a.HospitalName, a.HospitalAddress);

        var procedureLabel = !string.IsNullOrWhiteSpace(S("procedureKey")) && S("procedureKey") != "Other"
            ? S("procedureKey")
            : FirstNonEmpty(S("procedureDetails"), "—");

        var logoLetters = LogoLetters(a.HospitalName);
        var sb = new StringBuilder();
        sb.Append(Css(a.AdmissionCode));

        // Continuous document — browser/PDF print paginates naturally (no forced blank pages).
        sb.Append("""<div class="dsf-page">""");
        AppendLetterhead(sb, a.HospitalName, doctor, credentialLines, logoLetters, logoBytes);

        SectionOpen(sb, 1, "PATIENT DETAILS");
        sb.Append("""<div class="dsf-grid-2"><div>""");
        Kv(sb, "Patient Name", name);
        Kv(sb, "Age / Sex", ageSex);
        Kv(sb, "Father's Name", S("fatherName"));
        Kv(sb, "Address", a.PatientAddress);
        Kv(sb, "Date & Time of Admission", doa);
        Kv(sb, "Date & Time of Discharge", dodRaw);
        sb.Append("</div><div>");
        Kv(sb, "IPD No.", a.AdmissionCode);
        Kv(sb, "UHID", a.PatientCode);
        Kv(sb, "Ward / Bed", wardBed);
        Kv(sb, "Mobile No.", a.PatientPhone);
        Kv(sb, "Date & Time of Surgery", dosRaw);
        Kv(sb, "Treating Consultant", doctor);
        sb.Append("</div></div>");
        SectionClose(sb);

        SectionText(sb, 2, "FINAL DIAGNOSIS", diagnosis);
        SectionText(sb, 3, "CHIEF COMPLAINTS", S("chiefComplaints"));
        SectionText(sb, 4, "HISTORY OF PRESENT ILLNESS", S("briefHistory"));

        SectionOpen(sb, 5, "PAST HISTORY");
        sb.Append("""<div class="dsf-past">""");
        PastCell(sb, "Comorbidities", Nested("pastHistory", "comorbidities"));
        PastCell(sb, "Past Surgical History", Nested("pastHistory", "pastSurgery"));
        PastCell(sb, "Allergy", Nested("pastHistory", "allergy"));
        PastCell(sb, "Addiction", Nested("pastHistory", "addiction"));
        sb.Append("</div>");
        SectionClose(sb);

        SectionOpen(sb, 6, "EXAMINATION AT ADMISSION");
        Kv(sb, "General Examination", Nested("examination", "general"));
        Kv(sb, "Vitals", vitalsAdmit);
        Kv(sb, "Systemic Examination", Nested("examination", "systemic"));
        Kv(sb, "Per Abdomen", Nested("examination", "perAbdomen"));
        Kv(sb, "Genitalia", Nested("examination", "genitalia"));
        SectionClose(sb);

        SectionOpen(sb, 7, "INVESTIGATIONS");
        sb.Append(BuildInvestigations(form));
        SectionClose(sb);

        SectionOpen(sb, 8, "TREATMENT DURING ADMISSION");
        if (treatmentLines.Length == 0)
            sb.Append("""<p class="dsf-text">—</p>""");
        else
        {
            sb.Append("""<ul class="dsf-list">""");
            foreach (var line in treatmentLines)
                sb.Append($"<li>{E(line)}</li>");
            sb.Append("</ul>");
        }
        SectionClose(sb);

        SectionOpen(sb, 9, "PROCEDURE / SURGERY DETAILS");
        sb.Append("""<div class="dsf-grid-2"><div>""");
        Kv(sb, "Procedure", procedureLabel);
        Kv(sb, "Date of Surgery", dosRaw);
        Kv(sb, "Anaesthesia", S("anaesthesia"));
        sb.Append("</div><div>");
        Kv(sb, "Operating Surgeon", doctor);
        Kv(sb, "Assistant Surgeon", S("assistantSurgeon"));
        Kv(sb, "Indication", FirstNonEmpty(S("indication"), diagnosis));
        sb.Append("</div></div>");
        if (!string.IsNullOrWhiteSpace(S("procedureDetails")))
            sb.Append($"""<p class="dsf-text" style="margin-top:0.35rem">{E(S("procedureDetails"))}</p>""");
        SectionClose(sb);

        SectionText(sb, 10, "OPERATIVE FINDINGS", S("findings"));
        SectionText(sb, 11, "PROCEDURE DETAILS", S("procedureDetails"));

        SectionOpen(sb, 12, "INTRAOPERATIVE / POSTOPERATIVE COURSE");
        sb.Append("""<div class="dsf-grid-2"><div>""");
        Kv(sb, "Specimen Sent", S("specimen"));
        Kv(sb, "Drain / Catheter", catheter);
        sb.Append("</div><div>");
        Kv(sb, "Intraoperative Complications", S("intraopComplications"));
        Kv(sb, "Postoperative Course", postop);
        sb.Append("</div></div>");
        SectionClose(sb);

        SectionOpen(sb, 13, "CONDITION AT DISCHARGE");
        sb.Append("""<div class="dsf-grid-2"><div>""");
        Kv(sb, "General Condition", general);
        Kv(sb, "Vitals", vitalsDischarge);
        Kv(sb, "Urine Output", Nested("conditionExtras", "urineOutput"));
        Kv(sb, "Oral Intake", Nested("conditionExtras", "oralIntake"));
        sb.Append("</div><div>");
        Kv(sb, "Ambulation", Nested("conditionExtras", "ambulation"));
        Kv(sb, "PUC", catheter);
        Kv(sb, "Discharge Type", S("dischargeType"));
        sb.Append("</div></div>");
        SectionClose(sb);

        SectionOpen(sb, 14, "DISCHARGE MEDICATIONS");
        sb.Append(BuildMedicines(form));
        SectionClose(sb);

        sb.Append("""<div class="dsf-split">""");
        SectionOpen(sb, 15, "ADVICE / INSTRUCTIONS");
        if (adviceLines.Length == 0)
            sb.Append("""<p class="dsf-text">—</p>""");
        else
        {
            sb.Append("""<ul class="dsf-list">""");
            foreach (var line in adviceLines)
                sb.Append($"<li>{E(line.TrimStart('-', '•', ' ').Trim())}</li>");
            sb.Append("</ul>");
        }
        SectionClose(sb);

        SectionOpen(sb, 16, "FOLLOW-UP");
        Kv(sb, "Follow-up Date", FormatDate(S("followUpDate")));
        Kv(sb, "Follow-up Department", S("followUpDepartment"));
        Kv(sb, "Planned Procedure", followPlan);
        Kv(sb, "Investigations (if any)", S("followUpInvestigations"));
        Kv(sb, "Additional Advice", FirstNonEmpty(S("followUpNotes"), S("followUp")));
        SectionClose(sb);
        sb.Append("</div>");

        sb.Append($"""
            <div class="dsf-sign">
              <div class="dsf-sign-box">
                <div class="dsf-sign-line">Patient / Attendant Signature</div>
                <p style="margin:0.35rem 0 0">Name: ____________________</p>
                <p style="margin:0.2rem 0 0">Relationship: ______________</p>
                <p style="margin:0.2rem 0 0">Date: ____________________</p>
              </div>
              <div class="dsf-sign-box" style="text-align:right">
                <div class="dsf-sign-line" style="margin-left:auto;width:80%">Treating Doctor Signature &amp; Stamp</div>
                <p style="margin:0.45rem 0 0;font-weight:700">{E(doctor)}</p>
                <p style="margin:0.15rem 0 0;font-size:0.7rem">{E(string.Join(" · ", credentialLines))}</p>
                <p style="margin:0.15rem 0 0;font-size:0.7rem">{E(a.HospitalName)}</p>
              </div>
            </div>
            """);
        sb.Append(ClinicalDocumentChrome.FooterHtml(a.HospitalPhone, a.HospitalAddress, copyright));
        sb.Append("""
            </div></div></body></html>
            """);

        return sb.ToString();
    }

    private static string Css(string? admissionCode)
    {
        var title = E(string.IsNullOrWhiteSpace(admissionCode) ? "Discharge Summary" : admissionCode);
        return ("""
        <!DOCTYPE html>
        <html><head><meta charset="utf-8" />
        <title>__DOC_TITLE__</title>
        <style>
          :root {
            --dsf-navy: #1e3a5f;
            --dsf-navy-mid: #2b4c7e;
            --dsf-teal: #2d6a4f;
            --dsf-soft: #e8eef6;
            --dsf-title: #cfe0f5;
            --dsf-bar: #6b7c93;
            --dsf-line: #c5d0de;
          }
          * { box-sizing: border-box; }
          body {
            font-family: "Segoe UI", Arial, sans-serif;
            color: #111;
            margin: 0;
            padding: 12px;
            background: #eef2f6;
          }
          .dsf-root { color: #111; }
          .dsf-page {
            background: #fff;
            border: 1px solid var(--dsf-line);
            border-radius: 0.75rem;
            padding: 1.15rem 1.25rem 0.85rem;
            margin: 0 auto 1rem;
            max-width: 900px;
            box-shadow: 0 1px 2px rgb(0 0 0 / 0.04);
          }
          .dsf-bar {
            background: var(--dsf-navy);
            color: #fff;
            font-size: 0.72rem;
            font-weight: 700;
            letter-spacing: 0.03em;
            padding: 0.28rem 0.5rem;
            margin-top: 0.65rem;
          }
          .dsf-body {
            border: 1px solid var(--dsf-line);
            border-top: 0;
            padding: 0.45rem 0.55rem;
            font-size: 0.78rem;
            line-height: 1.4;
          }
          .dsf-grid-2 {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 0.15rem 1.25rem;
          }
          .dsf-kv { margin: 0.1rem 0; }
          .dsf-k { font-weight: 700; color: var(--dsf-navy); }
          .dsf-v { color: #111; }
          .dsf-text { white-space: pre-wrap; margin: 0; }
          .dsf-past {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 0;
            border: 1px solid var(--dsf-line);
          }
          .dsf-past > div {
            border-right: 1px solid var(--dsf-line);
            padding: 0.3rem 0.4rem;
            min-height: 2.4rem;
          }
          .dsf-past > div:last-child { border-right: 0; }
          .dsf-past-h {
            display: block;
            font-size: 0.65rem;
            font-weight: 700;
            color: var(--dsf-navy-mid);
            margin-bottom: 0.15rem;
          }
          .dsf-inv-wrap {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 0.5rem;
          }
          .dsf-inv, .dsf-med {
            width: 100%;
            border-collapse: collapse;
            font-size: 0.72rem;
          }
          .dsf-inv th, .dsf-inv td, .dsf-med th, .dsf-med td {
            border: 1px solid var(--dsf-line);
            padding: 0.2rem 0.35rem;
            text-align: left;
          }
          .dsf-inv th { background: var(--dsf-soft); font-weight: 700; color: var(--dsf-navy); }
          .dsf-inv tr:nth-child(even) td { background: #f7f9fc; }
          .dsf-med th { background: var(--dsf-navy); color: #fff; font-weight: 700; }
          .dsf-med tr:nth-child(even) td { background: #f3f6fa; }
          .dsf-list { margin: 0; padding-left: 1.1rem; }
          .dsf-list li { margin: 0.1rem 0; }
          .dsf-split {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 0.65rem;
            align-items: start;
          }
          .dsf-sign {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 1rem;
            margin-top: 1.25rem;
            font-size: 0.75rem;
          }
          .dsf-sign-box { min-height: 5.5rem; }
          .dsf-sign-line {
            margin-top: 2.2rem;
            border-top: 1px solid #333;
            padding-top: 0.25rem;
            font-weight: 600;
          }
          .dsf-page-no {
            text-align: right;
            font-size: 0.68rem;
            color: #667;
            margin-top: 0.35rem;
          }
          @media print {
            body { padding: 0; background: #fff; }
            .dsf-page {
              border: 0 !important;
              border-radius: 0 !important;
              box-shadow: none !important;
              padding: 8mm 10mm;
              margin: 0;
              max-width: none;
            }
            .cdc-letterhead-wrap {
              break-inside: avoid;
              page-break-inside: avoid;
            }
          }
          @media (max-width: 720px) {
            .dsf-grid-2, .dsf-inv-wrap, .dsf-split, .dsf-sign, .dsf-past, .cdc-letterhead {
              grid-template-columns: 1fr;
            }
            .dsf-past > div { border-right: 0; border-bottom: 1px solid var(--dsf-line); }
          }
        """ + ClinicalDocumentChrome.CssBlock() + """
        </style></head><body><div class="dsf-root">
        """).Replace("__DOC_TITLE__", title, StringComparison.Ordinal);
    }

    private static void AppendLetterhead(
        StringBuilder sb,
        string hospitalName,
        string doctor,
        IReadOnlyList<string> credentialLines,
        string logoLetters,
        byte[]? logoBytes)
    {
        string? logoHtml = null;
        if (logoBytes is { Length: > 0 })
        {
            var b64 = Convert.ToBase64String(logoBytes);
            logoHtml = $"""<img class="cdc-logo" src="data:image/png;base64,{b64}" alt="Logo" />""";
        }

        sb.Append(ClinicalDocumentChrome.LetterheadHtml(
            hospitalName,
            doctor,
            credentialLines,
            "DISCHARGE SUMMARY",
            logoHtml,
            logoLetters));
    }

    private static void SectionOpen(StringBuilder sb, int n, string title) =>
        sb.Append($"""<section><div class="dsf-bar">{n}. {E(title)}</div><div class="dsf-body">""");

    private static void SectionClose(StringBuilder sb) => sb.Append("</div></section>");

    private static void SectionText(StringBuilder sb, int n, string title, string? text)
    {
        SectionOpen(sb, n, title);
        sb.Append($"""<p class="dsf-text">{Dash(text)}</p>""");
        SectionClose(sb);
    }

    private static void Kv(StringBuilder sb, string label, string? value) =>
        sb.Append($"""<div class="dsf-kv"><span class="dsf-k">{E(label)}:</span> <span class="dsf-v">{Dash(value)}</span></div>""");

    private static void PastCell(StringBuilder sb, string label, string? value) =>
        sb.Append($"""<div><span class="dsf-past-h">{E(label)}</span>{Dash(value)}</div>""");

    private static string BuildMedicines(JsonElement form)
    {
        if (!form.TryGetProperty("medicines", out var meds) || meds.ValueKind != JsonValueKind.Array)
            return """<p class="dsf-text">—</p>""";

        var rows = new StringBuilder();
        var i = 0;
        foreach (var m in meds.EnumerateArray())
        {
            var name = GetString(m, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            i++;
            rows.Append($"""
                <tr>
                  <td>{i}</td>
                  <td>{E(name)}</td>
                  <td>{Dash(GetString(m, "strength"))}</td>
                  <td>{Dash(GetString(m, "dose"))}</td>
                  <td>{Dash(GetString(m, "route"))}</td>
                  <td>{Dash(GetString(m, "frequency"))}</td>
                  <td>{Dash(GetString(m, "duration"))}</td>
                  <td>{Dash(GetString(m, "timing"))}</td>
                </tr>
                """);
        }

        if (i == 0) return """<p class="dsf-text">—</p>""";

        return """
            <table class="dsf-med">
              <thead><tr>
                <th>S. No.</th><th>Medicine</th><th>Strength</th><th>Dose</th>
                <th>Route</th><th>Frequency</th><th>Duration</th><th>Timing / Instructions</th>
              </tr></thead><tbody>
            """ + rows + "</tbody></table>";
    }

    private static string BuildInvestigations(JsonElement form)
    {
        var left = new (string Key, string Label)[]
        {
            ("cbc", "CBC"), ("kft", "KFT"), ("urine", "Urine C/S"), ("usgCt", "USG KUB"), ("lft", "LFT")
        };
        var right = new (string Key, string Label)[]
        {
            ("vm", "Viral Markers"), ("rbs", "RBS"), ("bg", "BG"), ("cxrEcg", "Chest X-ray / ECG")
        };

        form.TryGetProperty("investigations", out var inv);
        form.TryGetProperty("investigationDates", out var dates);

        string Table((string Key, string Label)[] keys)
        {
            var sb = new StringBuilder();
            sb.Append("""<table class="dsf-inv"><thead><tr><th>Test</th><th>Result</th><th>Date</th></tr></thead><tbody>""");
            foreach (var (key, label) in keys)
            {
                var result = inv.ValueKind == JsonValueKind.Object && inv.TryGetProperty(key, out var r)
                    ? r.GetString() ?? ""
                    : "";
                var date = dates.ValueKind == JsonValueKind.Object && dates.TryGetProperty(key, out var d)
                    ? FormatDate(d.GetString() ?? "")
                    : "";
                sb.Append($"<tr><td>{E(label)}</td><td>{Dash(result)}</td><td>{Dash(date)}</td></tr>");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        return $"""<div class="dsf-inv-wrap">{Table(left)}{Table(right)}</div>""";
    }

    private static string LogoLetters(string hospitalName)
    {
        var parts = hospitalName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        if (parts.Length == 1 && parts[0].Length >= 2)
            return parts[0][..2].ToUpperInvariant();
        return "JU";
    }

    private static string FormatAgeSex(int? age, string genderLabel)
    {
        var g = genderLabel switch
        {
            "Male" => "M",
            "Female" => "F",
            _ => genderLabel.Length > 0 ? genderLabel[0].ToString().ToUpperInvariant() : "—"
        };
        return age.HasValue ? $"{age}/{g}" : $"—/{g}";
    }

    private static string FormatDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        if (DateOnly.TryParse(raw, out var d)) return d.ToString("dd MMM yyyy");
        if (DateTime.TryParse(raw, out var dt)) return dt.ToString("dd MMM yyyy");
        return raw.Trim();
    }

    private static string[] SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string JoinNonEmpty(params string?[] parts) =>
        string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p))!);

    private static string? Prefix(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : $"{label} {value.Trim()}";

    private static string GetString(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v)
            ? v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()
            : "";

    private static string GetNestedString(JsonElement el, string parent, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(parent, out var p)
            ? GetString(p, prop)
            : "";

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        return "—";
    }

    private static string Pick(params string[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v) && !string.Equals(v, "Other", StringComparison.OrdinalIgnoreCase))
                return v.Trim();
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        return "";
    }

    private static string E(string? v) => WebUtility.HtmlEncode(v ?? "");

    private static string Dash(string? v)
    {
        var t = (v ?? "").Trim();
        return string.IsNullOrEmpty(t) ? "—" : E(t);
    }
}
