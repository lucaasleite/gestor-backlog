using GestorDeBacklogs.Api.Models;

namespace GestorDeBacklogs.Api.Services;

public interface IWorkItemService
{
    Task<IReadOnlyList<WorkItemPreviewDto>> GetSprintPreviewAsync(string iterationPath, string? areaPath, CancellationToken ct = default);

    Task<GenerateTasksResult> GenerateTasksAsync(GenerateTasksRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<ParentUserStoryDto>> GetChildUserStoriesAsync(int parentId, CancellationToken ct = default);

    Task<GenerateTasksResult> GenerateTasksFromParentAsync(GenerateTasksFromParentRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<WorkItemDto>> GetChildTasksAsync(int parentId, CancellationToken ct = default);

    Task<RegenerateTasksResult> RegenerateTasksAsync(int workItemId, CancellationToken ct = default);

    Task<WorkItemPreviewDto> GetWorkItemPreviewAsync(int workItemId, CancellationToken ct = default);

    Task<GenerateTasksItemResult> GenerateTasksForItemAsync(int workItemId, CancellationToken ct = default);
}
