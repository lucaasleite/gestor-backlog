namespace GestorDeBacklogs.Api.Models;

public record WorkItemDto(
    int Id,
    string Title,
    string WorkItemType,
    string? SizeLabel,
    int? EffortHours,
    string? AssignedTo,
    string IterationPath,
    string AreaPath,
    string Url,
    bool AlreadyHasTasks,
    double? OriginalEstimate = null,
    double? RemainingWork = null,
    double? CompletedWork = null,
    string? State = null);

public record PlannedTaskDto(string Title, double Hours);

public record WorkItemPreviewDto(
    int Id,
    string Title,
    string WorkItemType,
    string? SizeLabel,
    int? EffortHours,
    string? AssignedTo,
    bool AlreadyHasTasks,
    bool SizeRecognized,
    IReadOnlyList<PlannedTaskDto> PlannedTasks,
    string? State);

public record ParentUserStoryDto(int Id, string Title, string? AssignedTo, string? SizeLabel, int? EffortHours, bool AlreadyHasTasks);
