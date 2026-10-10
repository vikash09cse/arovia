namespace WebApi.Features.Admissions;

/// <summary>
/// Invoice line/data models used when substituting the Discharge Invoice HTML template.
/// PDF bytes are produced from the filled template (see <see cref="Shared.HtmlPdfRenderer"/>).
/// </summary>
public sealed class FinalInvoiceChargeLine(string particulars, string description, string amount)
{
    public string Particulars { get; } = particulars;
    public string Description { get; } = description;
    public string Amount { get; } = amount;
}

public sealed class FinalInvoiceData
{
    public string HospitalName { get; init; } = "";
    public string HospitalAddress { get; init; } = "";
    public string HospitalPhone { get; init; } = "";
    public string Website { get; init; } = "";
    public byte[]? LogoBytes { get; init; }
    public string ReceiptHeader { get; init; } = "";
    public string ProceduresLine { get; init; } = "";
    public string RegistrationNumber { get; init; } = "";
    public string InvoiceNumber { get; init; } = "";
    public string PatientId { get; init; } = "";
    public string PatientName { get; init; } = "";
    public string Age { get; init; } = "";
    public string Gender { get; init; } = "";
    public string Address { get; init; } = "";
    public string Phone { get; init; } = "";
    public string BillDate { get; init; } = "";
    public string AdmissionDate { get; init; } = "";
    public string ProcedureDate { get; init; } = "";
    public string DischargeDate { get; init; } = "";
    public IReadOnlyList<FinalInvoiceChargeLine> Charges { get; init; } = [];
    public string GrossTotal { get; init; } = "";
    public string? DiscountLine { get; init; }
    public string PayableAmount { get; init; } = "";
    public string AmountInWords { get; init; } = "";
    public string AmountPaid { get; init; } = "";
    public string PaymentMode { get; init; } = "";
    public string PaymentDate { get; init; } = "";
    public bool ShowPaidStamp { get; init; }
    public string DoctorName { get; init; } = "";
    public string DoctorDesignation { get; init; } = "";
    public IReadOnlyList<string> CredentialLines { get; init; } = [];
    public string CopyrightLine { get; init; } = "";
}
