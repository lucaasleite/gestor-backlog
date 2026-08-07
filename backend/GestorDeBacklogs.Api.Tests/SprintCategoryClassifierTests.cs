using GestorDeBacklogs.Api.Services;
using Xunit;

namespace GestorDeBacklogs.Api.Tests;

public class SprintCategoryClassifierTests
{
    private const string PlannedTag = "Planejado - Sprint";

    [Fact]
    public void ComTagPlanejada_RetornaPlanned()
    {
        var ok = SprintCategoryClassifier.IsPlanned(["Planejado - Sprint"], PlannedTag);

        Assert.True(ok);
    }

    [Fact]
    public void SemTags_RetornaOutOfSprint()
    {
        var ok = SprintCategoryClassifier.IsPlanned([], PlannedTag);

        Assert.False(ok);
    }

    [Fact]
    public void TagsNula_RetornaOutOfSprint()
    {
        var ok = SprintCategoryClassifier.IsPlanned(null, PlannedTag);

        Assert.False(ok);
    }

    [Fact]
    public void SoComTagForaDaSprint_RetornaOutOfSprint()
    {
        var ok = SprintCategoryClassifier.IsPlanned(["Fora da Sprint"], PlannedTag);

        Assert.False(ok);
    }

    [Theory]
    [InlineData("planejado - sprint")]
    [InlineData("PLANEJADO - SPRINT")]
    [InlineData("  Planejado - Sprint  ")]
    public void VariacoesDeCaixaOuEspaco_AindaRetornaPlanned(string tag)
    {
        var ok = SprintCategoryClassifier.IsPlanned([tag], PlannedTag);

        Assert.True(ok);
    }

    [Fact]
    public void TagPlanejadaEntreOutras_RetornaPlanned()
    {
        var ok = SprintCategoryClassifier.IsPlanned(["Prioridade Alta", "Planejado - Sprint", "#M"], PlannedTag);

        Assert.True(ok);
    }
}
