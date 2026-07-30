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
    bool AlreadyHasTasks);

public record WorkItemPreviewDto(
    int Id,
    string Title,
    string WorkItemType,
    string? SizeLabel,
    int? EffortHours,
    string? AssignedTo,
    bool AlreadyHasTasks,
    bool SizeRecognized,
    IReadOnlyList<string> PlannedTaskTitles);
