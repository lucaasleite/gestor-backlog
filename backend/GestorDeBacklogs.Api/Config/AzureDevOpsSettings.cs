namespace GestorDeBacklogs.Api.Config;

public class AzureDevOpsSettings
{
    public const string SectionName = "AzureDevOps";

    public string SizeFieldReferenceName { get; set; } = "Custom.EstimativaSize";
    public string EffortFieldReferenceName { get; set; } = "Microsoft.VSTS.Scheduling.Effort";
    public string ApiVersion { get; set; } = "7.1";
    public string TaskClosedState { get; set; } = "Closed";

    // Tag aplicada no planning às demandas combinadas com o time; qualquer item sem essa tag
    // é considerado "Fora da Sprint" pro dashboard (ver SprintCategoryClassifier).
    public string PlannedTagName { get; set; } = "Planejado - Sprint";

    // States que contam como concluído pros indicadores de entrega do dashboard.
    public string[] DoneStates { get; set; } = ["Resolved", "Closed"];
}
