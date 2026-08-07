using System.Collections.Concurrent;
using System.Text.Json;
using GestorDeBacklogs.Api.Models;

namespace GestorDeBacklogs.Api.Services;

// Usado apenas quando a variável de ambiente MOCK_DATA=true está definida (ver Program.cs),
// para permitir rodar a UI localmente com dados fake, sem depender de um Azure DevOps real.
// Mantém estado em memória (dicionários estáticos) pra que criar/fechar tasks durante o uso
// da UI se reflita nas próximas consultas, como aconteceria com o Azure DevOps real.
//
// Os itens estão distribuídos entre dois Area Paths de exemplo (mesmos do pedido original):
// "Ailos\Gerencia de SRE\Plataforma e DevOps" e "Ailos\Sre\Sre" — cadastre times com esses
// Area Paths na tela de Configuração pra ver o filtro por time funcionando.
public class MockAzureDevOpsClient : IAzureDevOpsClient
{
    private const string AreaPlataformaDevOps = @"Ailos\Gerencia de SRE\Plataforma e DevOps";
    private const string AreaSre = @"Ailos\Sre\Sre";

    private static WorkItemDto Build(
        int id, string title, string type, string? sizeLabel, int? effortHours, string? tags,
        string? assignedTo, string iterationPath, string areaPath, bool alreadyHasTasks = false,
        double? originalEstimate = null, double? remainingWork = null, double? completedWork = null,
        string? state = null)
    {
        var (resolvedSize, resolvedEffort) = SizeTagResolver.Resolve(sizeLabel, effortHours, tags);
        var tagList = tags?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        return new WorkItemDto(
            id, title, type, resolvedSize, resolvedEffort, assignedTo, iterationPath, areaPath,
            $"https://dev.azure.com/mock/_apis/wit/workItems/{id}", alreadyHasTasks,
            originalEstimate, remainingWork, completedWork, state ?? (type == "Task" ? "To Do" : "New"), tagList);
    }

    // Ids dos itens que aparecem na tela "Work items da sprint" (o que a query WIQL por iteração devolveria).
    private static readonly HashSet<int> SprintItemIds =
        [101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 121, 122, 123, 201, 202];

