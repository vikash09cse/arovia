using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApi.Features.Payments;

public static class ReceiptPdfBuilder
{
    static ReceiptPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(ReceiptData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(34);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(1.2f).Row(brand =>
                        {
                            if (data.LogoBytes is { Length: > 0 })
                            {
                                brand.ConstantItem(54).Height(54)
                                    .Border(1).BorderColor(Colors.Teal.Lighten2)
                                    .Background(Colors.White)
                                    .Padding(2)
                                    .Image(data.LogoBytes).FitArea();
                            }
                            else
                            {
                                brand.ConstantItem(54).Height(54).Border(1).BorderColor(Colors.Teal.Darken2)
                                    .Background(Colors.Teal.Lighten5)
                                    .AlignCenter().AlignMiddle()
                                    .Text(string.IsNullOrWhiteSpace(data.HospitalName) ? "H" : data.HospitalName[..1])
                                    .Bold().FontSize(18).FontColor(Colors.Teal.Darken2);
                            }
                            brand.RelativeItem().PaddingLeft(10).Column(c =>
                            {
                                c.Item().Text(data.HospitalName).Bold().FontSize(16).FontFamily(Fonts.TimesNewRoman)
                                    .FontColor(Colors.Teal.Darken3);
                                c.Item().PaddingTop(4).Text("Advanced Urology & Stone Care Centre").FontSize(8)
                                    .FontColor(Colors.Teal.Darken1);
                            });
                        });

                        row.RelativeItem(1.25f).BorderLeft(1).BorderColor(Colors.Teal.Lighten2).PaddingLeft(12).Column(c =>
                        {
                            c.Item().Text(data.DoctorName).Bold().FontSize(11);
                            if (!string.IsNullOrWhiteSpace(data.DoctorDesignation))
                                c.Item().Text(data.DoctorDesignation).FontSize(9);
                            c.Item().PaddingTop(6).Text("Consultant Urologist").SemiBold().FontSize(9)
                                .FontColor(Colors.Teal.Darken2);
                            c.Item().PaddingTop(3).Text("Endourology | Laparoscopy | Kidney Stone Laser").FontSize(8);
                            c.Item().Text("Prostate Care | Male Sexual Health | Uro-Oncology").FontSize(8);
                        });

                        row.RelativeItem().PaddingLeft(10).Column(c =>
                        {
                            c.Item().Text(data.HospitalAddress).FontSize(9);
                            c.Item().PaddingTop(4).Text($"Phone: {data.HospitalPhone}").FontSize(9);
                            c.Item().PaddingTop(4).Text($"Web: {data.Website}").FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(2).LineColor(Colors.Teal.Darken2);

                    col.Item().PaddingVertical(12).Row(r =>
                    {
                        r.RelativeItem().AlignMiddle().LineHorizontal(1).LineColor(Colors.Teal.Darken2);
                        r.ConstantItem(120).Border(1.5f).BorderColor(Colors.Teal.Darken2)
                            .Background(Colors.Teal.Lighten5)
                            .PaddingVertical(5).AlignCenter()
                            .Text("RECEIPT").Bold().FontSize(12).FontColor(Colors.Teal.Darken3);
                        r.RelativeItem().AlignMiddle().LineHorizontal(1).LineColor(Colors.Teal.Darken2);
                    });

                    col.Item().Row(meta =>
                    {
                        meta.RelativeItem().Column(left =>
                        {
                            MetaLine(left, "Receipt No.", data.ReceiptNumber);
                            MetaLine(left, "Patient ID", data.PatientCode);
                            MetaLine(left, "Date", data.VisitDate);
                            MetaLine(left, "Time", data.VisitTime);
                        });
                        meta.RelativeItem().PaddingLeft(20).Column(right =>
                        {
                            MetaLine(right, "Patient Name", data.PatientName);
                            MetaLine(right, "Age / Sex", $"{data.Age} / {data.Gender}");
                            MetaLine(right, "Mobile No.", data.Phone);
                            MetaLine(right, "Address", data.Address);
                        });
                    });

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(55);
                            c.RelativeColumn();
                            c.ConstantColumn(110);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Teal.Lighten4).BorderTop(1.5f).BorderBottom(1.5f)
                                .BorderColor(Colors.Teal.Darken2).Padding(7).AlignCenter().Text("S. No.").Bold().FontSize(9);
                            h.Cell().Background(Colors.Teal.Lighten4).BorderTop(1.5f).BorderBottom(1.5f)
                                .BorderColor(Colors.Teal.Darken2).Padding(7).Text("PARTICULARS").Bold().FontSize(9);
                            h.Cell().Background(Colors.Teal.Lighten4).BorderTop(1.5f).BorderBottom(1.5f)
                                .BorderColor(Colors.Teal.Darken2).Padding(7).AlignRight().Text("AMOUNT (₹)").Bold().FontSize(9);
                        });

                        var feeLines = data.FeeLines.Count > 0
                            ? data.FeeLines
                            :
                            [
                                new ReceiptFeeLine("Consultation Fee", data.ConsultationFee),
                                new ReceiptFeeLine("Procedure Charges", data.ProcedureCharge),
                                new ReceiptFeeLine("Add-on Charges", data.AddonCharges),
                                new ReceiptFeeLine("Discount", data.Discount)
                            ];

