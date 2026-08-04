using GestorDeBacklogs.Api.Models;
using GestorDeBacklogs.Api.Services;

namespace GestorDeBacklogs.Api.Endpoints;

public static class WorkItemsEndpoints
{
    public static void MapWorkItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithErrorHandling();

        group.MapGet("/sprints", async (IAzureDevOpsClient client) =>
            Results.Ok(await client.GetIterationsAsync()));

        group.MapGet("/workitems", async (string iterationPath, IWorkItemService service) =>
            Results.Ok(await service.GetSprintPreviewAsync(iterationPath)));

        group.MapPost("/workitems/generate-tasks", async (GenerateTasksRequest request, IWorkItemService service) =>
            Results.Ok(await service.GenerateTasksAsync(request)));

        group.MapGet("/workitems/{id:int}/fields", async (int id, IAzureDevOpsClient client) =>
            Results.Ok(await client.GetWorkItemRawFieldsAsync(id)));

        group.MapGet("/parent/{parentId:int}/user-stories", async (int parentId, IWorkItemService service) =>
            Results.Ok(await service.GetChildUserStoriesAsync(parentId)));

        group.MapPost("/parent/generate-tasks", async (GenerateTasksFromParentRequest request, IWorkItemService service) =>
            Results.Ok(await service.GenerateTasksFromParentAsync(request)));
    }

    private static RouteGroupBuilder WithErrorHandling(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                return await next(context);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
        });

        return group;
    }
}
