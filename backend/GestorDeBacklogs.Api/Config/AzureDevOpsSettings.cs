namespace GestorDeBacklogs.Api.Config;

public class AzureDevOpsSettings
{
    public const string SectionName = "AzureDevOps";

    public string SizeFieldReferenceName { get; set; } = "Custom.EstimativaSize";
    public string EffortFieldReferenceName { get; set; } = "Microsoft.VSTS.Scheduling.Effort";
    public string ApiVersion { get; set; } = "7.1";
}
