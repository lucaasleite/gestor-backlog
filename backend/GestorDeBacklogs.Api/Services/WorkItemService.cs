using GestorDeBacklogs.Api.Models;

namespace GestorDeBacklogs.Api.Services;

public class WorkItemService(IAzureDevOpsClient client) : IWorkItemService
{
    public async Task<IReadOnlyList<WorkItemPreviewDto>> GetSprintPreviewAsync(string iterationPath, CancellationToken ct = default)
    {
        var ids = await client.QueryWorkItemIdsForIterationAsync(iterationPath, ct);
        var workItems = await client.GetWorkItemsByIdsAsync(ids, ct);

        return workItems.Select(BuildPreview).ToList();
    }

    public async Task<GenerateTasksResult> GenerateTasksAsync(GenerateTasksRequest request, CancellationToken ct = default)
    {
        // Reconsulta o estado atual dos work items (em vez de confiar no preview que o cliente enviou)
        // para evitar duplicar tasks caso algo tenha mudado entre a listagem e a confirmação.
        var workItems = await client.GetWorkItemsByIdsAsync(request.WorkItemIds, ct);

        var created = new List<GenerateTasksItemResult>();
        var skipped = new List<SkippedItemResult>();

        foreach (var workItem in workItems)
        {
            if (workItem.AlreadyHasTasks)
            {
                skipped.Add(new SkippedItemResult(workItem.Id, workItem.Title, "Já possui tasks"));
                continue;
            }

            if (workItem.EffortHours is not { } effort || !TaskSizingCalculator.TryCalculate(workItem.Title, effort, out var tasksToCreate))
            {
                skipped.Add(new SkippedItemResult(workItem.Id, workItem.Title, "Tamanho não reconhecido"));
                continue;
            }

            var createdTasks = new List<CreatedTaskInfo>();
            foreach (var task in tasksToCreate)
            {
                var newId = await client.CreateTaskAsync(workItem, task.Title, task.Hours, ct);
                createdTasks.Add(new CreatedTaskInfo(newId, task.Title, task.Hours));
            }

            created.Add(new GenerateTasksItemResult(workItem.Id, workItem.Title, createdTasks));
        }

        return new GenerateTasksResult(created, skipped);
    }

    private static WorkItemPreviewDto BuildPreview(WorkItemDto workItem)
    {
        var sizeRecognized = workItem.EffortHours is { } effort && TaskSizingCalculator.TryCalculate(workItem.Title, effort, out _);
        var plannedTitles = !workItem.AlreadyHasTasks && workItem.EffortHours is { } e && TaskSizingCalculator.TryCalculate(workItem.Title, e, out var tasks)
            ? tasks.Select(t => t.Title).ToList()
            : [];

        return new WorkItemPreviewDto(
            workItem.Id,
            workItem.Title,
            workItem.WorkItemType,
            workItem.SizeLabel,
            workItem.EffortHours,
            workItem.AssignedTo,
            workItem.AlreadyHasTasks,
            sizeRecognized,
            plannedTitles);
    }
}
