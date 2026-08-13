namespace GestorDeBacklogs.Api.Models;

public enum AuthMode { Pat, Sso }

public record TeamConfig(string Name, string AreaPath);

public record ConnectionSettingsDto(string OrganizationUrl, string Project, IReadOnlyList<TeamConfig> Teams, AuthMode AuthMode, string? PersonalAccessToken);

public record ConnectionSettingsResponse(string OrganizationUrl, string Project, IReadOnlyList<TeamConfig> Teams, AuthMode AuthMode, bool HasToken);
