using Zenatur.LegacyBridge.Application.Common;

namespace Zenatur.LegacyBridge.UnitTests.Common;

public class PiiMaskTests
{
    [Theory]
    [InlineData("12345678901", "123******01")]
    [InlineData("00000000000", "000******00")]
    public void Cpf_Masks_11_Digits(string input, string expected)
    {
        Assert.Equal(expected, PiiMask.Cpf(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12")]
    [InlineData("123456789012")]
    public void Cpf_NonStandard_Length_Returns_Stars(string? input)
    {
        Assert.Equal("***", PiiMask.Cpf(input));
    }

    [Theory]
    [InlineData("12345678000199", "123******99")]
    [InlineData("00000000000000", "000******00")]
    public void Cnpj_Masks_14_Digits(string input, string expected)
    {
        Assert.Equal(expected, PiiMask.Cnpj(input));
    }

    [Fact]
    public void Path_Masks_Motorista_Cpf_Segment()
    {
        Assert.Equal(
            "/v1/legacy/motoristas/123******01",
            PiiMask.Path("/v1/legacy/motoristas/12345678901"));
    }

    [Fact]
    public void Path_Without_Match_Unchanged()
    {
        Assert.Equal("/health/live", PiiMask.Path("/health/live"));
    }

    [Fact]
    public void Path_Empty_Returns_Empty()
    {
        Assert.Equal(string.Empty, PiiMask.Path(""));
        Assert.Equal(string.Empty, PiiMask.Path(null));
    }
}
