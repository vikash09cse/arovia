namespace SharedKernel.Settings;

public class AppSettings
{
    public string SuperAdminUrl { get; set; } = "http://localhost:4200";
    public string TenantPortalUrl { get; set; } = "http://localhost:4201";
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class SendGridSettings
{
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Uro Care HMS";
    public string APIKey { get; set; } = string.Empty;
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AroviaHms";
    public string Audience { get; set; } = "AroviaHms";
    public int ExpirationHours { get; set; } = 24;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
