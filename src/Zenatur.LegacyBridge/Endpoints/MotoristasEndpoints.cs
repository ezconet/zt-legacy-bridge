using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

namespace Zenatur.LegacyBridge.Endpoints;

public static class MotoristasEndpoints
{
    public static IEndpointRouteBuilder MapMotoristasEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/legacy/motoristas").WithTags("Motoristas");
        grp.MapGet("/{documento}", HandleGet);
        return app;
    }

    private static async Task<IResult> HandleGet(
        string documento,
        HttpContext httpCtx,
        GetMotoristaByCpfHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetMotoristaByCpfQuery(documento), ct);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var err = result.Errors[0];
        return err switch
        {
            NotFoundError => Results.NotFound(new { error = "Favorecido não encontrado", documento }),
            ValidationError v => Results.BadRequest(new { error = v.Message, documento }),
            InfraError => RespondInfraError(httpCtx, documento),
            _ => Results.Problem("Unknown error", statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static IResult RespondInfraError(HttpContext httpCtx, string documento)
    {
        httpCtx.Response.Headers.RetryAfter = "5";
        return Results.Json(
            new { error = "Legacy DB unavailable", documento },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
