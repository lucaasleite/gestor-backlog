using GestorDeBacklogs.Api.Models;
using GestorDeBacklogs.Api.Services;

namespace GestorDeBacklogs.Api.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config");

        group.MapGet("", (IConnectionSettingsStore store) =>
        {
            var settings = store.GetSettings();
            return settings is null ? Results.NotFound() : Results.Ok(settings);
        });

        group.MapPost("", (ConnectionSettingsDto dto, IConnectionSettingsStore store) =>
        {
            store.SaveSettings(dto);
            return Results.Ok(store.GetSettings());
        });

        group.MapPost("/test-connection", async (IAzureDevOpsClient client) =>
        {
            try
            {
                var ok = await client.TestConnectionAsync();
                return ok ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Não foi possível conectar." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        });
    }
}
