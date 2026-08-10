using System.Text.Json;
using GestorDeBacklogs.Api.Models;

namespace GestorDeBacklogs.Api.Services;

public interface IAzureDevOpsClient
{
    Task<bool> TestConnectionAsync(CancellationToken ct = default);

    Task<IReadOnlyList<IterationDto>> GetIterationsAsync(string teamName, CancellationToken ct = default);

    Task<IReadOnlyList<int>> QueryWorkItemIdsForIterationAsync(string iterationPath, string? areaPath, CancellationToken ct = default);

    Task<IReadOnlyList<WorkItemDto>> GetWorkItemsByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);

    Task<IReadOnlyList<int>> GetChildWorkItemIdsAsync(int parentId, CancellationToken ct = default);

    Task<Dictionary<string, JsonElement>> GetWorkItemRawFieldsAsync(int id, CancellationToken ct = default);

    Task<int> CreateTaskAsync(WorkItemDto parent, string title, double hours, string iterationPath, CancellationToken ct = default);

    Task CloseWorkItemAsync(int id, CancellationToken ct = default);

    Task MoveToIterationAsync(int id, string iterationPath, CancellationToken ct = default);
}
