namespace WebApi.Features.TenantSettings;

public record UpdateTenantSettingsRequest(
    string HospitalName,
    string PrimaryContactFirstName,
    string PrimaryContactLastName,
    string PrimaryContactEmail,
    string PrimaryContactPhone,
    string Address,
    string Timezone,
    string? Website = null,
    string? HospitalRegistrationNo = null,
    string? LogoUrl = null);

public record TenantSettingsResponse(
    Guid Id,
    string HospitalName,
    string Subdomain,
    string PrimaryContactFirstName,
    string PrimaryContactLastName,
    string PrimaryContactEmail,
    string PrimaryContactPhone,
    string Address,
    string Timezone,
    string? Website,
    string? HospitalRegistrationNo,
    string? LogoUrl,
    DateTime UpdatedAt);
