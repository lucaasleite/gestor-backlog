using GestorDeBacklogs.Api.Services;

namespace GestorDeBacklogs.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth/entra");

        group.MapPost("/start", async (IEntraAuthService entraAuthService) =>
        {
            try
            {
                var info = await entraAuthService.StartDeviceCodeLoginAsync();
                return Results.Ok(info);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapGet("/status", (IEntraAuthService entraAuthService) =>
        {
            var status = entraAuthService.GetLoginStatus();
            return Results.Ok(new { status = status.State.ToString().ToLowerInvariant(), message = status.Message });
        });

        group.MapPost("/logout", async (IEntraAuthService entraAuthService) =>
        {
            await entraAuthService.SignOutAsync();
            return Results.Ok();
        });
    }
}
