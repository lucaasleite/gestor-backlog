using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GestorDeBacklogs.Api.Config;
using GestorDeBacklogs.Api.Models;
using Microsoft.Extensions.Options;

namespace GestorDeBacklogs.Api.Services;

public class AzureDevOpsClient(
    IHttpClientFactory httpClientFactory,
    IConnectionSettingsStore settingsStore,
    IOptionsSnapshot<AzureDevOpsSettings> options) : IAzureDevOpsClient
{
    private readonly AzureDevOpsSettings _settings = options.Value;

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        var (client, conn) = GetConfiguredClient();
        var url = $"{conn.OrganizationUrl}/_apis/projects/{Uri.EscapeDataString(conn.Project)}?api-version={_settings.ApiVersion}";
        using var response = await client.GetAsync(url, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<IterationDto>> GetIterationsAsync(CancellationToken ct = default)
    {
        var (client, conn) = GetConfiguredClient();
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/{Uri.EscapeDataString(conn.Team)}/_apis/work/teamsettings/iterations?api-version={_settings.ApiVersion}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var result = new List<IterationDto>();
        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            var timeFrame = item.TryGetProperty("attributes", out var attrs) && attrs.TryGetProperty("timeFrame", out var tf)
                ? tf.GetString()
                : null;

            result.Add(new IterationDto(
                item.GetProperty("id").GetString()!,
                item.GetProperty("name").GetString()!,
                item.GetProperty("path").GetString()!,
                string.Equals(timeFrame, "current", StringComparison.OrdinalIgnoreCase)));
        }

        return result;
    }

    public async Task<IReadOnlyList<int>> QueryWorkItemIdsForIterationAsync(string iterationPath, CancellationToken ct = default)
    {
        var (client, conn) = GetConfiguredClient();
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/wiql?api-version={_settings.ApiVersion}";

        var escapedIterationPath = iterationPath.Replace("'", "''");
        var escapedProject = conn.Project.Replace("'", "''");
        var query = "SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '" + escapedProject + "' " +
                    "AND [System.IterationPath] = '" + escapedIterationPath + "' AND [System.WorkItemType] <> ''";

        using var content = JsonContent(new { query });
        using var response = await client.PostAsync(url, content, ct);
        await EnsureSuccessAsync(response, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var ids = new List<int>();
        foreach (var item in doc.RootElement.GetProperty("workItems").EnumerateArray())
        {
            ids.Add(item.GetProperty("id").GetInt32());
        }

        return ids;
    }

    public async Task<IReadOnlyList<WorkItemDto>> GetWorkItemsByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var (client, conn) = GetConfiguredClient();
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitemsbatch?api-version={_settings.ApiVersion}";

        using var content = JsonContent(new { ids, expand = "relations" });
        using var response = await client.PostAsync(url, content, ct);
        await EnsureSuccessAsync(response, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var result = new List<WorkItemDto>();
        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            result.Add(ParseWorkItem(item));
        }

        return result;
    }

    public async Task<Dictionary<string, JsonElement>> GetWorkItemRawFieldsAsync(int id, CancellationToken ct = default)
    {
        var (client, conn) = GetConfiguredClient();
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitems/{id}?api-version={_settings.ApiVersion}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        return doc.RootElement.GetProperty("fields")
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    public async Task<int> CreateTaskAsync(WorkItemDto parent, string title, int hours, CancellationToken ct = default)
    {
        var (client, conn) = GetConfiguredClient();
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitems/$Task?api-version={_settings.ApiVersion}";

        var ops = new List<object>
        {
            new { op = "add", path = "/fields/System.Title", value = title },
            new { op = "add", path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate", value = hours },
            new { op = "add", path = "/fields/System.IterationPath", value = parent.IterationPath },
            new { op = "add", path = "/fields/System.AreaPath", value = parent.AreaPath },
        };

        if (!string.IsNullOrWhiteSpace(parent.AssignedTo))
        {
            ops.Add(new { op = "add", path = "/fields/System.AssignedTo", value = parent.AssignedTo });
        }

        ops.Add(new
        {
            op = "add",
            path = "/relations/-",
            value = new { rel = "System.LinkTypes.Hierarchy-Reverse", url = parent.Url }
        });

        using var content = new StringContent(JsonSerializer.Serialize(ops), Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");

        using var response = await client.PostAsync(url, content, ct);
        await EnsureSuccessAsync(response, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    private WorkItemDto ParseWorkItem(JsonElement item)
    {
        var fields = item.GetProperty("fields");

        var assignedTo = fields.TryGetProperty("System.AssignedTo", out var assignedEl)
            ? ExtractIdentity(assignedEl)
            : null;

        var sizeLabel = fields.TryGetProperty(_settings.SizeFieldReferenceName, out var sizeEl)
            ? sizeEl.ToString()
            : null;

        int? effortHours = fields.TryGetProperty(_settings.EffortFieldReferenceName, out var effortEl) && effortEl.ValueKind is JsonValueKind.Number
            ? (int)effortEl.GetDouble()
            : null;

        // Assumimos que qualquer link "filho" já existente é uma Task gerada por este fluxo
        // (não distinguimos o tipo do item relacionado para evitar uma chamada extra por item).
        var alreadyHasTasks = item.TryGetProperty("relations", out var relations) &&
            relations.ValueKind == JsonValueKind.Array &&
            relations.EnumerateArray().Any(r => r.TryGetProperty("rel", out var rel) && rel.GetString() == "System.LinkTypes.Hierarchy-Forward");

        return new WorkItemDto(
            item.GetProperty("id").GetInt32(),
            fields.GetProperty("System.Title").GetString()!,
            fields.GetProperty("System.WorkItemType").GetString()!,
            sizeLabel,
            effortHours,
            assignedTo,
            fields.GetProperty("System.IterationPath").GetString()!,
            fields.GetProperty("System.AreaPath").GetString()!,
            item.GetProperty("url").GetString()!,
            alreadyHasTasks);
    }

    private static string? ExtractIdentity(JsonElement assignedEl)
    {
        if (assignedEl.ValueKind != JsonValueKind.Object)
        {
            return assignedEl.ValueKind == JsonValueKind.String ? assignedEl.GetString() : null;
        }

        if (assignedEl.TryGetProperty("uniqueName", out var uniqueName))
        {
            return uniqueName.GetString();
        }

        return assignedEl.TryGetProperty("displayName", out var displayName) ? displayName.GetString() : null;
    }

    private (HttpClient client, StoredConnectionSettings settings) GetConfiguredClient()
    {
        var settings = settingsStore.GetRaw();
        if (settings is null || string.IsNullOrWhiteSpace(settings.ProtectedPat))
        {
            throw new InvalidOperationException("Configuração de conexão com o Azure DevOps não encontrada. Configure a Organização, Projeto, Team e PAT primeiro.");
        }

        var pat = settingsStore.GetDecryptedPat();
        var client = httpClientFactory.CreateClient("AzureDevOps");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}")));

        return (client, settings);
    }

    private static StringContent JsonContent(object payload)
    {
        return new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"Azure DevOps API retornou {(int)response.StatusCode} {response.StatusCode}: {body}");
    }
}
