using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

namespace Zenatur.LegacyBridge.Endpoints;

public static class VeiculosEndpoints
{
    public static IEndpointRouteBuilder MapVeiculosEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/legacy/veiculos").WithTags("Veiculos");
        grp.MapGet("/{placa}", HandleGet);
        return app;
    }

    private static async Task<IResult> HandleGet(
        string placa,
        HttpContext httpCtx,
        GetVeiculoByPlacaHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetVeiculoByPlacaQuery(placa), ct);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var err = result.Errors[0];
        return err switch
        {
            NotFoundError => Results.NotFound(new { error = "Veículo não encontrado", placa }),
            ValidationError v => Results.BadRequest(new { error = v.Message, placa }),
            InfraError => RespondInfraError(httpCtx, placa),
            _ => Results.Problem("Unknown error", statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static IResult RespondInfraError(HttpContext httpCtx, string placa)
    {
        httpCtx.Response.Headers.RetryAfter = "5";
        return Results.Json(
            new { error = "Legacy DB unavailable", placa },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
