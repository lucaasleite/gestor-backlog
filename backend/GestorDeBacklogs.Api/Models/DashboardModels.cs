namespace GestorDeBacklogs.Api.Models;

public record SprintTrendPointDto(
    string Label,
    bool IsCurrent,
    int PlannedItems,
    int OutOfSprintItems,
    double PlannedHours,
    double OutOfSprintHours);

public record AnalystBreakdownDto(
    string Name,
    int PlannedItems,
    int OutOfSprintItems,
    double PlannedHours,
    double OutOfSprintHours,
    int DoneItems,
    int TotalItems);

public record OutOfSprintItemDto(
    int Id,
    string Title,
    string WorkItemType,
    string? AssignedTo,
    int? EffortHours,
    string? State);

public record SprintDashboardDto(
    int PlannedItems,
    int OutOfSprintItems,
    double PlannedHours,
    double OutOfSprintHours,
    int PlannedDoneItems,
    int OutOfSprintDoneItems,
    double PlannedDoneHours,
    double OutOfSprintDoneHours,
    IReadOnlyList<SprintTrendPointDto> Trend,
    IReadOnlyList<AnalystBreakdownDto> Analysts,
    IReadOnlyList<OutOfSprintItemDto> OutOfSprintDetail);
