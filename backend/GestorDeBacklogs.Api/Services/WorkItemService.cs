using GestorDeBacklogs.Api.Models;

namespace GestorDeBacklogs.Api.Services;

public class WorkItemService(IAzureDevOpsClient client) : IWorkItemService
{
    // Tipos que não fazem parte do fluxo de geração de tasks e não devem aparecer na listagem da sprint.
    // Task entra aqui porque ela só deve aparecer aninhada (expand) sob o pai, nunca como linha solta.
    private static readonly HashSet<string> ExcludedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ação de Gestão",
        "Feature",
        "Epic",
        "Pacote de Trabalho",
        "Task",
    };

    public async Task<IReadOnlyList<WorkItemPreviewDto>> GetSprintPreviewAsync(string iterationPath, string? areaPath, CancellationToken ct = default)
    {
        var ids = await client.QueryWorkItemIdsForIterationAsync(iterationPath, areaPath, ct);
        var workItems = await client.GetWorkItemsByIdsAsync(ids, ct);

        return workItems
            .Where(wi => !ExcludedTypes.Contains(wi.WorkItemType))
            .Select(BuildPreview)
            .ToList();
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
            var (createdResult, skippedResult) = await GenerateForItemAsync(workItem, ct);
            if (createdResult is not null)
            {
                created.Add(createdResult);
            }
            if (skippedResult is not null)
            {
                skipped.Add(skippedResult);
            }
        }

        return new GenerateTasksResult(created, skipped);
    }

    public async Task<WorkItemPreviewDto> GetWorkItemPreviewAsync(int workItemId, CancellationToken ct = default)
    {
        var workItems = await client.GetWorkItemsByIdsAsync([workItemId], ct);
        var workItem = workItems.FirstOrDefault()
            ?? throw new InvalidOperationException("Work item não encontrado.");

        return BuildPreview(workItem);
    }

    public async Task<GenerateTasksItemResult> GenerateTasksForItemAsync(int workItemId, CancellationToken ct = default)
    {
        var workItems = await client.GetWorkItemsByIdsAsync([workItemId], ct);
        var workItem = workItems.FirstOrDefault()
            ?? throw new InvalidOperationException("Work item não encontrado.");

        var (created, skipped) = await GenerateForItemAsync(workItem, ct);
        if (skipped is not null)
        {
            throw new InvalidOperationException(skipped.Reason);
        }

        return created!;
    }

    private async Task<(GenerateTasksItemResult? Created, SkippedItemResult? Skipped)> GenerateForItemAsync(WorkItemDto workItem, CancellationToken ct)
    {
        if (workItem.AlreadyHasTasks)
        {
            return (null, new SkippedItemResult(workItem.Id, workItem.Title, "Já possui tasks"));
        }

        if (workItem.EffortHours is not { } effort || !TaskSizingCalculator.TryCalculate(workItem.Title, effort, out var tasksToCreate))
        {
            return (null, new SkippedItemResult(workItem.Id, workItem.Title, "Tamanho não reconhecido"));
        }

        var createdTasks = new List<CreatedTaskInfo>();
        foreach (var task in tasksToCreate)
        {
            var newId = await client.CreateTaskAsync(workItem, task.Title, task.Hours, workItem.IterationPath, ct);
            createdTasks.Add(new CreatedTaskInfo(newId, task.Title, task.Hours));
        }

        return (new GenerateTasksItemResult(workItem.Id, workItem.Title, createdTasks), null);
    }

    public async Task<IReadOnlyList<ParentUserStoryDto>> GetChildUserStoriesAsync(int parentId, CancellationToken ct = default)
    {
        var childIds = await client.GetChildWorkItemIdsAsync(parentId, ct);
        var children = await client.GetWorkItemsByIdsAsync(childIds, ct);

        return children
            .Where(wi => wi.WorkItemType == "User Story")
            .Select(wi => new ParentUserStoryDto(wi.Id, wi.Title, wi.AssignedTo, wi.SizeLabel, wi.EffortHours, wi.AlreadyHasTasks))
            .ToList();
    }

    public async Task<IReadOnlyList<WorkItemDto>> GetChildTasksAsync(int parentId, CancellationToken ct = default)
    {
        var childIds = await client.GetChildWorkItemIdsAsync(parentId, ct);
        var children = await client.GetWorkItemsByIdsAsync(childIds, ct);

        return children.Where(wi => wi.WorkItemType == "Task").ToList();
    }

    public async Task<RegenerateTasksResult> RegenerateTasksAsync(int workItemId, CancellationToken ct = default)
    {
        var workItems = await client.GetWorkItemsByIdsAsync([workItemId], ct);
        var workItem = workItems.FirstOrDefault()
            ?? throw new InvalidOperationException("Work item não encontrado.");

        if (workItem.EffortHours is not { } effort || !TaskSizingCalculator.TryCalculate(workItem.Title, effort, out var tasksToCreate))
        {
            throw new InvalidOperationException("Tamanho não reconhecido - não é possível gerar tasks.");
        }

        var existingTasks = await GetChildTasksAsync(workItemId, ct);

        var closedTasks = new List<CreatedTaskInfo>();
        foreach (var task in existingTasks)
        {
            await client.CloseWorkItemAsync(task.Id, ct);
            closedTasks.Add(new CreatedTaskInfo(task.Id, task.Title, (int)(task.OriginalEstimate ?? 0)));
        }

        var createdTasks = new List<CreatedTaskInfo>();
        foreach (var task in tasksToCreate)
        {
            var newId = await client.CreateTaskAsync(workItem, task.Title, task.Hours, workItem.IterationPath, ct);
            createdTasks.Add(new CreatedTaskInfo(newId, task.Title, task.Hours));
        }

        return new RegenerateTasksResult(workItem.Id, workItem.Title, closedTasks, createdTasks);
    }

    public async Task<GenerateTasksResult> GenerateTasksFromParentAsync(GenerateTasksFromParentRequest request, CancellationToken ct = default)
    {
        // Reconsulta as USs (em vez de confiar no que o cliente enviou) pra garantir AreaPath/AssignedTo atuais.
        var userStories = await client.GetWorkItemsByIdsAsync(request.UserStoryIds, ct);

        var created = new List<GenerateTasksItemResult>();

        foreach (var userStory in userStories)
        {
            var createdTasks = new List<CreatedTaskInfo>();

            foreach (var iterationPath in request.IterationPaths)
            {
                var title = BuildParentTaskTitle(userStory.Title, iterationPath);
                var newId = await client.CreateTaskAsync(userStory, title, 0, iterationPath, ct);
                createdTasks.Add(new CreatedTaskInfo(newId, title, 0));
            }

            created.Add(new GenerateTasksItemResult(userStory.Id, userStory.Title, createdTasks));
        }

        return new GenerateTasksResult(created, []);
    }

    private static string BuildParentTaskTitle(string userStoryTitle, string iterationPath)
    {
        var segments = iterationPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        var sprint = segments.Length > 0 ? segments[^1] : iterationPath;
        var release = segments.Length > 1 ? segments[^2] : null;

        return release is null
            ? $"{userStoryTitle} - {sprint}"
            : $"{userStoryTitle} - {release} - {sprint}";
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
            plannedTitles,
            workItem.State);
    }
}
