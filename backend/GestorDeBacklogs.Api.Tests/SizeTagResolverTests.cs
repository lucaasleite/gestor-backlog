using GestorDeBacklogs.Api.Services;
using Xunit;

namespace GestorDeBacklogs.Api.Tests;

public class SizeTagResolverTests
{
    [Fact]
    public void EffortJaPreenchido_NaoMexeEmNada()
    {
        var (sizeLabel, effortHours) = SizeTagResolver.Resolve("M", 16, "#PP");

        Assert.Equal("M", sizeLabel);
        Assert.Equal(16, effortHours);
    }

    [Fact]
    public void SizeLabelPreenchidoMasEffortVazio_ResolvePeloProprioSizeLabel()
    {
        var (sizeLabel, effortHours) = SizeTagResolver.Resolve("PP", null, null);

        Assert.Equal("PP", sizeLabel);
        Assert.Equal(4, effortHours);
    }

    [Fact]
    public void SizeLabelEEffortVazios_ResolvePelaTag()
    {
        var (sizeLabel, effortHours) = SizeTagResolver.Resolve(null, null, "#PP");

        Assert.Equal("PP", sizeLabel);
        Assert.Equal(4, effortHours);
    }

    [Fact]
    public void SizeLabelNaoReconhecidoEEffortVazio_TentaPelaTag()
    {
        var (sizeLabel, effortHours) = SizeTagResolver.Resolve("Outro", null, "#G");

        Assert.Equal("Outro", sizeLabel);
        Assert.Equal(24, effortHours);
    }

    [Fact]
    public void NadaPreenchido_RetornaTudoNulo()
    {
        var (sizeLabel, effortHours) = SizeTagResolver.Resolve(null, null, null);

        Assert.Null(sizeLabel);
        Assert.Null(effortHours);
    }
}
