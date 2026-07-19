using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using SharedKernel.Utilities.Helpers;
using WebApi.Features.Payments.Infrastructure;

namespace WebApi.Features.Payments;

public class PaymentsService(
    IPaymentsRepository repository,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment,
    PhiEncryptionHelper encryption)
{
    public async Task<Result<PaymentListResponse>> GetListAsync(
        int page,
        int pageSize,
        string? patientCode,
        bool openVisitsOnly,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        var tenantError = RequireTenantContext<PaymentListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (items, total) = await repository.GetListAsync(
            tenantId,
            page,
            pageSize,
            string.IsNullOrWhiteSpace(patientCode) ? null : patientCode.Trim(),
            openVisitsOnly,
            dateFrom,
            dateTo,
            ct);

        var mapped = items.Select(MapListItem);
        return Result<PaymentListResponse>.Ok(new PaymentListResponse(mapped, total, page, pageSize));
    }

    public async Task<Result<AddPaymentResponse>> AddCollectionAsync(
        Guid visitId, AddPaymentRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Amount must be greater than zero.");

        if (request.CollectedByUserId == Guid.Empty)
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Payment collector is required.");

        if (request.Notes?.Length > 500)
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Notes cannot exceed 500 characters.");

        if (request.PaymentMethod is not null
            && request.PaymentMethod is not ((byte)PaymentMethod.Cash) and not ((byte)PaymentMethod.Upi)
                and not ((byte)PaymentMethod.BankAccount) and not ((byte)PaymentMethod.Cheque))
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Invalid payment mode.");

        var tenantError = RequireTenantContext<AddPaymentResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var result = await repository.AddCollectionAsync(
            tenantId,
            visitId,
            request.Amount,
            request.CollectedByUserId,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            request.PaymentMethod,
            GetUserId(),
            ct);

        if (result == null)
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Unable to record payment collection.");

        return Result<AddPaymentResponse>.Ok(new AddPaymentResponse(
            result.PaymentId,
            result.ReceiptNumber,
            result.Amount,
            result.TotalDue,
            result.TotalCollected,
            result.BalanceDue), "Payment recorded.");
    }

    public async Task<Result<bool>> VoidCollectionAsync(
        Guid paymentId, VoidPaymentRequest request, CancellationToken ct)
    {
        if (request.Reason?.Length > 500)
            return Result<bool>.Fail(ErrorCode.Validation, "Reason cannot exceed 500 characters.");

        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.VoidCollectionAsync(
            tenantId,
            paymentId,
            string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            GetUserId(),
            ct);

        return Result<bool>.Ok(true, "Payment voided.");
    }

    public async Task<Result<int>> CollectVisitPendingAsync(
        Guid visitId, CollectVisitPendingRequest? request, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<int>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var count = await repository.CollectVisitPendingAsync(
            tenantId, visitId, request?.CollectedByUserId, GetUserId(), ct);
        return Result<int>.Ok(count, count > 0 ? "Remaining balance collected." : "No balance due to collect.");
    }

    public async Task<Result<PaymentReceiptHtmlResponse>> GetReceiptHtmlAsync(Guid paymentId, CancellationToken ct)
    {
        var built = await BuildReceiptAsync(paymentId, ct);
        if (built.Error != null) return built.Error;
        return Result<PaymentReceiptHtmlResponse>.Ok(new PaymentReceiptHtmlResponse(
            built.Data!.PaymentId,
            built.Data.ReceiptNumber,
            built.Html!));
    }

    public async Task<Result<(byte[] Bytes, string ReceiptNumber)>> GetReceiptPdfAsync(Guid paymentId, CancellationToken ct)
    {
        var built = await BuildReceiptAsync(paymentId, ct);
        if (built.Error != null)
            return Result<(byte[], string)>.Fail(built.Error.ErrorCode, built.Error.Message ?? "Unable to build receipt.");

        var pdf = ReceiptPdfBuilder.Build(built.Data!);
        return Result<(byte[], string)>.Ok((pdf, built.Data!.ReceiptNumber));
    }

    private async Task<(ReceiptData? Data, string? Html, Result<PaymentReceiptHtmlResponse>? Error)> BuildReceiptAsync(
        Guid paymentId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<PaymentReceiptHtmlResponse>();
        if (tenantError != null) return (null, null, tenantError);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (row, addons) = await repository.GetReceiptAsync(tenantId, paymentId, ct);
        if (row == null)
            return (null, null, Result<PaymentReceiptHtmlResponse>.Fail(ErrorCode.NotFound, "Payment receipt not found."));

        var data = MapReceiptData(row, addons);
        var template = string.IsNullOrWhiteSpace(row.TemplateBodyHtml)
            ? FallbackReceiptHtml
            : row.TemplateBodyHtml;
        var html = MergePlaceholders(template, data);
        return (data, html, null);
    }

    private ReceiptData MapReceiptData(PaymentReceiptRow row, IReadOnlyList<PaymentReceiptAddonRow> addons)
    {
        var phone = SafeDecrypt(row.PhoneCipher);
        var address = SafeDecrypt(row.AddressCipher);
        var gender = row.PatientGender switch
        {
            (byte)Gender.Male => "M",
            (byte)Gender.Female => "F",
            (byte)Gender.Other => "O",
            _ => "—"
        };
        var paymentMode = row.PaymentMethod switch
        {
            (byte)PaymentMethod.Cash => "Cash",
            (byte)PaymentMethod.Upi => "UPI",
            (byte)PaymentMethod.BankAccount => "Bank Account",
            (byte)PaymentMethod.Cheque => "Cheque",
            _ => "—"
        };
        var collector = $"{row.CollectorFirstName} {row.CollectorLastName}".Trim();
        if (string.IsNullOrWhiteSpace(collector)) collector = "";

        static string Money(decimal? v) => (v ?? 0).ToString("N2");

        var isCash = row.PaymentMethod == (byte)PaymentMethod.Cash;
        var isUpi = row.PaymentMethod == (byte)PaymentMethod.Upi;
        var isOther = row.PaymentMethod is (byte)PaymentMethod.BankAccount or (byte)PaymentMethod.Cheque;
        var otherDetail = row.PaymentMethod switch
        {
            (byte)PaymentMethod.BankAccount => "Bank Account",
            (byte)PaymentMethod.Cheque => "Cheque",
            _ => ""
        };

        var logoAbsoluteUrl = ToAbsoluteUrl(row.LogoUrl);
        var logoBytes = TryReadLogoBytes(row.LogoUrl);
        string logoHtml;
        if (logoBytes is { Length: > 0 })
        {
            var mime = GuessImageMime(row.LogoUrl);
            logoHtml = $"<img class=\"logo\" src=\"data:{mime};base64,{Convert.ToBase64String(logoBytes)}\" alt=\"Logo\" />";
        }
        else if (!string.IsNullOrWhiteSpace(logoAbsoluteUrl))
        {
            logoHtml = $"<img class=\"logo\" src=\"{logoAbsoluteUrl}\" alt=\"Logo\" />";
        }
        else
        {
            logoHtml = $"<div class=\"logo-fallback\">{(row.HospitalName.Length > 0 ? row.HospitalName[0] : 'H')}</div>";
        }

        var footer = string.IsNullOrWhiteSpace(row.ReceiptFooterText)
            ? "Timings: 10:00 AM - 8:00 PM (Monday to Saturday)"
            : row.ReceiptFooterText!;

        var feeLines = new List<ReceiptFeeLine>
        {
            new("Consultation Fee", Money(row.ConsultationFee)),
            new("Procedure Charges", Money(row.ProcedureCharge))
        };
        foreach (var addon in addons)
        {
            var name = string.IsNullOrWhiteSpace(addon.AddonName) ? "Add-on" : addon.AddonName.Trim();
            feeLines.Add(new(name, Money(addon.Amount)));
        }
        feeLines.Add(new("Discount", Money(row.Discount)));

        var feeRowsHtml = string.Concat(feeLines.Select((line, index) =>
            $"<tr><td class=\"sno\">{index + 1}</td><td>{System.Net.WebUtility.HtmlEncode(line.Label)}</td><td class=\"amt\">{line.Amount}</td></tr>"));

        return new ReceiptData
        {
            HospitalName = row.HospitalName,
            HospitalAddress = row.HospitalAddress,
            HospitalPhone = row.HospitalPhone,
            Website = string.IsNullOrWhiteSpace(row.Website) ? "—" : row.Website.Trim(),
            LogoHtml = logoHtml,
            LogoBytes = logoBytes,
            ReceiptHeader = "Advanced Urology & Stone Care Centre",
            ReceiptFooter = footer,
            ReceiptNumber = row.ReceiptNumber ?? "—",
            VisitCode = row.VisitCode,
            VisitDate = row.VisitDateTime.ToString("dd-MM-yyyy"),
            VisitTime = row.VisitDateTime.ToString("hh:mm tt"),
            DoctorName = FormatDoctorName(row.DoctorFirstName, row.DoctorLastName),
            DoctorDesignation = row.DoctorDesignation ?? "",
            DoctorSpecialties = "Consultant Urologist",
            PatientName = $"{row.PatientFirstName} {row.PatientLastName}".Trim(),
            PatientCode = row.PatientCode,
            Age = row.PatientAge?.ToString() ?? "—",
            Gender = gender,
            Phone = phone,
            Address = address,
            ConsultationFee = Money(row.ConsultationFee),
            ProcedureCharge = Money(row.ProcedureCharge),
            AddonCharges = Money(row.AddonCharges),
            FeeLines = feeLines,
            FeeRowsHtml = feeRowsHtml,
            Discount = Money(row.Discount),
            TotalPaid = Money(row.AmountPaid),
            PaymentMode = paymentMode,
            CashMark = isCash ? "✓" : "",
            UpiMark = isUpi ? "✓" : "",
            CardMark = "",
            OtherMark = isOther ? "✓" : "",
            OtherPaymentDetail = otherDetail,
            CollectedBy = collector,
            PaymentId = row.PaymentId
        };
    }

    private string? ToAbsoluteUrl(string? relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return null;
        var value = relativeOrAbsolute.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return value;

        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null) return value;

        var path = value.StartsWith('/') ? value : "/" + value;
        return $"{request.Scheme}://{request.Host}{path}";
    }

    private byte[]? TryReadLogoBytes(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)) return null;

        var relative = logoUrl.Trim();
        if (relative.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(relative);
                relative = uri.AbsolutePath;
            }
            catch
            {
                return null;
            }
        }

        relative = relative.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot)) return null;

        var absolutePath = Path.GetFullPath(Path.Combine(webRoot, relative));
        var rootFull = Path.GetFullPath(webRoot);
        if (!absolutePath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) || !File.Exists(absolutePath))
            return null;

        try { return File.ReadAllBytes(absolutePath); }
        catch { return null; }
    }

    private static string GuessImageMime(string? logoUrl)
    {
        var ext = Path.GetExtension(logoUrl ?? string.Empty).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/png"
        };
    }

    private static string FormatDoctorName(string? firstName, string? lastName)
    {
        var full = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(full)) return "";
        if (full.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase)
            || full.StartsWith("Dr ", StringComparison.OrdinalIgnoreCase))
            return full;
        return $"Dr. {full}";
    }

    private string SafeDecrypt(byte[]? cipher)
    {
        if (cipher == null || cipher.Length == 0) return "—";
        try { return encryption.Decrypt(cipher); }
        catch { return "—"; }
    }

    private static string MergePlaceholders(string template, ReceiptData data)
    {
        var merged = template
            .Replace("{{HospitalName}}", data.HospitalName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{HospitalAddress}}", data.HospitalAddress, StringComparison.OrdinalIgnoreCase)
            .Replace("{{HospitalPhone}}", data.HospitalPhone, StringComparison.OrdinalIgnoreCase)
            .Replace("{{Website}}", data.Website, StringComparison.OrdinalIgnoreCase)
            .Replace("{{LogoHtml}}", data.LogoHtml, StringComparison.OrdinalIgnoreCase)
            .Replace("{{ReceiptHeader}}", data.ReceiptHeader, StringComparison.OrdinalIgnoreCase)
            .Replace("{{ReceiptFooter}}", data.ReceiptFooter, StringComparison.OrdinalIgnoreCase)
            .Replace("{{ReceiptNumber}}", data.ReceiptNumber, StringComparison.OrdinalIgnoreCase)
            .Replace("{{VisitCode}}", data.VisitCode, StringComparison.OrdinalIgnoreCase)
            .Replace("{{VisitDate}}", data.VisitDate, StringComparison.OrdinalIgnoreCase)
            .Replace("{{VisitTime}}", data.VisitTime, StringComparison.OrdinalIgnoreCase)
            .Replace("{{DoctorName}}", data.DoctorName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{DoctorDesignation}}", data.DoctorDesignation, StringComparison.OrdinalIgnoreCase)
            .Replace("{{DoctorSpecialties}}", data.DoctorSpecialties, StringComparison.OrdinalIgnoreCase)
            .Replace("{{PatientName}}", data.PatientName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{PatientCode}}", data.PatientCode, StringComparison.OrdinalIgnoreCase)
            .Replace("{{Age}}", data.Age, StringComparison.OrdinalIgnoreCase)
            .Replace("{{Gender}}", data.Gender, StringComparison.OrdinalIgnoreCase)
            .Replace("{{Phone}}", data.Phone, StringComparison.OrdinalIgnoreCase)
            .Replace("{{Address}}", data.Address, StringComparison.OrdinalIgnoreCase)
            .Replace("{{ConsultationFee}}", data.ConsultationFee, StringComparison.OrdinalIgnoreCase)
            .Replace("{{ProcedureCharge}}", data.ProcedureCharge, StringComparison.OrdinalIgnoreCase)
            .Replace("{{AddonCharges}}", data.AddonCharges, StringComparison.OrdinalIgnoreCase)
            .Replace("{{FeeRows}}", data.FeeRowsHtml, StringComparison.OrdinalIgnoreCase)
            .Replace("{{Discount}}", data.Discount, StringComparison.OrdinalIgnoreCase)
            .Replace("{{TotalPaid}}", data.TotalPaid, StringComparison.OrdinalIgnoreCase)
            .Replace("{{PaymentMode}}", data.PaymentMode, StringComparison.OrdinalIgnoreCase)
            .Replace("{{CashMark}}", data.CashMark, StringComparison.OrdinalIgnoreCase)
            .Replace("{{UpiMark}}", data.UpiMark, StringComparison.OrdinalIgnoreCase)
            .Replace("{{CardMark}}", data.CardMark, StringComparison.OrdinalIgnoreCase)
            .Replace("{{OtherMark}}", data.OtherMark, StringComparison.OrdinalIgnoreCase)
            .Replace("{{OtherPaymentDetail}}", data.OtherPaymentDetail, StringComparison.OrdinalIgnoreCase)
            .Replace("{{CollectedBy}}", data.CollectedBy, StringComparison.OrdinalIgnoreCase);

        // Ensure particulars use add-on names even if the stored template still has fixed rows.
        if (!template.Contains("{{FeeRows}}", StringComparison.OrdinalIgnoreCase)
            && merged.Contains("<tbody>", StringComparison.OrdinalIgnoreCase))
        {
            merged = System.Text.RegularExpressions.Regex.Replace(
                merged,
                @"<tbody>[\s\S]*?</tbody>",
                $"<tbody>\n      {data.FeeRowsHtml}\n    </tbody>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return merged;
    }

    private const string FallbackReceiptHtml = """
        <!DOCTYPE html><html><head><meta charset="utf-8"/><title>Receipt</title>
        <style>body{font-family:Segoe UI,Arial,sans-serif;padding:16px}table{width:100%;border-collapse:collapse}td,th{border-bottom:1px solid #ddd;padding:8px}</style>
        </head><body>
          <h1>{{HospitalName}}</h1>
          <p>Advanced Urology & Stone Care Centre</p>
          <p>{{HospitalAddress}} · {{HospitalPhone}} · {{Website}}</p>
          {{LogoHtml}}
          <h3>RECEIPT</h3>
          <p>Receipt No.: {{ReceiptNumber}} · Patient: {{PatientName}} ({{PatientCode}})</p>
          <p>Date: {{VisitDate}} {{VisitTime}} · Age/Sex: {{Age}} / {{Gender}}</p>
          <table>
            <tr><th>Particulars</th><th>Amount</th></tr>
            {{FeeRows}}
            <tr><td><b>TOTAL PAID</b></td><td><b>₹ {{TotalPaid}}</b></td></tr>
          </table>
          <p>Payment: {{PaymentMode}} · Received By: {{CollectedBy}}</p>
        </body></html>
        """;

    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;

    private static PaymentListItemResponse MapListItem(PaymentListRow row)
    {
        var collectorName = $"{row.CollectorFirstName} {row.CollectorLastName}".Trim();
        if (string.IsNullOrWhiteSpace(collectorName))
            collectorName = null;

        return new PaymentListItemResponse(
            row.PaymentId,
            row.VisitId,
            row.VisitCode,
            row.VisitDateTime,
            row.VisitStatus,
            row.PatientCode,
            row.PatientFirstName,
            row.PatientLastName,
            $"{row.PatientFirstName} {row.PatientLastName}".Trim(),
            row.AmountPaid ?? row.FeeAmount,
            row.ReceiptNumber,
            row.CollectionDateTime,
            collectorName,
            row.Notes,
            row.TotalDue,
            row.TotalCollected,
            row.BalanceDue,
            row.CreatedAt);
    }
}
