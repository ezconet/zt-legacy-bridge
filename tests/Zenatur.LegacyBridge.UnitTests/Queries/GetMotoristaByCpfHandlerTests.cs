using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;
using Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

namespace Zenatur.LegacyBridge.UnitTests.Queries;

public class GetMotoristaByCpfHandlerTests
{
    private static readonly MotoristaDto Sample = new(
        Documento: "12345678901",
        TipoDocumento: "CPF",
        Nome: "JOAO DA SILVA",
        DataNascimento: new DateOnly(1980, 3, 15),
        Telefone: "11999998888",
        Endereco: null,
        AnttValidade: new DateOnly(2027, 1, 31),
        Veiculos: new List<Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca.VeiculoDto>());

    [Fact]
    public async Task Sanitizes_Cpf_Stripping_Mask_And_Letters_Before_Repo_Call()
    {
        var repo = new Mock<IMotoristaRepository>();
        repo.Setup(r => r.GetByCpfAsync("12345678901", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<MotoristaDto?>(Sample));
        var handler = new GetMotoristaByCpfHandler(repo.Object, NullLogger<GetMotoristaByCpfHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetMotoristaByCpfQuery("123.456.789-01"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.GetByCpfAsync("12345678901", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Returns_Found_Motorista_On_Happy_Path()
    {
        var repo = new Mock<IMotoristaRepository>();
        repo.Setup(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<MotoristaDto?>(Sample));
        var handler = new GetMotoristaByCpfHandler(repo.Object, NullLogger<GetMotoristaByCpfHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetMotoristaByCpfQuery("12345678901"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Sample, result.Value);
    }

    [Fact]
    public async Task Returns_NotFound_When_Repo_Returns_Null()
    {
        var repo = new Mock<IMotoristaRepository>();
        repo.Setup(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<MotoristaDto?>(null));
        var handler = new GetMotoristaByCpfHandler(repo.Object, NullLogger<GetMotoristaByCpfHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetMotoristaByCpfQuery("99999999999"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task Returns_ValidationError_When_Sanitized_Cpf_Has_Wrong_Length()
    {
        var repo = new Mock<IMotoristaRepository>();
        var handler = new GetMotoristaByCpfHandler(repo.Object, NullLogger<GetMotoristaByCpfHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetMotoristaByCpfQuery("abc-123"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
        repo.Verify(
            r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Accepts_Cnpj_14_Digits_With_Mask()
    {
        var repo = new Mock<IMotoristaRepository>();
        repo.Setup(r => r.GetByCpfAsync("12345678000199", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<MotoristaDto?>(Sample with { Documento = "12345678000199", TipoDocumento = "CNPJ" }));
        var handler = new GetMotoristaByCpfHandler(repo.Object, NullLogger<GetMotoristaByCpfHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetMotoristaByCpfQuery("12.345.678/0001-99"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.GetByCpfAsync("12345678000199", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Propagates_InfraError_From_Repo()
    {
        var repo = new Mock<IMotoristaRepository>();
        repo.Setup(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<MotoristaDto?>(new InfraError("LegacyDbUnreachable")));
        var handler = new GetMotoristaByCpfHandler(repo.Object, NullLogger<GetMotoristaByCpfHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetMotoristaByCpfQuery("12345678901"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<InfraError>(result.Errors[0]);
    }
}
