using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

namespace Zenatur.LegacyBridge.Endpoints;

public static class MotoristasEndpoints
{
    public static IEndpointRouteBuilder MapMotoristasEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/legacy/motoristas").WithTags("Motoristas");
        grp.MapGet("/{cpf}", HandleGet);
        return app;
    }

    private static async Task<IResult> HandleGet(
        string cpf,
        HttpContext httpCtx,
        GetMotoristaByCpfHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetMotoristaByCpfQuery(cpf), ct);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var err = result.Errors[0];
        return err switch
        {
            NotFoundError => Results.NotFound(new { error = "Motorista não encontrado", cpf }),
            ValidationError v => Results.BadRequest(new { error = v.Message, cpf }),
            InfraError => RespondInfraError(httpCtx, cpf),
            _ => Results.Problem("Unknown error", statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static IResult RespondInfraError(HttpContext httpCtx, string cpf)
    {
        httpCtx.Response.Headers.RetryAfter = "5";
        return Results.Json(
            new { error = "Legacy DB unavailable", cpf },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