                        for (var i = 0; i < feeLines.Count; i++)
                        {
                            FeeRow(table, (i + 1).ToString(), feeLines[i].Label, feeLines[i].Amount, last: i == feeLines.Count - 1);
                        }
                    });

                    col.Item().BorderTop(1.5f).BorderBottom(1.5f).BorderColor(Colors.Teal.Darken2)
                        .Background(Colors.Teal.Lighten5).Row(total =>
                    {
                        total.RelativeItem().Padding(9).Text("TOTAL PAID").Bold().FontSize(12)
                            .FontColor(Colors.Teal.Darken3);
                        total.ConstantItem(110).Padding(9).AlignRight().Text($"₹ {data.TotalPaid}").Bold().FontSize(12)
                            .FontColor(Colors.Teal.Darken3);
                    });

                    col.Item().PaddingTop(14).Row(footer =>
                    {
                        footer.RelativeItem().Border(1).BorderColor(Colors.Teal.Lighten2).Padding(9).Column(pay =>
                        {
                            pay.Item().Text("PAYMENT MODE").Bold().FontSize(9).FontColor(Colors.Teal.Darken2);
                            pay.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Text($"{Check(data.CashMark)} Cash");
                                r.RelativeItem().Text($"{Check(data.UpiMark)} UPI");
                            });
                            pay.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text($"{Check(data.CardMark)} Card");
                                r.RelativeItem().Text($"{Check(data.OtherMark)} Other {data.OtherPaymentDetail}");
                            });
                        });

                        footer.RelativeItem().PaddingHorizontal(10).AlignCenter().Column(mid =>
                        {
                            mid.Item().Row(r =>
                            {
                                r.AutoItem().Text("Received By : ");
                                r.RelativeItem().BorderBottom(1).PaddingLeft(4).Text(data.CollectedBy);
                            });
                            mid.Item().PaddingTop(12).Text("Thank You!").Italic().FontSize(16)
                                .FontColor(Colors.Teal.Darken2);
                            mid.Item().PaddingTop(4)
                                .Text($"For choosing {data.HospitalName}. We wish you good health.").FontSize(8);
                        });

                        footer.RelativeItem().Border(1).BorderColor(Colors.Teal.Lighten2).Padding(9).Column(appt =>
                        {
                            appt.Item().Text($"For Appointment").Bold().FontSize(9).FontColor(Colors.Teal.Darken2);
                            appt.Item().Text(data.HospitalPhone).FontSize(9);
                            appt.Item().PaddingTop(8).Text("Timings: 10:00 AM - 8:00 PM").FontSize(8);
                            appt.Item().Text("(Monday to Saturday)").FontSize(8);
                        });
                    });

                    col.Item().PaddingTop(14).AlignCenter().Column(s =>
                    {
                        s.Item().LineHorizontal(1).LineColor(Colors.Teal.Lighten2);
                        s.Item().PaddingTop(8).Text("Better Urology, Better Life.").Italic().FontSize(10)
                            .FontColor(Colors.Teal.Darken2);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static string Check(string mark) =>
        !string.IsNullOrWhiteSpace(mark) && (mark.Contains('✓') || mark is "X" or "x") ? "☑" : "☐";

    private static void MetaLine(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(6).Row(r =>
        {
            r.ConstantItem(90).Text(label).SemiBold();
            r.ConstantItem(10).Text(":");
            r.RelativeItem().BorderBottom(0.7f).BorderColor(Colors.Grey.Lighten1).PaddingLeft(4).Text(value);
        });
    }

    private static void FeeRow(TableDescriptor table, string sno, string label, string amount, bool last = false)
    {
        var bottom = last ? 1.5f : 0.6f;
        table.Cell().BorderBottom(bottom).BorderColor(Colors.Grey.Lighten2).Padding(7).AlignCenter().Text(sno);
        table.Cell().BorderBottom(bottom).BorderColor(Colors.Grey.Lighten2).Padding(7).Text(label);
        table.Cell().BorderBottom(bottom).BorderColor(Colors.Grey.Lighten2).Padding(7).AlignRight().Text(amount);
    }
}

public sealed class ReceiptFeeLine(string label, string amount)
{
    public string Label { get; } = label;
    public string Amount { get; } = amount;
}

public sealed class ReceiptData
{
    public string HospitalName { get; init; } = "";
    public string HospitalAddress { get; init; } = "";
    public string HospitalPhone { get; init; } = "";
    public string Website { get; init; } = "";
    public string LogoHtml { get; init; } = "";
    public byte[]? LogoBytes { get; init; }
    public string ReceiptHeader { get; init; } = "";
    public string ReceiptFooter { get; init; } = "";
    public string ReceiptNumber { get; init; } = "";
    public string VisitCode { get; init; } = "";
    public string VisitDate { get; init; } = "";
    public string VisitTime { get; init; } = "";
    public string DoctorName { get; init; } = "";
    public string DoctorDesignation { get; init; } = "";
    public string DoctorSpecialties { get; init; } = "";
    public string PatientName { get; init; } = "";
    public string PatientCode { get; init; } = "";
    public string Age { get; init; } = "";
    public string Gender { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Address { get; init; } = "";
    public string ConsultationFee { get; init; } = "";
    public string ProcedureCharge { get; init; } = "";
    public string AddonCharges { get; init; } = "";
    public IReadOnlyList<ReceiptFeeLine> FeeLines { get; init; } = [];
    public string FeeRowsHtml { get; init; } = "";
    public string Discount { get; init; } = "";
    public string TotalPaid { get; init; } = "";
    public string PaymentMode { get; init; } = "";
    public string CashMark { get; init; } = "";
    public string UpiMark { get; init; } = "";
    public string CardMark { get; init; } = "";
    public string OtherMark { get; init; } = "";
    public string OtherPaymentDetail { get; init; } = "";
    public string CollectedBy { get; init; } = "";
    public Guid PaymentId { get; init; }
}
