using GestorDeBacklogs.Api.Services;

namespace GestorDeBacklogs.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithErrorHandling();

        group.MapGet("/dashboard", async (
            string team, string? areaPath, string iterationPath, int? trendSprints, IDashboardService service) =>
            Results.Ok(await service.GetSprintDashboardAsync(team, areaPath, iterationPath, trendSprints ?? 5)));
    }
}
