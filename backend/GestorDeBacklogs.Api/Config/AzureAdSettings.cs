namespace GestorDeBacklogs.Api.Config;

public class AzureAdSettings
{
    public const string SectionName = "AzureAd";

    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
}
