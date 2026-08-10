namespace GestorDeBacklogs.Api.Models;

public record TaskOverride(string Title, double Hours);

public record GenerateTasksRequest(
    string IterationPath,
    IReadOnlyList<int> WorkItemIds,
    IReadOnlyDictionary<int, IReadOnlyList<TaskOverride>>? Overrides = null);

public record GenerateTaskForItemRequest(IReadOnlyList<TaskOverride>? Tasks = null);

public record MoveToIterationRequest(string IterationPath);

public record GenerateTasksFromParentRequest(IReadOnlyList<int> UserStoryIds, IReadOnlyList<string> IterationPaths);

public record CreatedTaskInfo(int Id, string Title, double HoursEstimate);

public record GenerateTasksItemResult(int ParentId, string ParentTitle, IReadOnlyList<CreatedTaskInfo> CreatedTasks);

public record SkippedItemResult(int ParentId, string ParentTitle, string Reason);

public record GenerateTasksResult(IReadOnlyList<GenerateTasksItemResult> Created, IReadOnlyList<SkippedItemResult> Skipped);

public record RegenerateTasksResult(
    int ParentId,
    string ParentTitle,
    IReadOnlyList<CreatedTaskInfo> ClosedTasks,
    IReadOnlyList<CreatedTaskInfo> CreatedTasks);
