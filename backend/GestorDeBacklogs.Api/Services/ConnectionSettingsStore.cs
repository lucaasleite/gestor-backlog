using System.Text.Json;
using GestorDeBacklogs.Api.Models;
using GestorDeBacklogs.Api.Security;

namespace GestorDeBacklogs.Api.Services;

public record StoredConnectionSettings(string OrganizationUrl, string Project, IReadOnlyList<TeamConfig> Teams, AuthMode AuthMode, string? ProtectedPat);

public interface IConnectionSettingsStore
{
    StoredConnectionSettings? GetRaw();
    Task<ConnectionSettingsResponse?> GetSettingsAsync();
    string? GetDecryptedPat();
    void SaveSettings(ConnectionSettingsDto dto);
}

public class ConnectionSettingsStore : IConnectionSettingsStore
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestorBacklogs");

    private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "config.json");

    private readonly ISecretProtector _secretProtector;
    private readonly IEntraAuthService _entraAuthService;

    public ConnectionSettingsStore(ISecretProtector secretProtector, IEntraAuthService entraAuthService)
    {
        _secretProtector = secretProtector;
        _entraAuthService = entraAuthService;
    }

    public StoredConnectionSettings? GetRaw()
    {
        if (!File.Exists(ConfigPath))
        {
            return null;
        }

        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<StoredConnectionSettings>(json);
    }

    public async Task<ConnectionSettingsResponse?> GetSettingsAsync()
    {
        var raw = GetRaw();
        if (raw is null)
        {
            return null;
        }

        var hasToken = raw.AuthMode == AuthMode.Sso
            ? await _entraAuthService.IsSignedInAsync()
            : !string.IsNullOrEmpty(raw.ProtectedPat);

        return new ConnectionSettingsResponse(raw.OrganizationUrl, raw.Project, raw.Teams, raw.AuthMode, hasToken);
    }

    public string? GetDecryptedPat()
    {
        var raw = GetRaw();
        return raw?.ProtectedPat is null ? null : _secretProtector.Unprotect(raw.ProtectedPat);
    }

    public void SaveSettings(ConnectionSettingsDto dto)
    {
        Directory.CreateDirectory(ConfigDirectory);

        var protectedPat = !string.IsNullOrWhiteSpace(dto.PersonalAccessToken)
            ? _secretProtector.Protect(dto.PersonalAccessToken)
            : GetRaw()?.ProtectedPat;

        var stored = new StoredConnectionSettings(dto.OrganizationUrl.TrimEnd('/'), dto.Project, dto.Teams, dto.AuthMode, protectedPat);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(stored));
    }
}
