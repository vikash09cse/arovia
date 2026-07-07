namespace SharedKernel.Enums;

public enum TenantStatus : byte
{
    Active = 1,
    Suspended = 2
}

public enum UserStatus : byte
{
    Active = 1,
    Inactive = 2
}

public enum UserType : byte
{
    PlatformAdmin = 0,
    TenantSuperAdmin = 1,
    Staff = 2,
    Doctor = 3,
    BackOfficeUser = 4
}

public enum LoginType : byte
{
    Platform = 1,
    Tenant = 2
}

public enum Gender : byte
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum PatientStatus : byte
{
    Active = 1,
    Inactive = 2
}

public enum BloodGroup : byte
{
    APositive = 1,
    ANegative = 2,
    BPositive = 3,
    BNegative = 4,
    ABPositive = 5,
    ABNegative = 6,
    OPositive = 7,
    ONegative = 8
}

public enum VisitType : byte
{
    Opd = 1,
    FollowUp = 2,
    PreOp = 3,
    Surgery = 4
}

public enum VisitFeeStatus : byte
{
    Charged = 1,
    Free = 2
}

public enum VisitStatus : byte
{
    Active = 1,
    Cancelled = 2
}

public enum PaymentLineType : byte
{
    Consultation = 1,
    Procedure = 2,
    Collection = 3
}

public enum PaymentStatus : byte
{
    Pending = 1,
    Paid = 2,
    Refunded = 3
}

public enum AggregatedPaymentStatus : byte
{
    None = 0,
    Pending = 1,
    Paid = 2,
    Partial = 3
}

public enum LabAgencyStatus : byte
{
    Active = 1,
    Inactive = 2
}

public enum VisitAddonStatus : byte
{
    Active = 1,
    Inactive = 2
}

public static class RoleNames
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string TenantSuperAdmin = "TenantSuperAdmin";
    public const string Staff = "Staff";
    public const string Doctor = "Doctor";
    public const string BackOfficeUser = "BackOfficeUser";

    public static string FromUserType(UserType type) => type switch
    {
        UserType.PlatformAdmin => PlatformAdmin,
        UserType.TenantSuperAdmin => TenantSuperAdmin,
        UserType.Staff => Staff,
        UserType.Doctor => Doctor,
        UserType.BackOfficeUser => BackOfficeUser,
        _ => Staff
    };
}
