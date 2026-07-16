namespace WebApi.Features.Dashboard;

public record TenantDashboardResponse(
    int TotalPatientCount,
    int TodayNewPatientCount,
    int TodayVisitCount,
    decimal TodayRevenue,
    decimal CurrentMonthRevenue,
    decimal TotalPendingAmount,
    int TodayLabAssignCount,
    int CurrentMonthLabAssignCount);
