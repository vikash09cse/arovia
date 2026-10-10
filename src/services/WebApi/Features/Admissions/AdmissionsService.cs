using System.Globalization;
using System.Net;
using System.Text;
using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using SharedKernel.Utilities.Helpers;
using WebApi.Features.Admissions.Infrastructure;
using WebApi.Features.Shared;

namespace WebApi.Features.Admissions;

public class AdmissionsService(
    IAdmissionsRepository repository,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment,
    PhiEncryptionHelper encryption,
    PublicUrlHelper publicUrls)
{
    public async Task<Result<AdmissionListResponse>> GetListAsync(
        int page,
        int pageSize,
        byte? admissionStatus,
        Guid? patientId,
        string? admissionCode,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        var tenantError = RequireTenantContext<AdmissionListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (items, total) = await repository.GetListAsync(
            tenantId, page, pageSize, admissionStatus, patientId,
            string.IsNullOrWhiteSpace(admissionCode) ? null : admissionCode.Trim(),
            dateFrom, dateTo, ct);

        var mapped = items.Select(r => new AdmissionListItemResponse(
            r.AdmissionId,
            r.AdmissionCode,
            r.AdmittedAt,
            r.DischargedAt,
            r.Ward,
            r.Bed,
            r.RoomClass,
            FormatStatus(r.AdmissionStatus),
            r.AdmissionStatus,
            r.EstimatedAmount,
            r.DepartmentId,
            r.DepartmentName,
            r.PatientId,
            r.PatientCode,
            r.PatientFirstName,
            r.PatientLastName,
            $"{r.PatientFirstName} {r.PatientLastName}".Trim(),
            r.AttendingDoctorId,
            $"{r.DoctorFirstName} {r.DoctorLastName}".Trim()));

        return Result<AdmissionListResponse>.Ok(new AdmissionListResponse(mapped, total, page, pageSize));
    }

    public async Task<Result<AdmissionResponse>> GetByIdAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (admission, charges, payments) = await repository.GetByIdAsync(tenantId, admissionId, ct);
        if (admission == null)
            return Result<AdmissionResponse>.Fail(ErrorCode.NotFound, "Admission not found.");

        return Result<AdmissionResponse>.Ok(MapDetail(admission, charges, payments));
    }

    public async Task<Result<AdmissionResponse>> CreateAsync(CreateAdmissionRequest request, CancellationToken ct)
    {
        var validation = ValidateCreate(request);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;

        var (activeItems, activeCount) = await repository.GetListAsync(
            tenantId, 1, 1, (byte)AdmissionStatus.Admitted, request.PatientId,
            null, null, null, ct);
        if (activeCount > 0 || activeItems.Any())
        {
            return Result<AdmissionResponse>.Fail(
                ErrorCode.AlreadyExists,
                "Patient is already admitted. Discharge the current stay before admitting again.");
        }

        var bed = string.IsNullOrWhiteSpace(request.Bed) ? null : request.Bed.Trim();
        Guid id;
        try
        {
            id = await repository.SaveAsync(
                tenantId,
                request.PatientId,
                request.DepartmentId,
                request.AttendingDoctorId,
                request.Ward.Trim(),
                bed,
                request.RoomClass.Trim(),
                request.EstimatedAmount,
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                request.FromVisitId,
                request.DepositAmount is > 0 ? request.DepositAmount : null,
                request.DepositAmount is > 0 ? request.PaymentMethod : null,
                request.DepositAmount is > 0 ? request.CollectedByUserId : null,
                request.AdmissionDate,
                GetUserId(),
                ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (
            ex.Number is 50409 or 2601 or 2627
            || ex.Message.Contains("already admitted", StringComparison.OrdinalIgnoreCase))
        {
            return Result<AdmissionResponse>.Fail(
                ErrorCode.AlreadyExists,
                "Patient is already admitted. Discharge the current stay before admitting again.");
        }

        var created = await GetByIdAsync(id, ct);
        if (!created.Success || created.Data == null)
            return Result<AdmissionResponse>.Fail(ErrorCode.NotFound, "Admission was created but could not be loaded.");

        return Result<AdmissionResponse>.Ok(created.Data, "Patient admitted successfully.");
    }

    public async Task<Result<AdmissionResponse>> AddChargeAsync(
        Guid admissionId, AddAdmissionChargeRequest request, CancellationToken ct)
    {
        if (request.ChargeCategory is < 1 or > 6)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid charge category.");
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 250)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Charge description is required (max 250).");
        if (request.Amount is < 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid charge amount.");

        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.AddChargeAsync(
            tenantId, admissionId, request.ChargeCategory,
            request.Description.Trim(), request.Amount, GetUserId(), ct);

        return await GetByIdAsync(admissionId, ct);
    }

    public async Task<Result<AdmissionResponse>> ApplyDiscountAsync(
        Guid admissionId, ApplyAdmissionDiscountRequest request, CancellationToken ct)
    {
        if (request.DiscountAmount is < 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid discount amount.");
        if (request.DiscountAmount > 0 && string.IsNullOrWhiteSpace(request.DiscountReason))
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Discount reason is required.");
        if (request.DiscountReason?.Length > 500)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Discount reason cannot exceed 500 characters.");

        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.ApplyDiscountAsync(
            tenantId, admissionId, request.DiscountAmount,
            request.DiscountAmount > 0 ? request.DiscountReason!.Trim() : null,
            GetUserId(), ct);

        return await GetByIdAsync(admissionId, ct);
    }

    public async Task<Result<AdmissionResponse>> AddPaymentAsync(
        Guid admissionId, AddAdmissionPaymentRequest request, CancellationToken ct)
    {
        if (request.Amount is <= 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Amount must be greater than zero.");
        if (request.CollectedByUserId == Guid.Empty)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Collector is required.");
        if (request.PaymentMethod is not ((byte)PaymentMethod.Cash) and not ((byte)PaymentMethod.Upi)
            and not ((byte)PaymentMethod.BankAccount) and not ((byte)PaymentMethod.Cheque))
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid payment method.");
        if (request.PaymentKind is not ((byte)AdmissionPaymentKind.Deposit)
            and not ((byte)AdmissionPaymentKind.Partial)
            and not ((byte)AdmissionPaymentKind.Final))
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid payment kind.");
        if (request.Notes?.Length > 500)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Notes cannot exceed 500 characters.");

        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.AddPaymentAsync(
            tenantId, admissionId, request.Amount, request.PaymentMethod, request.PaymentKind,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            request.CollectedByUserId, GetUserId(), ct);

        return await GetByIdAsync(admissionId, ct);
    }

    public async Task<Result<AdmissionResponse>> DischargeAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.DischargeAsync(tenantId, admissionId, GetUserId(), ct);
        return await GetByIdAsync(admissionId, ct);
    }

    public async Task<Result<bool>> DeleteAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.DeleteAsync(tenantId, admissionId, GetUserId(), ct);
        return Result<bool>.Ok(true, "Admission deleted.");
    }

    private static Result<AdmissionResponse>? ValidateCreate(CreateAdmissionRequest request)
    {
        if (request.PatientId == Guid.Empty)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Patient is required.");
        if (request.DepartmentId == Guid.Empty)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Department is required.");
        if (request.AttendingDoctorId == Guid.Empty)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Attending doctor is required.");
        if (string.IsNullOrWhiteSpace(request.Ward) || request.Ward.Trim().Length > 100)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Ward is required (max 100).");
        if (request.Bed != null && request.Bed.Trim().Length > 50)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Bed cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(request.RoomClass) || request.RoomClass.Trim().Length > 50)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Room class is required (max 50).");
        if (request.EstimatedAmount is < 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid estimated amount.");
        if (request.Notes?.Length > 1000)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Notes cannot exceed 1000 characters.");
        if (request.DepositAmount is < 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid deposit amount.");
        if (request.DepositAmount > 0 && request.CollectedByUserId == null)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Collector is required for deposit.");
        if (request.DepositAmount > 0
            && request.PaymentMethod is not ((byte)PaymentMethod.Cash) and not ((byte)PaymentMethod.Upi)
                and not ((byte)PaymentMethod.BankAccount) and not ((byte)PaymentMethod.Cheque))
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid payment method for deposit.");
        if (request.AdmissionDate is { } admissionDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (admissionDate > today.AddDays(1))
                return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Admission date cannot be more than one day in the future.");
            if (admissionDate < today.AddYears(-2))
                return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Admission date is too far in the past.");
        }
        return null;
    }

    private static AdmissionResponse MapDetail(
        AdmissionDetailRow a,
        IEnumerable<AdmissionChargeRow> charges,
        IEnumerable<AdmissionPaymentRow> payments)
    {
        return new AdmissionResponse(
            a.AdmissionId,
            a.AdmissionCode,
            a.AdmittedAt,
            a.DischargedAt,
            a.Ward,
            a.Bed,
            a.RoomClass,
            FormatStatus(a.AdmissionStatus),
            a.AdmissionStatus,
            a.EstimatedAmount,
            a.Notes,
            a.DiscountAmount,
            a.DiscountReason,
            a.InvoiceNumber,
            a.FromVisitId,
            a.FromVisitCode,
            a.DepartmentId,
            a.DepartmentName,
            a.PatientId,
            a.PatientCode,
            a.PatientFirstName,
            a.PatientLastName,
            $"{a.PatientFirstName} {a.PatientLastName}".Trim(),
            a.AttendingDoctorId,
            $"{a.DoctorFirstName} {a.DoctorLastName}".Trim(),
            a.ChargesTotal,
            a.BillTotal,
            a.PaidTotal,
            a.BalanceDue,
            a.CreatedAt,
            charges.Select(c => new AdmissionChargeResponse(
                c.AdmissionChargeId,
                c.ChargeCategory,
                FormatChargeCategory(c.ChargeCategory),
                c.Description,
                c.Amount,
                c.ChargedOn,
                FormatName(c.CreatorFirstName, c.CreatorLastName))),
            payments.Select(p => new AdmissionPaymentResponse(
                p.AdmissionPaymentId,
                p.Amount,
                p.PaymentMethod,
                FormatPaymentMethod(p.PaymentMethod),
                p.PaymentKind,
                FormatPaymentKind(p.PaymentKind),
                p.ReceiptNumber,
                p.Notes,
                p.CollectionDateTime,
                p.CollectedByUserId,
                FormatName(p.CollectorFirstName, p.CollectorLastName))));
    }

    private static string FormatStatus(byte code) => code switch
    {
        (byte)AdmissionStatus.Admitted => "Admitted",
        (byte)AdmissionStatus.Discharged => "Discharged",
        (byte)AdmissionStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };

    private static string FormatChargeCategory(byte code) => code switch
    {
        (byte)AdmissionChargeCategory.Room => "Room",
        (byte)AdmissionChargeCategory.Procedure => "Procedure",
        (byte)AdmissionChargeCategory.Lab => "Lab",
        (byte)AdmissionChargeCategory.Pharmacy => "Pharmacy",
        (byte)AdmissionChargeCategory.Doctor => "Doctor",
        (byte)AdmissionChargeCategory.Other => "Other",
        _ => "Other"
    };

    private static string FormatPaymentKind(byte code) => code switch
    {
        (byte)AdmissionPaymentKind.Deposit => "Deposit",
        (byte)AdmissionPaymentKind.Partial => "Partial",
        (byte)AdmissionPaymentKind.Final => "Final",
        _ => "Unknown"
    };

    private static string FormatPaymentMethod(byte code) => code switch
    {
        (byte)PaymentMethod.Cash => "Cash",
        (byte)PaymentMethod.Upi => "UPI",
        (byte)PaymentMethod.BankAccount => "Bank Account",
        (byte)PaymentMethod.Cheque => "Cheque",
        _ => "Unknown"
    };

    private static string? FormatName(string? first, string? last)
    {
        var name = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public async Task<Result<AdmissionFinalInvoiceResponse>> GetFinalInvoiceAsync(
        Guid admissionId, CancellationToken ct)
    {
        var built = await BuildFinalInvoiceAsync(admissionId, requireTemplate: true, ct);
        if (built.Error != null) return built.Error;
        return Result<AdmissionFinalInvoiceResponse>.Ok(
            new AdmissionFinalInvoiceResponse(admissionId, built.Data!.InvoiceNumber, built.Html!));
    }

    public async Task<Result<(byte[] Bytes, string InvoiceNumber)>> GetFinalInvoicePdfAsync(
        Guid admissionId, CancellationToken ct)
    {
        // PDF is rendered from the Discharge Invoice HTML template after placeholder substitution.
        var built = await BuildFinalInvoiceAsync(admissionId, requireTemplate: true, ct);
        if (built.Error != null)
            return Result<(byte[], string)>.Fail(built.Error.ErrorCode, built.Error.Message ?? "Unable to build invoice.");

        try
        {
            var pdf = await HtmlPdfRenderer.RenderAsync(built.Html!, ct);
            return Result<(byte[], string)>.Ok((pdf, built.Data!.InvoiceNumber));
        }
        catch (Exception ex)
        {
            return Result<(byte[], string)>.Fail(
                ErrorCode.InternalError,
                $"Unable to render invoice PDF from template. {ex.Message}");
        }
    }

    private async Task<(
        FinalInvoiceBuildData? Data,
        FinalInvoiceData? PdfData,
        string? Html,
        Result<AdmissionFinalInvoiceResponse>? Error)> BuildFinalInvoiceAsync(
        Guid admissionId, bool requireTemplate, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<AdmissionFinalInvoiceResponse>();
        if (tenantError != null) return (null, null, null, tenantError);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        string invoiceNumber;
        try
        {
            invoiceNumber = await repository.EnsureInvoiceNumberAsync(tenantId, admissionId, GetUserId(), ct);
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return (null, null, null, Result<AdmissionFinalInvoiceResponse>.Fail(ErrorCode.Validation, message));
        }

        var (header, charges, payments) = await repository.GetFinalInvoiceDataAsync(tenantId, admissionId, ct);
        if (header == null)
            return (null, null, null, Result<AdmissionFinalInvoiceResponse>.Fail(ErrorCode.NotFound, "Admission not found."));

        if (header.AdmissionStatus is not ((byte)AdmissionStatus.Admitted)
            and not ((byte)AdmissionStatus.Discharged))
            return (null, null, null, Result<AdmissionFinalInvoiceResponse>.Fail(
                ErrorCode.Validation, "Invoice is available only for admitted or discharged stays."));

        if (requireTemplate && string.IsNullOrWhiteSpace(header.TemplateBodyHtml))
            return (null, null, null, Result<AdmissionFinalInvoiceResponse>.Fail(
                ErrorCode.NotFound, "Discharge invoice template is not configured."));

        invoiceNumber = string.IsNullOrWhiteSpace(invoiceNumber)
            ? (header.InvoiceNumber ?? "—")
            : invoiceNumber;

        var pdfData = MapFinalInvoiceData(header, charges, payments, invoiceNumber);
        var html = string.IsNullOrWhiteSpace(header.TemplateBodyHtml)
            ? null
            : BuildFinalInvoiceHtml(header, charges, invoiceNumber, pdfData);

        return (new FinalInvoiceBuildData(admissionId, invoiceNumber), pdfData, html, null);
    }

    private FinalInvoiceData MapFinalInvoiceData(
        AdmissionFinalInvoiceHeaderRow header,
        IReadOnlyList<AdmissionFinalInvoiceChargeRow> charges,
        IReadOnlyList<AdmissionFinalInvoicePaymentRow> payments,
        string invoiceNumber)
    {
        var gender = header.PatientGender switch
        {
            (byte)Gender.Male => "Male",
            (byte)Gender.Female => "Female",
            (byte)Gender.Other => "Other",
            _ => "—"
        };

        var latestPayment = payments.OrderByDescending(p => p.CollectionDateTime).FirstOrDefault();
        var paymentModes = payments
            .Select(p => FormatPaymentMethod(p.PaymentMethod))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var paymentMode = paymentModes.Count == 0 ? "—" : string.Join(" / ", paymentModes);
        var paymentDate = latestPayment?.CollectionDateTime.ToString("dd-MM-yyyy") ?? "—";

        var procedureDate = header.ProcedureChargedOn ?? header.AdmittedAt;
        var billDate = header.DischargedAt ?? header.AdmittedAt;
        var receiptHeader = string.IsNullOrWhiteSpace(header.ReceiptHeaderText)
            ? "ADVANCED UROLOGY & KIDNEY STONE CARE CENTRE"
            : header.ReceiptHeaderText.Trim();
        var proceduresLine = string.IsNullOrWhiteSpace(header.ReceiptFooterText)
            ? "PCNL | RIRS | URSL | TURP | UROFLOWMETRY | CYSTOSCOPY"
            : header.ReceiptFooterText.Trim();

        var discount = header.DiscountAmount;
        string? discountLine = discount > 0
            ? $"PACKAGE DISCOUNT{(string.IsNullOrWhiteSpace(header.DiscountReason) ? "" : $" ({header.DiscountReason.Trim()})")} (-) ₹{Money(discount)} /-"
            : null;

        var chargeLines = charges.Select(c => new FinalInvoiceChargeLine(
            FormatChargeCategory(c.ChargeCategory),
            c.Description,
            Money(c.Amount))).ToList();

        return new FinalInvoiceData
        {
            HospitalName = header.HospitalName ?? "",
            HospitalAddress = header.HospitalAddress ?? "",
            HospitalPhone = header.HospitalPhone ?? "",
            Website = header.Website?.Trim() ?? "",
            LogoBytes = TryReadLogoBytes(header.LogoUrl),
            ReceiptHeader = receiptHeader,
            ProceduresLine = proceduresLine,
            RegistrationNumber = string.IsNullOrWhiteSpace(header.RegistrationNumber) ? "—" : header.RegistrationNumber.Trim(),
            InvoiceNumber = invoiceNumber,
            PatientId = string.IsNullOrWhiteSpace(header.PatientCode) ? "—" : header.PatientCode.Trim(),
            PatientName = $"{header.PatientFirstName} {header.PatientLastName}".Trim(),
            Age = header.PatientAge?.ToString() ?? "—",
            Gender = gender,
            Address = SafeDecrypt(header.AddressCipher),
            Phone = SafeDecrypt(header.PhoneCipher),
            BillDate = billDate.ToString("dd-MM-yyyy"),
            AdmissionDate = header.AdmittedAt.ToString("dd-MM-yyyy"),
            ProcedureDate = procedureDate.ToString("dd-MM-yyyy"),
            DischargeDate = header.DischargedAt?.ToString("dd-MM-yyyy") ?? "—",
            Charges = chargeLines,
            GrossTotal = Money(header.ChargesTotal),
            DiscountLine = discountLine,
            PayableAmount = Money(header.BillTotal),
            AmountInWords = IndianCurrencyWords.ToRupeesOnly(header.BillTotal),
            AmountPaid = Money(header.PaidTotal),
            PaymentMode = paymentMode,
            PaymentDate = paymentDate,
            ShowPaidStamp = header.BalanceDue <= 0.009m,
            DoctorName = ClinicalDocumentChrome.FormatDoctorName(header.DoctorFirstName, header.DoctorLastName),
            DoctorDesignation = header.DoctorDesignation ?? "",
            CredentialLines = ClinicalDocumentChrome.CredentialLines(header.DoctorDesignation),
            CopyrightLine = ClinicalDocumentChrome.BuildCopyrightLine(header.HospitalName ?? "", header.HospitalAddress)
        };
    }

    private string BuildFinalInvoiceHtml(
        AdmissionFinalInvoiceHeaderRow header,
        IReadOnlyList<AdmissionFinalInvoiceChargeRow> charges,
        string invoiceNumber,
        FinalInvoiceData data)
    {
        var logoHtml = BuildLogoHtml(header.LogoUrl, header.HospitalName);
        var chargeRows = BuildChargeRowsHtml(charges);
        var discountLineHtml = string.IsNullOrWhiteSpace(data.DiscountLine)
            ? ""
            : $"<div class=\"discount\">{WebUtility.HtmlEncode(data.DiscountLine)}</div>";
        var paidStampClass = data.ShowPaidStamp ? "" : "hidden";
        var credentialsHtml = string.Join("", data.CredentialLines.Select(l =>
            $"<div class=\"cdc-doctor-cred\">{WebUtility.HtmlEncode(l)}</div>"));
        var designationHtml = data.CredentialLines.Count > 0
            ? string.Join("<br/>", data.CredentialLines.Select(WebUtility.HtmlEncode))
            : Enc(data.DoctorDesignation);
        var brandTitleHtml = ClinicalDocumentChrome.InvoiceBrandTitleHtml(data.HospitalName);
        var websiteDisplay = FormatWebsiteForTemplate(data.Website);
        var registration = Enc(data.RegistrationNumber);

        return header.TemplateBodyHtml!
            .Replace("{{HospitalName}}", Enc(data.HospitalName), StringComparison.OrdinalIgnoreCase)
            .Replace("{{HospitalAddress}}", Enc(data.HospitalAddress), StringComparison.OrdinalIgnoreCase)
            .Replace("{{HospitalPhone}}", Enc(data.HospitalPhone), StringComparison.OrdinalIgnoreCase)
            .Replace("{{Website}}", Enc(websiteDisplay), StringComparison.OrdinalIgnoreCase)
            .Replace("{{LogoHtml}}", logoHtml, StringComparison.OrdinalIgnoreCase)
            .Replace("{{BrandTitleHtml}}", brandTitleHtml, StringComparison.OrdinalIgnoreCase)
            .Replace("{{DoctorCredentialsHtml}}", credentialsHtml, StringComparison.OrdinalIgnoreCase)
            .Replace("{{CopyrightLine}}", Enc(data.CopyrightLine), StringComparison.OrdinalIgnoreCase)
            .Replace("{{ReceiptHeader}}", Enc(data.ReceiptHeader), StringComparison.OrdinalIgnoreCase)
            .Replace("{{ProceduresLine}}", Enc(data.ProceduresLine), StringComparison.OrdinalIgnoreCase)
            .Replace("{{RegistrationNumber}}", registration, StringComparison.OrdinalIgnoreCase)
            .Replace("{{HospitalRegistrationNo}}", registration, StringComparison.OrdinalIgnoreCase)
            .Replace("{{PatientId}}", Enc(data.PatientId), StringComparison.OrdinalIgnoreCase)
            .Replace("{{PatientCode}}", Enc(data.PatientId), StringComparison.OrdinalIgnoreCase)
            .Replace("{{PatientName}}", Enc(data.PatientName), StringComparison.OrdinalIgnoreCase)
            .Replace("{{Age}}", Enc(data.Age), StringComparison.OrdinalIgnoreCase)
            .Replace("{{Gender}}", Enc(data.Gender), StringComparison.OrdinalIgnoreCase)
            .Replace("{{Address}}", Enc(data.Address), StringComparison.OrdinalIgnoreCase)
            .Replace("{{Phone}}", Enc(data.Phone), StringComparison.OrdinalIgnoreCase)
            .Replace("{{InvoiceNumber}}", Enc(invoiceNumber), StringComparison.OrdinalIgnoreCase)
            .Replace("{{BillDate}}", Enc(data.BillDate), StringComparison.OrdinalIgnoreCase)
            .Replace("{{AdmissionDate}}", Enc(data.AdmissionDate), StringComparison.OrdinalIgnoreCase)
            .Replace("{{ProcedureDate}}", Enc(data.ProcedureDate), StringComparison.OrdinalIgnoreCase)
            .Replace("{{DischargeDate}}", Enc(data.DischargeDate), StringComparison.OrdinalIgnoreCase)
            .Replace("{{ChargeRows}}", chargeRows, StringComparison.OrdinalIgnoreCase)
            .Replace("{{GrossTotal}}", data.GrossTotal, StringComparison.OrdinalIgnoreCase)
            .Replace("{{DiscountLine}}", discountLineHtml, StringComparison.OrdinalIgnoreCase)
            .Replace("{{PayableAmount}}", data.PayableAmount, StringComparison.OrdinalIgnoreCase)
            .Replace("{{AmountInWords}}", Enc(data.AmountInWords), StringComparison.OrdinalIgnoreCase)
            .Replace("{{AmountPaid}}", data.AmountPaid, StringComparison.OrdinalIgnoreCase)
            .Replace("{{PaymentMode}}", Enc(data.PaymentMode), StringComparison.OrdinalIgnoreCase)
            .Replace("{{PaymentDate}}", Enc(data.PaymentDate), StringComparison.OrdinalIgnoreCase)
            .Replace("{{PaidStampClass}}", paidStampClass, StringComparison.OrdinalIgnoreCase)
            .Replace("{{DoctorName}}", Enc(data.DoctorName), StringComparison.OrdinalIgnoreCase)
            .Replace("{{DoctorDesignation}}", designationHtml, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatWebsiteForTemplate(string? website)
    {
        if (string.IsNullOrWhiteSpace(website) || website.Trim() == "—")
            return "—";
        var w = website.Trim();
        if (w.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            w = w[8..];
        else if (w.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            w = w[7..];
        return w.TrimEnd('/');
    }

    private sealed record FinalInvoiceBuildData(Guid AdmissionId, string InvoiceNumber);

    private static string BuildChargeRowsHtml(IReadOnlyList<AdmissionFinalInvoiceChargeRow> charges)
    {
        if (charges.Count == 0)
        {
            return """
              <tr>
                <td class="sno">1</td>
                <td class="part">—</td>
                <td>No charges recorded</td>
                <td class="amt">₹ 0.00</td>
              </tr>
              """;
        }

        var sb = new StringBuilder();
        var i = 1;
        foreach (var c in charges)
        {
            sb.AppendLine($"""
              <tr>
                <td class="sno">{i}</td>
                <td class="part">{Enc(FormatChargeCategory(c.ChargeCategory))}</td>
                <td>{Enc(c.Description)}</td>
                <td class="amt">₹ {Money(c.Amount)}</td>
              </tr>
              """);
            i++;
        }
        return sb.ToString();
    }

    private string BuildLogoHtml(string? logoUrl, string hospitalName)
    {
        var bytes = TryReadLogoBytes(logoUrl);
        if (bytes is { Length: > 0 })
        {
            var mime = GuessImageMime(logoUrl);
            var b64 = Convert.ToBase64String(bytes);
            return $"<img class=\"cdc-logo logo\" src=\"data:{mime};base64,{b64}\" alt=\"Logo\"/>";
        }

        var publicUrl = publicUrls.ToPublicUrl(logoUrl);
        if (!string.IsNullOrWhiteSpace(publicUrl))
            return $"<img class=\"cdc-logo logo\" src=\"{WebUtility.HtmlEncode(publicUrl)}\" alt=\"Logo\"/>";

        var initial = string.IsNullOrWhiteSpace(hospitalName) ? "H" : hospitalName.Trim()[..1].ToUpperInvariant();
        return $"<div class=\"cdc-logo-fallback logo-fallback\">{Enc(initial)}</div>";
    }

    private byte[]? TryReadLogoBytes(string? logoUrl)
    {
        var relative = publicUrls.ToWebRootRelativePath(logoUrl);
        if (string.IsNullOrWhiteSpace(relative)) return null;

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

    private string SafeDecrypt(byte[]? cipher)
    {
        if (cipher == null || cipher.Length == 0) return "—";
        try { return encryption.Decrypt(cipher); }
        catch { return "—"; }
    }

    private static string Money(decimal amount) =>
        amount.ToString("N2", CultureInfo.InvariantCulture);

    private static string Enc(string? value) =>
        WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "—" : value);

    private static bool TrySqlMessage(Exception ex, out string message)
    {
        message = ex.Message;
        if (ex is Microsoft.Data.SqlClient.SqlException sql && !string.IsNullOrWhiteSpace(sql.Message))
        {
            message = sql.Message.Split('\n')[0].Trim();
            return true;
        }

        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
        {
            if (inner is Microsoft.Data.SqlClient.SqlException sqlInner && !string.IsNullOrWhiteSpace(sqlInner.Message))
            {
                message = sqlInner.Message.Split('\n')[0].Trim();
                return true;
            }
        }

        return false;
    }

    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;
}
