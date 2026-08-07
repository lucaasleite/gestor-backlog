using GestorDeBacklogs.Api.Config;
using GestorDeBacklogs.Api.Models;
using Microsoft.Extensions.Options;

namespace GestorDeBacklogs.Api.Services;

public class DashboardService(IAzureDevOpsClient client, IOptionsSnapshot<AzureDevOpsSettings> options) : IDashboardService
{
    private readonly AzureDevOpsSettings _settings = options.Value;

    public async Task<SprintDashboardDto> GetSprintDashboardAsync(
        string teamName, string? areaPath, string iterationPath, int trendSprints = 5, CancellationToken ct = default)
    {
        var iterations = await client.GetIterationsAsync(teamName, ct);
        var targetIndex = iterations.ToList().FindIndex(i => i.Path == iterationPath);
        if (targetIndex < 0)
        {
            throw new InvalidOperationException("Sprint não encontrada para esse time.");
        }

        var windowStart = Math.Max(0, targetIndex - (trendSprints - 1));
        var window = iterations.Skip(windowStart).Take(targetIndex - windowStart + 1).ToList();

        var trend = new List<SprintTrendPointDto>();
        IReadOnlyList<ClassifiedItem> targetItems = [];

        foreach (var iteration in window)
        {
            var ids = await client.QueryWorkItemIdsForIterationAsync(iteration.Path, areaPath, ct);
            var workItems = await client.GetWorkItemsByIdsAsync(ids, ct);

            var classified = workItems
                .Where(wi => !WorkItemTypeFilters.ExcludedFromSprintView.Contains(wi.WorkItemType))
                .Select(Classify)
                .ToList();

            trend.Add(BuildTrendPoint(iteration.Name, iteration.Path == iterationPath, classified));

            if (iteration.Path == iterationPath)
            {
                targetItems = classified;
            }
        }

        var planned = targetItems.Where(c => c.Planned).ToList();
        var outOfSprint = targetItems.Where(c => !c.Planned).ToList();

        var analysts = targetItems
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Item.AssignedTo) ? "(Sem responsável)" : c.Item.AssignedTo!)
            .Select(g => new AnalystBreakdownDto(
                g.Key,
                g.Count(c => c.Planned),
                g.Count(c => !c.Planned),
                g.Where(c => c.Planned).Sum(c => c.Hours),
                g.Where(c => !c.Planned).Sum(c => c.Hours),
                g.Count(c => c.Done),
                g.Count()))
            .OrderByDescending(a => a.PlannedItems + a.OutOfSprintItems)
            .ToList();

        var outOfSprintDetail = outOfSprint
            .Select(c => new OutOfSprintItemDto(c.Item.Id, c.Item.Title, c.Item.WorkItemType, c.Item.AssignedTo, c.Item.EffortHours, c.Item.State))
            .ToList();

        return new SprintDashboardDto(
            planned.Count,
            outOfSprint.Count,
            planned.Sum(c => c.Hours),
            outOfSprint.Sum(c => c.Hours),
            planned.Count(c => c.Done),
            outOfSprint.Count(c => c.Done),
            planned.Where(c => c.Done).Sum(c => c.Hours),
            outOfSprint.Where(c => c.Done).Sum(c => c.Hours),
            trend,
            analysts,
            outOfSprintDetail);
    }

    private static SprintTrendPointDto BuildTrendPoint(string label, bool isCurrent, IReadOnlyList<ClassifiedItem> items)
    {
        var planned = items.Where(c => c.Planned).ToList();
        var outOfSprint = items.Where(c => !c.Planned).ToList();

        return new SprintTrendPointDto(
            label,
            isCurrent,
            planned.Count,
            outOfSprint.Count,
            planned.Sum(c => c.Hours),
            outOfSprint.Sum(c => c.Hours));
    }

    private ClassifiedItem Classify(WorkItemDto item)
    {
        var planned = SprintCategoryClassifier.IsPlanned(item.Tags, _settings.PlannedTagName);
        var done = item.State is not null && _settings.DoneStates.Contains(item.State, StringComparer.OrdinalIgnoreCase);
        var hours = (double)(item.EffortHours ?? 0);

        return new ClassifiedItem(item, planned, done, hours);
    }

    private sealed record ClassifiedItem(WorkItemDto Item, bool Planned, bool Done, double Hours);
}
