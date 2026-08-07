namespace GestorDeBacklogs.Api.Services;

public static class WorkItemTypeFilters
{
    // Tipos que não fazem parte do fluxo de sprint (geração de tasks, dashboard) e não devem
    // aparecer nessas listagens. Task entra aqui porque só deve aparecer aninhada sob o pai.
    public static readonly HashSet<string> ExcludedFromSprintView = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ação de Gestão",
        "Feature",
        "Epic",
        "Pacote de Trabalho",
        "Task",
    };
}
