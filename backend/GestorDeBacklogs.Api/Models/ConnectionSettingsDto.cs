namespace GestorDeBacklogs.Api.Models;

public record ConnectionSettingsDto(string OrganizationUrl, string Project, string Team, string? PersonalAccessToken);

public record ConnectionSettingsResponse(string OrganizationUrl, string Project, string Team, bool HasToken);
