using Xunit;

namespace Vocalis.Test;

public class MacchinaStatiDettaturaTests
{
    [Fact]
    public void StatoIniziale_EInattivo()
    {
        var macchina = new MacchinaStatiDettatura();

        Assert.Equal(StatoDettatura.Inattivo, macchina.Stato);
    }

    [Fact]
    public void GestisciClic_DaInattivo_PassaARegistrazione()
    {
        var macchina = new MacchinaStatiDettatura();

        bool cambiato = macchina.GestisciClic();

        Assert.True(cambiato);
        Assert.Equal(StatoDettatura.Registrazione, macchina.Stato);
    }

    [Fact]
    public void GestisciClic_DaRegistrazione_PassaATrascrizione()
    {
        var macchina = new MacchinaStatiDettatura();
        macchina.GestisciClic(); // Inattivo -> Registrazione

        bool cambiato = macchina.GestisciClic(); // Registrazione -> Trascrizione

        Assert.True(cambiato);
        Assert.Equal(StatoDettatura.Trascrizione, macchina.Stato);
    }

    [Fact]
    public void GestisciClic_DuranteTrascrizione_VieneIgnorato()
    {
        var macchina = new MacchinaStatiDettatura();
        macchina.GestisciClic(); // -> Registrazione
        macchina.GestisciClic(); // -> Trascrizione

        bool cambiato = macchina.GestisciClic(); // ignorato

        Assert.False(cambiato);
        Assert.Equal(StatoDettatura.Trascrizione, macchina.Stato);
    }

    [Fact]
    public void Annulla_DuranteRegistrazione_TornaInattivo()
    {
        var macchina = new MacchinaStatiDettatura();
        macchina.GestisciClic(); // -> Registrazione

        bool annullato = macchina.Annulla();

        Assert.True(annullato);
        Assert.Equal(StatoDettatura.Inattivo, macchina.Stato);
    }

    [Theory]
    [InlineData(StatoDettatura.Inattivo)]
    [InlineData(StatoDettatura.Trascrizione)]
    public void Annulla_FuoriDallaRegistrazione_NonFaNulla(StatoDettatura statoIniziale)
    {
        var macchina = new MacchinaStatiDettatura();
        if (statoIniziale == StatoDettatura.Trascrizione)
        {
            macchina.GestisciClic();
            macchina.GestisciClic();
        }

        bool annullato = macchina.Annulla();

        Assert.False(annullato);
        Assert.Equal(statoIniziale, macchina.Stato);
    }

    [Fact]
    public void FineTrascrizione_TornaInattivo()
    {
        var macchina = new MacchinaStatiDettatura();
        macchina.GestisciClic();
        macchina.GestisciClic(); // -> Trascrizione

        macchina.FineTrascrizione();

        Assert.Equal(StatoDettatura.Inattivo, macchina.Stato);
    }
}
