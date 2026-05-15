using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Queries.GetMinutaByNumero;

namespace Zenatur.LegacyBridge.Endpoints;

public static class MinutasEndpoints
{
    public static IEndpointRouteBuilder MapMinutasEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/legacy/minutas").WithTags("Minutas");
        grp.MapGet("/{numeroMinuta}", HandleGet);
        return app;
    }

    private static async Task<IResult> HandleGet(
        string numeroMinuta,
        HttpContext httpCtx,
        GetMinutaByNumeroHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetMinutaByNumeroQuery(numeroMinuta), ct);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var err = result.Errors[0];
        return err switch
        {
            NotFoundError => Results.NotFound(new { error = "Minuta não encontrada", numeroMinuta }),
            ValidationError v => Results.BadRequest(new { error = v.Message, numeroMinuta }),
            InfraError => RespondInfraError(httpCtx, numeroMinuta),
            _ => Results.Problem("Unknown error", statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static IResult RespondInfraError(HttpContext httpCtx, string numeroMinuta)
    {
        httpCtx.Response.Headers.RetryAfter = "5";
        return Results.Json(
            new { error = "Legacy DB unavailable", numeroMinuta },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
