using System.Text.Json;
using GestorDeBacklogs.Api.Models;
using GestorDeBacklogs.Api.Security;

namespace GestorDeBacklogs.Api.Services;

public record StoredConnectionSettings(string OrganizationUrl, string Project, IReadOnlyList<TeamConfig> Teams, string? ProtectedPat);

public interface IConnectionSettingsStore
{
    StoredConnectionSettings? GetRaw();
    ConnectionSettingsResponse? GetSettings();
    string? GetDecryptedPat();
    void SaveSettings(ConnectionSettingsDto dto);
}

public class ConnectionSettingsStore : IConnectionSettingsStore
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestorBacklogs");

    private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "config.json");

    private readonly ISecretProtector _secretProtector;

    public ConnectionSettingsStore(ISecretProtector secretProtector)
    {
        _secretProtector = secretProtector;
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

    public ConnectionSettingsResponse? GetSettings()
    {
        var raw = GetRaw();
        return raw is null
            ? null
            : new ConnectionSettingsResponse(raw.OrganizationUrl, raw.Project, raw.Teams, !string.IsNullOrEmpty(raw.ProtectedPat));
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

        var stored = new StoredConnectionSettings(dto.OrganizationUrl.TrimEnd('/'), dto.Project, dto.Teams, protectedPat);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(stored));
    }
}
