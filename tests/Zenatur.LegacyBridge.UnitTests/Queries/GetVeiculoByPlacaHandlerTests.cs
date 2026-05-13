using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;
using Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

namespace Zenatur.LegacyBridge.UnitTests.Queries;

public class GetVeiculoByPlacaHandlerTests
{
    private static readonly VeiculoDto Sample = new(
        Placa: "ABC1D23",
        TipoVeiculo: new TipoVeiculoDto(1, "CAMINHAO 3/4"),
        Renavam: "00123456789",
        AnoFabricacao: 2018,
        AnoModelo: 2018,
        Marca: "VOLVO",
        Modelo: "FH 540",
        Tara: 8500m,
        CapacidadeKg: 25000m,
        Rntrc: "12345678",
        Proprietario: new ProprietarioDto("12345678000199", "CNPJ", "TRANSPORTES X LTDA"));

    private static (Mock<IVeiculoRepository>, GetVeiculoByPlacaHandler) Build(Result<VeiculoDto?>? result = null)
    {
        var repo = new Mock<IVeiculoRepository>();
        repo.Setup(r => r.GetByPlacaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? Result.Ok<VeiculoDto?>(Sample));
        return (repo, new GetVeiculoByPlacaHandler(repo.Object, NullLogger<GetVeiculoByPlacaHandler>.Instance));
    }

    [Fact]
    public async Task Sanitizes_Placa_Removes_Hyphen_And_Uppercases_Before_Repo_Call()
    {
        var (repo, handler) = Build();

        await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("abc-1d23"),
            CancellationToken.None);

        repo.Verify(r => r.GetByPlacaAsync("ABC1D23", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sanitizes_Placa_Removes_Spaces()
    {
        var (repo, handler) = Build();

        await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("abc 1 d 23"),
            CancellationToken.None);

        repo.Verify(r => r.GetByPlacaAsync("ABC1D23", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Accepts_Mercosul_Format()
    {
        var (_, handler) = Build();

        var result = await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("ABC1D23"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Accepts_Antigo_Format()
    {
        var (repo, handler) = Build();

        var result = await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("ABC1234"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.GetByPlacaAsync("ABC1234", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Returns_ValidationError_When_Length_Wrong_After_Sanitize()
    {
        var (repo, handler) = Build();

        var result = await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("ABC"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
        repo.Verify(
            r => r.GetByPlacaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Returns_ValidationError_When_Format_Unrecognised()
    {
        var (repo, handler) = Build();

        var result = await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("1234567"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
        Assert.Contains("Placa format not recognised", result.Errors[0].Message);
        repo.Verify(
            r => r.GetByPlacaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Returns_NotFound_When_Repo_Returns_Null()
    {
        var (_, handler) = Build(Result.Ok<VeiculoDto?>(null));

        var result = await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("ABC1D23"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task Propagates_InfraError_From_Repo()
    {
        var (_, handler) = Build(Result.Fail<VeiculoDto?>(new InfraError("LegacyDbUnreachable")));

        var result = await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("ABC1D23"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<InfraError>(result.Errors[0]);
    }

    [Fact]
    public async Task Returns_Found_Veiculo_On_Happy_Path()
    {
        var (_, handler) = Build();

        var result = await handler.HandleAsync(
            new GetVeiculoByPlacaQuery("ABC1D23"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Sample, result.Value);
    }
}
