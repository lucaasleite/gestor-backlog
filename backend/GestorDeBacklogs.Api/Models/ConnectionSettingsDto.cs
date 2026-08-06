namespace GestorDeBacklogs.Api.Models;

public record TeamConfig(string Name, string AreaPath);

public record ConnectionSettingsDto(string OrganizationUrl, string Project, IReadOnlyList<TeamConfig> Teams, string? PersonalAccessToken);

public record ConnectionSettingsResponse(string OrganizationUrl, string Project, IReadOnlyList<TeamConfig> Teams, bool HasToken);