    // Tags usadas pra alimentar o dashboard de "Planejado vs Fora da Sprint" em MOCK_DATA=true:
    // itens com a tag "Planejado - Sprint" contam como planejado; os demais (104, 111, 123, 202) viram Fora da Sprint.
    private static readonly ConcurrentDictionary<int, WorkItemDto> Items = new(
    [
        // Sprint 23 — usada só pra dar histórico à tendência do dashboard.
        KeyValue(Build(121, "Como usuário, quero exportar dados em CSV", "Product Backlog Item", "M", 8, "Planejado - Sprint", "ana@empresa.com", @"GestorBacklogs\Sprint 23", AreaPlataformaDevOps, state: "Closed")),
        KeyValue(Build(122, "Corrigir timeout na integração com SSO", "Bug", "P", 4, "Planejado - Sprint", "bruno@empresa.com", @"GestorBacklogs\Sprint 23", AreaPlataformaDevOps, state: "Resolved")),
        // Sem a tag de planejamento — chegou fora do planning.
        KeyValue(Build(123, "Acesso emergencial a produção", "Requisição", null, null, "#PP", "carla@empresa.com", @"GestorBacklogs\Sprint 23", AreaSre, state: "Resolved")),
        // Sprint 24 — Plataforma e DevOps
        KeyValue(Build(101, "Como usuário, quero fazer login com SSO", "Product Backlog Item", "M", 8, "Planejado - Sprint", "ana@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, state: "Active")),
        KeyValue(Build(102, "Como usuário, quero recuperar minha senha", "Product Backlog Item", "P", 4, "Planejado - Sprint", "bruno@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, alreadyHasTasks: true, state: "Resolved")),
        KeyValue(Build(103, "Como admin, quero exportar relatório de backlog", "Product Backlog Item", "G", 16, "Planejado - Sprint", "carla@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, state: "Proposed")),
        // Sem tamanho no campo — deve ser resolvido pela tag #GG. Sem a tag de planejamento — fora da sprint.
        KeyValue(Build(111, "Como usuário, quero gerenciar tags favoritas", "Product Backlog Item", null, null, "Prioridade Alta; #GG", "diego@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, state: "Active")),
        // Sprint 24 — SRE
        // Sem a tag de planejamento e sem responsável — fora da sprint.
        KeyValue(Build(104, "Corrigir erro de arredondamento no cálculo de estimativa", "Bug", "M", 8, null, null, @"GestorBacklogs\Sprint 24", AreaSre, state: "Resolved")),
        KeyValue(Build(105, "Como usuário, quero filtrar itens por responsável", "Product Backlog Item", "GG", 24, "Planejado - Sprint", "ana@empresa.com", @"GestorBacklogs\Sprint 24", AreaSre, state: "Resolved")),
        // Sem tamanho no campo — deve ser resolvido pela tag #M.
        KeyValue(Build(106, "Como usuário, quero ver o histórico de alterações", "Product Backlog Item", null, null, "#M; Planejado - Sprint", "bruno@empresa.com", @"GestorBacklogs\Sprint 24", AreaSre, state: "Resolved")),
        // Tipos que a tela não deve trazer.
        KeyValue(Build(107, "Reunião de alinhamento com o PO", "Ação de Gestão", null, null, null, "ana@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps)),
        KeyValue(Build(108, "Notificações por e-mail", "Feature", null, null, null, null, @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps)),
        KeyValue(Build(109, "Onboarding de novos usuários", "Epic", null, null, null, null, @"GestorBacklogs\Sprint 24", AreaSre)),
        KeyValue(Build(110, "Pacote Infra Q3", "Pacote de Trabalho", null, null, null, null, @"GestorBacklogs\Sprint 24", AreaSre)),
        // Sprint 25
        KeyValue(Build(201, "Como usuário, quero receber notificações por e-mail", "Product Backlog Item", "M", 8, "Planejado - Sprint", "diego@empresa.com", @"GestorBacklogs\Sprint 25", AreaPlataformaDevOps, state: "Proposed")),
        KeyValue(Build(202, "Como usuário, quero integrar com Microsoft Teams", "Product Backlog Item", "GG", 40, null, "carla@empresa.com", @"GestorBacklogs\Sprint 25", AreaSre, state: "Active")),
        // User Stories filhas de um parent (Epic/Feature/Pacote de Trabalho), usadas na tela "Tasks por parent".
        KeyValue(Build(301, "Como admin, quero escolher o formato do relatório (PDF/Excel)", "User Story", "P", 4, null, "carla@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, state: "Active")),
        KeyValue(Build(302, "Como admin, quero agendar o envio periódico do relatório", "User Story", "M", 8, null, "diego@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, alreadyHasTasks: true, state: "Active")),
        KeyValue(Build(303, "Como usuário, quero salvar filtros favoritos", "User Story", null, null, "#P", "ana@empresa.com", @"GestorBacklogs\Sprint 24", AreaSre, state: "Proposed")),
        // Tasks já existentes, filhas de alguns dos itens acima (usadas na expansão de linha).
        KeyValue(Build(5001, "Como usuário, quero recuperar minha senha", "Task", null, null, null, "bruno@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, originalEstimate: 4, remainingWork: 1, completedWork: 3, state: "Doing")),
        KeyValue(Build(4001, "Como admin, quero agendar o envio periódico do relatório", "Task", null, null, null, "diego@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, originalEstimate: 8, remainingWork: 8, completedWork: 0, state: "To Do")),
        // Itens avulsos (fora de qualquer sprint listada), usados na tela "Tasks por item único" — outros tipos além de PBI/Bug.
        KeyValue(Build(601, "Erro ao carregar dashboard financeiro", "Incidente", "GG", 24, null, "carla@empresa.com", @"GestorBacklogs\Sprint 24", AreaSre, state: "Active")),
        KeyValue(Build(602, "Acesso ao ambiente de homologação", "Requisição", null, null, "#P", "bruno@empresa.com", @"GestorBacklogs\Sprint 24", AreaPlataformaDevOps, state: "Proposed")),
        KeyValue(Build(603, "Migrar rotina de billing pro novo cluster", "User Story", "M", 8, null, "diego@empresa.com", @"GestorBacklogs\Sprint 24", AreaSre, alreadyHasTasks: true, state: "Active")),
        KeyValue(Build(6001, "Migrar rotina de billing pro novo cluster", "Task", null, null, null, "diego@empresa.com", @"GestorBacklogs\Sprint 24", AreaSre, originalEstimate: 8, remainingWork: 2, completedWork: 6, state: "Doing")),
    ]);

    private static readonly ConcurrentDictionary<int, List<int>> ChildrenByParent = new(
        new Dictionary<int, List<int>>
        {
            [103] = [301, 302],
            [105] = [303],
            [603] = [6001],
            [102] = [5001],
            [302] = [4001],
        });

    private static int _nextId = 9000;

    private static KeyValuePair<int, WorkItemDto> KeyValue(WorkItemDto item) => new(item.Id, item);

    public Task<bool> TestConnectionAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task<IReadOnlyList<IterationDto>> GetIterationsAsync(string teamName, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return Task.FromResult<IReadOnlyList<IterationDto>>(
        [
            new IterationDto("iter-23", "Sprint 23", @"GestorBacklogs\Sprint 23", false, today.AddDays(-4)),
            new IterationDto("iter-24", "Sprint 24", @"GestorBacklogs\Sprint 24", true, today.AddDays(10)),
            new IterationDto("iter-25", "Sprint 25", @"GestorBacklogs\Sprint 25", false, today.AddDays(24)),
        ]);
    }

    public Task<IReadOnlyList<int>> QueryWorkItemIdsForIterationAsync(string iterationPath, string? areaPath, CancellationToken ct = default)
    {
        var ids = Items.Values
            .Where(w => SprintItemIds.Contains(w.Id) && w.IterationPath == iterationPath)
            .Where(w => string.IsNullOrWhiteSpace(areaPath) || w.AreaPath.StartsWith(areaPath, StringComparison.OrdinalIgnoreCase))
            .Select(w => w.Id)
            .ToList();
        return Task.FromResult<IReadOnlyList<int>>(ids);
    }

    public Task<IReadOnlyList<WorkItemDto>> GetWorkItemsByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        var result = ids.Where(Items.ContainsKey).Select(id => Items[id]).ToList();
        return Task.FromResult<IReadOnlyList<WorkItemDto>>(result);
    }

    public Task<IReadOnlyList<int>> GetChildWorkItemIdsAsync(int parentId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>(ChildrenByParent.TryGetValue(parentId, out var ids) ? ids : []);

    public Task<Dictionary<string, JsonElement>> GetWorkItemRawFieldsAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, JsonElement>());

    public Task<int> CreateTaskAsync(WorkItemDto parent, string title, double hours, string iterationPath, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _nextId);
        var task = Build(id, title, "Task", null, null, null, parent.AssignedTo, iterationPath, parent.AreaPath, originalEstimate: hours, remainingWork: hours, completedWork: 0);
        Items[id] = task;

        ChildrenByParent.AddOrUpdate(parent.Id, [id], (_, existing) => [.. existing, id]);

        if (Items.TryGetValue(parent.Id, out var parentItem))
        {
            Items[parent.Id] = parentItem with { AlreadyHasTasks = true };
        }

        return Task.FromResult(id);
    }

    public Task CloseWorkItemAsync(int id, CancellationToken ct = default)
    {
        if (Items.TryGetValue(id, out var item))
        {
            Items[id] = item with { State = "Closed" };
        }

        return Task.CompletedTask;
    }
}
