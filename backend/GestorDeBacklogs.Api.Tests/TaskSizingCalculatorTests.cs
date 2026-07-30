using GestorDeBacklogs.Api.Services;
using Xunit;

namespace GestorDeBacklogs.Api.Tests;

public class TaskSizingCalculatorTests
{
    [Fact]
    public void PP_4Horas_GeraUmaUnicaTaskDe4hSemSufixo()
    {
        var ok = TaskSizingCalculator.TryCalculate("Corrigir bug X", 4, out var tasks);

        Assert.True(ok);
        Assert.Single(tasks);
        Assert.Equal("Corrigir bug X", tasks[0].Title);
        Assert.Equal(4, tasks[0].Hours);
    }

    [Fact]
    public void P_8Horas_GeraUmaUnicaTaskDe8hSemSufixo()
    {
        var ok = TaskSizingCalculator.TryCalculate("Implementar feature Y", 8, out var tasks);

        Assert.True(ok);
        Assert.Single(tasks);
        Assert.Equal("Implementar feature Y", tasks[0].Title);
        Assert.Equal(8, tasks[0].Hours);
    }

    [Fact]
    public void M_16Horas_GeraDuasTasksDe8hComSufixoParte()
    {
        var ok = TaskSizingCalculator.TryCalculate("US Cadastro", 16, out var tasks);

        Assert.True(ok);
        Assert.Equal(2, tasks.Count);
        Assert.Equal("US Cadastro - Parte 1", tasks[0].Title);
        Assert.Equal(8, tasks[0].Hours);
        Assert.Equal("US Cadastro - Parte 2", tasks[1].Title);
        Assert.Equal(8, tasks[1].Hours);
    }

    [Fact]
    public void G_24Horas_GeraTresTasksDe8hComSufixoParte()
    {
        var ok = TaskSizingCalculator.TryCalculate("US Relatorio", 24, out var tasks);

        Assert.True(ok);
        Assert.Equal(3, tasks.Count);
        Assert.All(tasks, t => Assert.Equal(8, t.Hours));
        Assert.Equal(["US Relatorio - Parte 1", "US Relatorio - Parte 2", "US Relatorio - Parte 3"], tasks.Select(t => t.Title));
    }

    [Fact]
    public void GG_40Horas_GeraCincoTasksDe8hComSufixoParte()
    {
        var ok = TaskSizingCalculator.TryCalculate("US Migracao", 40, out var tasks);

        Assert.True(ok);
        Assert.Equal(5, tasks.Count);
        Assert.All(tasks, t => Assert.Equal(8, t.Hours));
        Assert.Equal(
            ["US Migracao - Parte 1", "US Migracao - Parte 2", "US Migracao - Parte 3", "US Migracao - Parte 4", "US Migracao - Parte 5"],
            tasks.Select(t => t.Title));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(100)]
    public void EffortNaoReconhecido_RetornaFalseSemEstourarExcecao(int effort)
    {
        var ok = TaskSizingCalculator.TryCalculate("Item qualquer", effort, out var tasks);

        Assert.False(ok);
        Assert.Empty(tasks);
    }
}
