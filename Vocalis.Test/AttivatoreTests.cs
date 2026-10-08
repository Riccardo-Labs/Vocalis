using Vocalis.Attivazione;

namespace Vocalis.Test;

public class AttivatoreTests
{
    [Theory]
    [InlineData("Mouse4", 1)]
    [InlineData("Mouse5", 2)]
    [InlineData("  mouse5 ", 2)]
    [InlineData("MOUSE4", 1)]
    public void TentaConverti_ConNomeValido_RitornaIlPulsante(string nome, int atteso)
    {
        bool riuscito = Attivatore.TentaConverti(nome, out int pulsante);

        Assert.True(riuscito);
        Assert.Equal(atteso, pulsante);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pippo")]
    [InlineData("Mouse6")]
    [InlineData(null)]
    public void TentaConverti_ConNomeNonValido_RitornaFalse(string? nome)
    {
        bool riuscito = Attivatore.TentaConverti(nome, out _);

        Assert.False(riuscito);
    }
}
