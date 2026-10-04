namespace WebApi.Features.Dashboard;

public record TenantDashboardResponse(
    int TotalPatientCount,
    int TodayNewPatientCount,
    int TodayVisitCount,
    decimal TodayRevenue,
    decimal TodayOpdRevenue,
    decimal TodayIpdRevenue,
    decimal CurrentMonthRevenue,
    decimal CurrentMonthOpdRevenue,
    decimal CurrentMonthIpdRevenue,
    decimal TotalPendingAmount,
    decimal TodayPendingAmount,
    int TodayLabAssignCount,
    int CurrentMonthLabAssignCount);
