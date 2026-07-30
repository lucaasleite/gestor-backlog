namespace GestorDeBacklogs.Api.Models;

public record GenerateTasksRequest(string IterationPath, IReadOnlyList<int> WorkItemIds);

public record CreatedTaskInfo(int Id, string Title, int HoursEstimate);

public record GenerateTasksItemResult(int ParentId, string ParentTitle, IReadOnlyList<CreatedTaskInfo> CreatedTasks);

public record SkippedItemResult(int ParentId, string ParentTitle, string Reason);

public record GenerateTasksResult(IReadOnlyList<GenerateTasksItemResult> Created, IReadOnlyList<SkippedItemResult> Skipped);
