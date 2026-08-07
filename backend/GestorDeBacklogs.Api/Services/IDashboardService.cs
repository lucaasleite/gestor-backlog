using GestorDeBacklogs.Api.Models;

namespace GestorDeBacklogs.Api.Services;

public interface IDashboardService
{
    Task<SprintDashboardDto> GetSprintDashboardAsync(
        string teamName, string? areaPath, string iterationPath, int trendSprints = 5, CancellationToken ct = default);
}
