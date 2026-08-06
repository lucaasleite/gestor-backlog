using GestorDeBacklogs.Api.Models;
using GestorDeBacklogs.Api.Services;

namespace GestorDeBacklogs.Api.Endpoints;

public static class WorkItemsEndpoints
{
    public static void MapWorkItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithErrorHandling();

        group.MapGet("/sprints", async (string team, IAzureDevOpsClient client) =>
            Results.Ok(await client.GetIterationsAsync(team)));

        group.MapGet("/workitems", async (string iterationPath, string? areaPath, IWorkItemService service) =>
            Results.Ok(await service.GetSprintPreviewAsync(iterationPath, areaPath)));

        group.MapPost("/workitems/generate-tasks", async (GenerateTasksRequest request, IWorkItemService service) =>
            Results.Ok(await service.GenerateTasksAsync(request)));

        group.MapGet("/workitems/{id:int}/fields", async (int id, IAzureDevOpsClient client) =>
            Results.Ok(await client.GetWorkItemRawFieldsAsync(id)));

        group.MapGet("/workitems/{id:int}/tasks", async (int id, IWorkItemService service) =>
            Results.Ok(await service.GetChildTasksAsync(id)));

        group.MapPost("/workitems/{id:int}/regenerate-tasks", async (int id, IWorkItemService service) =>
            Results.Ok(await service.RegenerateTasksAsync(id)));

        group.MapPost("/workitems/{id:int}/close", async (int id, IAzureDevOpsClient client) =>
        {
            await client.CloseWorkItemAsync(id);
            return Results.Ok();
        });

        group.MapGet("/workitems/{id:int}/preview", async (int id, IWorkItemService service) =>
            Results.Ok(await service.GetWorkItemPreviewAsync(id)));

        group.MapPost("/workitems/{id:int}/generate-tasks", async (int id, GenerateTaskForItemRequest? request, IWorkItemService service) =>
            Results.Ok(await service.GenerateTasksForItemAsync(id, request?.Tasks)));

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
