namespace GestorDeBacklogs.Api.Endpoints;

internal static class EndpointExtensions
{
    internal static RouteGroupBuilder WithErrorHandling(this RouteGroupBuilder group)
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
