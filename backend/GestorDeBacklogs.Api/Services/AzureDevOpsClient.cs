using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestorDeBacklogs.Api.Config;
using GestorDeBacklogs.Api.Models;
using Microsoft.Extensions.Options;

namespace GestorDeBacklogs.Api.Services;

public class AzureDevOpsClient(
    IHttpClientFactory httpClientFactory,
    IConnectionSettingsStore settingsStore,
    IAzureCliAuthService azureCliAuthService,
    IOptionsSnapshot<AzureDevOpsSettings> options) : IAzureDevOpsClient
{
    private readonly AzureDevOpsSettings _settings = options.Value;

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/_apis/projects/{Uri.EscapeDataString(conn.Project)}?api-version={_settings.ApiVersion}";
        using var response = await client.GetAsync(url, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<IterationDto>> GetIterationsAsync(string teamName, CancellationToken ct = default)
    {
        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/{Uri.EscapeDataString(teamName)}/_apis/work/teamsettings/iterations?api-version={_settings.ApiVersion}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var result = new List<IterationDto>();
        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            string? timeFrame = null;
            DateTime? finishDate = null;
            if (item.TryGetProperty("attributes", out var attrs))
            {
                timeFrame = attrs.TryGetProperty("timeFrame", out var tf) ? tf.GetString() : null;
                finishDate = attrs.TryGetProperty("finishDate", out var fd) && fd.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(fd.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind)
                    : null;
            }

            result.Add(new IterationDto(
                item.GetProperty("id").GetString()!,
                item.GetProperty("name").GetString()!,
                item.GetProperty("path").GetString()!,
                string.Equals(timeFrame, "current", StringComparison.OrdinalIgnoreCase),
                finishDate));
        }

        return result;
    }

    public async Task<IReadOnlyList<int>> QueryWorkItemIdsForIterationAsync(string iterationPath, string? areaPath, CancellationToken ct = default)
    {
        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/wiql?api-version={_settings.ApiVersion}";

        var escapedIterationPath = iterationPath.Replace("'", "''");
        var escapedProject = conn.Project.Replace("'", "''");
        var query = "SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '" + escapedProject + "' " +
                    "AND [System.IterationPath] = '" + escapedIterationPath + "' AND [System.WorkItemType] <> ''";

        if (!string.IsNullOrWhiteSpace(areaPath))
        {
            var escapedAreaPath = areaPath.Replace("'", "''");
            query += " AND [System.AreaPath] UNDER '" + escapedAreaPath + "'";
        }

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

    // A API workitemsbatch do Azure DevOps rejeita requisicoes com mais de 200 ids.
    private const int WorkItemsBatchSize = 200;

    public async Task<IReadOnlyList<WorkItemDto>> GetWorkItemsByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitemsbatch?api-version={_settings.ApiVersion}";

        var result = new List<WorkItemDto>();
        foreach (var chunk in ids.Chunk(WorkItemsBatchSize))
        {
            // A API workitemsbatch exige a propriedade "$expand" (não "expand") pra incluir relations
            // na resposta; sem isso ela ignora o campo silenciosamente e AlreadyHasTasks nunca é detectado.
            using var content = JsonContent(new WorkItemsBatchRequestBody(chunk, "relations"));
            using var response = await client.PostAsync(url, content, ct);
            await EnsureSuccessAsync(response, ct);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
            {
                result.Add(ParseWorkItem(item));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<int>> GetChildWorkItemIdsAsync(int parentId, CancellationToken ct = default)
    {
        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitems/{parentId}?$expand=relations&api-version={_settings.ApiVersion}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var ids = new List<int>();

        if (doc.RootElement.TryGetProperty("relations", out var relations) && relations.ValueKind == JsonValueKind.Array)
        {
            foreach (var relation in relations.EnumerateArray())
            {
                var isChild = relation.TryGetProperty("rel", out var rel) && rel.GetString() == "System.LinkTypes.Hierarchy-Forward";
                if (isChild && relation.TryGetProperty("url", out var relUrl))
                {
                    var urlStr = relUrl.GetString()!;
                    var idStr = urlStr[(urlStr.LastIndexOf('/') + 1)..];
                    if (int.TryParse(idStr, out var childId))
                    {
                        ids.Add(childId);
                    }
                }
            }
        }

        return ids;
    }

    public async Task<Dictionary<string, JsonElement>> GetWorkItemRawFieldsAsync(int id, CancellationToken ct = default)
    {
        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitems/{id}?api-version={_settings.ApiVersion}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        return doc.RootElement.GetProperty("fields")
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    public async Task<int> CreateTaskAsync(WorkItemDto parent, string title, double hours, string iterationPath, CancellationToken ct = default)
    {
        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitems/$Task?api-version={_settings.ApiVersion}";

        var ops = new List<object>
        {
            new { op = "add", path = "/fields/System.Title", value = title },
            new { op = "add", path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate", value = hours },
            new { op = "add", path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork", value = hours },
            new { op = "add", path = "/fields/System.IterationPath", value = iterationPath },
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

    public async Task CloseWorkItemAsync(int id, CancellationToken ct = default)
    {
        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitems/{id}?api-version={_settings.ApiVersion}";

        var ops = new object[]
        {
            new { op = "add", path = "/fields/System.State", value = _settings.TaskClosedState },
        };

        using var content = new StringContent(JsonSerializer.Serialize(ops), Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");

        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        using var response = await client.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task MoveToIterationAsync(int id, string iterationPath, CancellationToken ct = default)
    {
        var (client, conn) = await GetConfiguredClientAsync(ct);
        var url = $"{conn.OrganizationUrl}/{Uri.EscapeDataString(conn.Project)}/_apis/wit/workitems/{id}?api-version={_settings.ApiVersion}";

        var ops = new object[]
        {
            new { op = "add", path = "/fields/System.IterationPath", value = iterationPath },
        };

        using var content = new StringContent(JsonSerializer.Serialize(ops), Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");

        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        using var response = await client.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private WorkItemDto ParseWorkItem(JsonElement item)
    {
        var fields = item.GetProperty("fields");

        var assignedTo = fields.TryGetProperty("System.AssignedTo", out var assignedEl)
            ? ExtractIdentity(assignedEl)
            : null;

        string? sizeLabel = null;
        if (fields.TryGetProperty(_settings.SizeFieldReferenceName, out var sizeEl) &&
            sizeEl.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
        {
            sizeLabel = sizeEl.ValueKind == JsonValueKind.String ? sizeEl.GetString() : sizeEl.ToString();
        }

        int? effortHours = fields.TryGetProperty(_settings.EffortFieldReferenceName, out var effortEl) && effortEl.ValueKind is JsonValueKind.Number
            ? (int)effortEl.GetDouble()
            : null;

        var tags = fields.TryGetProperty("System.Tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.String
            ? tagsEl.GetString()
            : null;

        (sizeLabel, effortHours) = SizeTagResolver.Resolve(sizeLabel, effortHours, tags);

        var tagList = tags?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        var originalEstimate = ReadDouble(fields, "Microsoft.VSTS.Scheduling.OriginalEstimate");
        var remainingWork = ReadDouble(fields, "Microsoft.VSTS.Scheduling.RemainingWork");
        var completedWork = ReadDouble(fields, "Microsoft.VSTS.Scheduling.CompletedWork");
        var state = fields.TryGetProperty("System.State", out var stateEl) && stateEl.ValueKind == JsonValueKind.String
            ? stateEl.GetString()
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
            alreadyHasTasks,
            originalEstimate,
            remainingWork,
            completedWork,
            state,
            tagList);
    }

    private static double? ReadDouble(JsonElement fields, string fieldName) =>
        fields.TryGetProperty(fieldName, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetDouble() : null;

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

    private async Task<(HttpClient client, StoredConnectionSettings settings)> GetConfiguredClientAsync(CancellationToken ct)
    {
        var settings = settingsStore.GetRaw();
        if (settings is null)
        {
            throw new InvalidOperationException("Configuração de conexão com o Azure DevOps não encontrada. Configure a Organização, Projeto e Team primeiro.");
        }

        var client = httpClientFactory.CreateClient("AzureDevOps");

        if (settings.AuthMode == AuthMode.Sso)
        {
            var accessToken = await azureCliAuthService.GetAccessTokenAsync(ct);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.ProtectedPat))
            {
                throw new InvalidOperationException("PAT não configurado. Configure o Personal Access Token primeiro.");
            }

            var pat = settingsStore.GetDecryptedPat();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}")));
        }

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

    private record WorkItemsBatchRequestBody(
        [property: JsonPropertyName("ids")] int[] Ids,
        [property: JsonPropertyName("$expand")] string Expand);
}
