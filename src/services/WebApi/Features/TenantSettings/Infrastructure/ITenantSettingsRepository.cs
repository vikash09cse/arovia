namespace WebApi.Features.TenantSettings.Infrastructure;

public class TenantSettingsRow
{
    public Guid TenantId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string PrimaryContactFirstName { get; set; } = string.Empty;
    public string PrimaryContactLastName { get; set; } = string.Empty;
    public string PrimaryContactEmail { get; set; } = string.Empty;
    public string PrimaryContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public interface ITenantSettingsRepository
{
    Task<TenantSettingsRow?> GetAsync(Guid tenantId, CancellationToken ct);
    Task UpdateAsync(
        Guid tenantId,
        string hospitalName,
        string primaryContactFirstName,
        string primaryContactLastName,
        string primaryContactEmail,
        string primaryContactPhone,
        string address,
        string timezone,
        string? website,
        string? logoUrl,
        CancellationToken ct);
}
