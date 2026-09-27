using Vocalis.Trascrizione;
using Xunit;

namespace Vocalis.Test;

public class FiltroTrascrizioneTests
{
    [Fact]
    public void Ripulisci_ConTestoNormale_NonLoTocca()
    {
        string risultato = FiltroTrascrizione.Ripulisci("Ciao, questo è un test di dettatura.");

        Assert.Equal("Ciao, questo è un test di dettatura.", risultato);
    }

    [Fact]
    public void Ripulisci_ConSoloMarcatoreBlankAudio_RitornaVuoto()
    {
        string risultato = FiltroTrascrizione.Ripulisci("[BLANK_AUDIO]");

        Assert.Equal(string.Empty, risultato);
    }

    [Fact]
    public void Ripulisci_ConMarcatoreEmbeddedInTesto_TieneSoloIlTestoVero()
    {
        string risultato = FiltroTrascrizione.Ripulisci("Ciao [BLANK_AUDIO] come stai?");

        Assert.Equal("Ciao  come stai?", risultato);
    }

    [Theory]
    [InlineData("Sottotitoli a cura di QTSS")]
    [InlineData("Grazie per la visione")]
    [InlineData("grazie per l'attenzione")]
    public void Ripulisci_ConFraseInventataNota_LaRimuove(string testoInventato)
    {
        string risultato = FiltroTrascrizione.Ripulisci(testoInventato);

        Assert.DoesNotContain("grazie", risultato, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sottotitoli", risultato, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ripulisci_ConTestoNullOVuoto_RitornaVuotoSenzaCrash(string? testoInvalido)
    {
        string risultato = FiltroTrascrizione.Ripulisci(testoInvalido);

        Assert.Equal(string.Empty, risultato);
    }
}
