namespace WebApi.Features.UserSalaries;

public record CreateUserSalaryRequest(
    decimal MonthlySalary,
    DateOnly EffectiveFrom,
    string? Notes);

public record UserSalaryResponse(
    Guid Id,
    Guid UserId,
    decimal MonthlySalary,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Notes,
    DateTime CreatedAt);
