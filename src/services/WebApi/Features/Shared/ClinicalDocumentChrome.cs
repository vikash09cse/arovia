using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApi.Features.Shared;

/// <summary>
/// Shared Janak-style letterhead + footer used by discharge summary and final invoice.
/// Target layout (Image 1): logo L+R, centered brand with lime URO, black rule,
/// doctor credentials under rule, then light-blue title bar with white text.
/// </summary>
public static class ClinicalDocumentChrome
{
    public static readonly string UroLime = "#7CB342";
    public static readonly string TitleBarBlue = "#5BA3D9";
    public static readonly string FooterRed = "#C62828";

    public static string FormatDoctorName(string? firstOrFull, string? lastName = null)
    {
        var full = string.IsNullOrWhiteSpace(lastName)
            ? (firstOrFull ?? "").Trim()
            : $"{firstOrFull} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(full)) return "";
        if (full.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase)
            || full.StartsWith("Dr ", StringComparison.OrdinalIgnoreCase))
            return full;
        return $"Dr. {full}";
    }

    public static IReadOnlyList<string> CredentialLines(string? designation, string? fallback = null)
    {
        var raw = string.IsNullOrWhiteSpace(designation) ? (fallback ?? "") : designation!;
        if (string.IsNullOrWhiteSpace(raw)) return [];

        var lines = raw
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 1 && lines[0].Contains(" / ", StringComparison.Ordinal))
        {
            return lines[0]
                .Split(" / ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }

        // "MBBS (…), MS (…), MCh (…)" → one line per degree
        if (lines.Count == 1)
        {
            var degreeSplit = Regex.Split(
                lines[0],
                @"(?<=\))\s*,\s*(?=[A-Za-z])");
            if (degreeSplit.Length > 1)
                return degreeSplit.Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        }

        return lines;
    }

    public static string BuildCopyrightLine(string hospitalName, string? address)
    {
        var year = DateTime.UtcNow.Year;
        var city = TryExtractLocality(address);
        var brand = string.IsNullOrWhiteSpace(city)
            ? hospitalName.Trim()
            : $"{hospitalName.Trim()}, {city}";
        return $"© {year} {brand} | All Rights Reserve";
    }

    public static string BrandTitleHtml(string hospitalName)
    {
        var display = (hospitalName ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(display)) display = "HOSPITAL";

        var idx = display.IndexOf("URO", StringComparison.Ordinal);
        if (idx < 0)
            return $"<span class=\"cdc-brand-black\">{Enc(display)}</span>";

        var before = display[..idx];
        var uro = display.Substring(idx, 3);
        var after = display[(idx + 3)..];
        return $"<span class=\"cdc-brand-black\">{Enc(before)}</span>"
             + $"<span class=\"cdc-brand-uro\">{Enc(uro)}</span>"
             + $"<span class=\"cdc-brand-black\">{Enc(after)}</span>";
    }

    /// <summary>
    /// Invoice letterhead brand: keeps tenant name casing, highlights "Uro" in lime.
    /// </summary>
    public static string InvoiceBrandTitleHtml(string hospitalName)
    {
        var display = (hospitalName ?? "").Trim();
        if (string.IsNullOrEmpty(display)) display = "Hospital";

        var idx = display.IndexOf("Uro", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return $"<span class=\"cdc-brand-black\">{Enc(display)}</span>";

        // Highlight from "Uro" through end of that token (e.g. UroCare / URO CARE)
        var end = idx + 3;
        while (end < display.Length && !char.IsWhiteSpace(display[end]) && display[end] != '|')
            end++;

        var before = display[..idx];
        var uroPart = display[idx..end];
        var after = display[end..];
        return $"<span class=\"cdc-brand-black\">{Enc(before)}</span>"
             + $"<span class=\"cdc-brand-uro\">{Enc(uroPart)}</span>"
             + $"<span class=\"cdc-brand-black\">{Enc(after)}</span>";
    }

    public static string CssBlock() => """
        .cdc-letterhead-wrap { margin-bottom: 0; }
        .cdc-letterhead {
          display: grid;
          grid-template-columns: 110px 1fr 110px;
          gap: 8px;
          align-items: start;
        }
        .cdc-logo-cell { text-align: center; }
        .cdc-logo, .cdc-logo-fallback {
          width: 100px; height: 100px; object-fit: contain;
          display: block; margin: 0 auto;
        }
        .cdc-logo-fallback {
          border: 2px solid #7CB342; border-radius: 8px;
          background: #f7fbe9; color: #7CB342;
          font-weight: 800; font-size: 22px;
          display: flex; align-items: center; justify-content: center;
        }
        .cdc-brand-center { text-align: center; padding-top: 2px; }
        .cdc-brand-title {
          font-family: Arial, Helvetica, sans-serif;
          font-size: 30px; font-weight: 800; letter-spacing: 0.4px; line-height: 1.05;
        }
        .cdc-brand-black { color: #111; }
        .cdc-brand-uro { color: #7CB342; }
        .cdc-brand-rule {
          width: 72%; max-width: 280px; margin: 6px auto 8px;
          border: 0; border-top: 1.5px solid #111;
        }
        .cdc-doctor-name {
          margin: 0; font-size: 13px; font-weight: 700; color: #111;
        }
        .cdc-doctor-cred {
          margin-top: 2px; font-size: 11px; color: #222; line-height: 1.35;
        }
        .cdc-title-bar {
          margin-top: 10px;
          background: #5BA3D9;
          color: #fff;
          text-align: center;
          font-size: 15px;
          font-weight: 800;
          letter-spacing: 1px;
          padding: 7px 10px;
          text-transform: uppercase;
        }
        .cdc-page-footer {
          margin-top: 18px; text-align: center; line-height: 1.45;
        }
        .cdc-phone {
          font-size: 13px; font-weight: 700; color: #111;
        }
        .cdc-phone-icon { color: #C62828; margin-right: 4px; }
        .cdc-address {
          margin-top: 4px; font-size: 11px; color: #333;
        }
        .cdc-copy {
          margin-top: 4px; font-size: 10px; color: #C62828; font-weight: 600;
        }
        """;

    public static string LetterheadHtml(
        string hospitalName,
        string doctorName,
        IReadOnlyList<string> credentialLines,
        string documentTitle,
        string? logoHtml,
        string logoFallbackLetter,
        bool includeDocumentTitle = true)
    {
        var logo = string.IsNullOrWhiteSpace(logoHtml)
            ? $"<div class=\"cdc-logo-fallback\">{Enc(logoFallbackLetter)}</div>"
            : logoHtml;

        // Force logo class so both sides render the same size.
        if (!string.IsNullOrWhiteSpace(logoHtml) && !logoHtml.Contains("cdc-logo", StringComparison.OrdinalIgnoreCase))
            logo = logoHtml.Replace("class=\"", "class=\"cdc-logo ", StringComparison.OrdinalIgnoreCase);

        var cred = new StringBuilder();
        foreach (var line in credentialLines)
            cred.Append($"<div class=\"cdc-doctor-cred\">{Enc(line)}</div>");

        var titleBar = includeDocumentTitle && !string.IsNullOrWhiteSpace(documentTitle)
            ? $"""<div class="cdc-title-bar">{Enc(documentTitle)}</div>"""
            : "";

        return $"""
            <header class="cdc-letterhead-wrap">
              <div class="cdc-letterhead">
                <div class="cdc-logo-cell">{logo}</div>
                <div class="cdc-brand-center">
                  <div class="cdc-brand-title">{BrandTitleHtml(hospitalName)}</div>
                  <hr class="cdc-brand-rule" />
                  <div class="cdc-doctor-name">{Enc(doctorName)}</div>
                  {cred}
                </div>
                <div class="cdc-logo-cell">{logo}</div>
              </div>
              {titleBar}
            </header>
            """;
    }

    public static string DocumentTitleBarHtml(string documentTitle) =>
        string.IsNullOrWhiteSpace(documentTitle)
            ? ""
            : $"""<div class="cdc-title-bar">{Enc(documentTitle)}</div>""";

    public static string FooterHtml(string? phone, string? address, string copyrightLine)
    {
        var phoneLine = string.IsNullOrWhiteSpace(phone)
            ? ""
            : $"""<div class="cdc-phone"><span class="cdc-phone-icon">☎</span>{Enc(phone.Trim())}</div>""";
        var addrLine = string.IsNullOrWhiteSpace(address)
            ? ""
            : $"""<div class="cdc-address">{Enc(address.Trim())}</div>""";

        return $"""
            <footer class="cdc-page-footer">
              {phoneLine}
              {addrLine}
              <div class="cdc-copy">{Enc(copyrightLine)}</div>
            </footer>
            """;
    }

    public static void ComposeLetterhead(
        IContainer container,
        string hospitalName,
        string doctorName,
        IReadOnlyList<string> credentialLines,
        string documentTitle,
        byte[]? logoBytes,
        string logoFallbackLetter,
        bool includeDocumentTitle = true)
    {
        container.Column(col =>
        {
            // Three columns: logo | brand+doctor | logo
            col.Item().Row(row =>
            {
                row.ConstantItem(86).Height(86).Element(e => DrawLogo(e, logoBytes, logoFallbackLetter));

                row.RelativeItem().PaddingHorizontal(6).AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Element(e => ComposeBrandTitle(e, hospitalName));

                    // Black rule under brand title only (centered)
                    c.Item().PaddingTop(4).AlignCenter().Width(200)
                        .LineHorizontal(1.2f).LineColor(Colors.Black);

                    c.Item().PaddingTop(5).AlignCenter()
                        .Text(doctorName).Bold().FontSize(10.5f).FontColor(Colors.Black);

                    foreach (var line in credentialLines)
                    {
                        c.Item().AlignCenter()
                            .Text(line).FontSize(8.5f).FontColor(Colors.Grey.Darken3);
                    }
                });

                row.ConstantItem(86).Height(86).Element(e => DrawLogo(e, logoBytes, logoFallbackLetter));
            });

            if (includeDocumentTitle && !string.IsNullOrWhiteSpace(documentTitle))
                col.Item().PaddingTop(6).Element(c => ComposeDocumentTitleBar(c, documentTitle));
        });
    }

    public static void ComposeDocumentTitleBar(IContainer container, string documentTitle)
    {
        container.Background(Color.FromHex(TitleBarBlue))
            .PaddingVertical(6).AlignCenter()
            .Text(documentTitle).Bold().FontSize(11)
            .FontColor(Colors.White).LetterSpacing(0.8f);
    }

    public static void ComposeFooter(
        IContainer container,
        string? phone,
        string? address,
        string copyrightLine,
        string? pageLabel = null)
    {
        container.Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                col.Item().AlignCenter().Text(text =>
                {
                    text.Span("☎ ").FontColor(Color.FromHex(FooterRed)).FontSize(10);
                    text.Span(phone.Trim()).Bold().FontSize(10).FontColor(Colors.Black);
                });
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                col.Item().PaddingTop(2).AlignCenter()
                    .Text(address.Trim()).FontSize(8.5f).FontColor(Colors.Grey.Darken3);
            }

            col.Item().PaddingTop(2).AlignCenter()
                .Text(copyrightLine).FontSize(8).FontColor(Color.FromHex(FooterRed));

            if (!string.IsNullOrWhiteSpace(pageLabel))
            {
                col.Item().PaddingTop(3).AlignRight()
                    .Text(pageLabel).FontSize(8).FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private static void ComposeBrandTitle(IContainer container, string hospitalName)
    {
        var display = (hospitalName ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(display)) display = "HOSPITAL";

        var idx = display.IndexOf("URO", StringComparison.Ordinal);
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(24).Bold());
            if (idx < 0)
            {
                text.Span(display).FontColor(Colors.Black);
                return;
            }

            if (idx > 0)
                text.Span(display[..idx]).FontColor(Colors.Black);
            text.Span(display.Substring(idx, 3)).FontColor(Color.FromHex(UroLime));
            if (idx + 3 < display.Length)
                text.Span(display[(idx + 3)..]).FontColor(Colors.Black);
        });
    }

    private static void DrawLogo(IContainer box, byte[]? logoBytes, string fallbackLetter)
    {
        if (logoBytes is { Length: > 0 })
        {
            box.Image(logoBytes).FitArea();
            return;
        }

        box.Border(2).BorderColor(Color.FromHex(UroLime))
            .Background("#f7fbe9")
            .AlignCenter().AlignMiddle()
            .Text(string.IsNullOrWhiteSpace(fallbackLetter) ? "H" : fallbackLetter[..1])
            .Bold().FontSize(20).FontColor(Color.FromHex(UroLime));
    }

    private static string? TryExtractLocality(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        var pinMatch = Regex.Match(
            address,
            @"([^\d,，،|/]+?)\s*[–—\-]\s*\d{5,6}\s*$",
            RegexOptions.CultureInvariant);
        if (pinMatch.Success)
            return pinMatch.Groups[1].Value.Trim().Trim(',', ' ');

        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            var last = parts[^1];
            var withoutPin = Regex.Replace(last, @"\d{5,6}", "").Trim(' ', '-', '–', '—');
            if (!string.IsNullOrWhiteSpace(withoutPin))
                return withoutPin;
            return parts[^2];
        }

        return null;
    }

    private static string Enc(string? value) =>
        WebUtility.HtmlEncode(value ?? "");
}
