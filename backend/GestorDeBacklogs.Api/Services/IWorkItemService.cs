using GestorDeBacklogs.Api.Models;

namespace GestorDeBacklogs.Api.Services;

public interface IWorkItemService
{
    Task<IReadOnlyList<WorkItemPreviewDto>> GetSprintPreviewAsync(string iterationPath, CancellationToken ct = default);

    Task<GenerateTasksResult> GenerateTasksAsync(GenerateTasksRequest request, CancellationToken ct = default);
}
