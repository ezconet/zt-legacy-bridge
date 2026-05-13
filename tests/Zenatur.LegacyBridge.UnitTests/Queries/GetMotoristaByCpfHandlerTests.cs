using FluentResults;
using Moq;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;
using Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

namespace Zenatur.LegacyBridge.UnitTests.Queries;

public class GetMotoristaByCpfHandlerTests
{
    private static readonly MotoristaDto Sample = new(
        Cpf: "12345678901",
        Nome: "JOAO DA SILVA",
        DataNascimento: new DateOnly(1980, 3, 15),
        Telefone: "11999998888",
        Endereco: null,
        Rntrc: null);

    [Fact]
    public async Task Sanitizes_Cpf_Stripping_Mask_And_Letters_Before_Repo_Call()
    {
        var repo = new Mock<IMotoristaRepository>();
        repo.Setup(r => r.GetByCpfAsync("12345678901", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<MotoristaDto?>(Sample));
        var handler = new GetMotoristaByCpfHandler(repo.Object);

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
        var handler = new GetMotoristaByCpfHandler(repo.Object);

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
        var handler = new GetMotoristaByCpfHandler(repo.Object);

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
        var handler = new GetMotoristaByCpfHandler(repo.Object);

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
    public async Task Propagates_InfraError_From_Repo()
    {
        var repo = new Mock<IMotoristaRepository>();
        repo.Setup(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<MotoristaDto?>(new InfraError("LegacyDbUnreachable")));
        var handler = new GetMotoristaByCpfHandler(repo.Object);

        var result = await handler.HandleAsync(
            new GetMotoristaByCpfQuery("12345678901"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<InfraError>(result.Errors[0]);
    }
}
