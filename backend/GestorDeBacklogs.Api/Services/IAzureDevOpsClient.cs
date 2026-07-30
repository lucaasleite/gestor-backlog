using System.Text.Json;
using GestorDeBacklogs.Api.Models;

namespace GestorDeBacklogs.Api.Services;

public interface IAzureDevOpsClient
{
    Task<bool> TestConnectionAsync(CancellationToken ct = default);

    Task<IReadOnlyList<IterationDto>> GetIterationsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<int>> QueryWorkItemIdsForIterationAsync(string iterationPath, CancellationToken ct = default);

    Task<IReadOnlyList<WorkItemDto>> GetWorkItemsByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);

    Task<Dictionary<string, JsonElement>> GetWorkItemRawFieldsAsync(int id, CancellationToken ct = default);

    Task<int> CreateTaskAsync(WorkItemDto parent, string title, int hours, CancellationToken ct = default);
}
