namespace GestorDeBacklogs.Api.Models;

public record ConnectionSettingsDto(string OrganizationUrl, string Project, string Team, string? AreaPath, string? PersonalAccessToken);

public record ConnectionSettingsResponse(string OrganizationUrl, string Project, string Team, string? AreaPath, bool HasToken);
