using Vocalis.Dati;

namespace Vocalis.Test;

public class GestoreImpostazioniTests : IDisposable
{
    private readonly string cartella = Path.Combine(Path.GetTempPath(), "vocalis-test-" + Guid.NewGuid());

    private string Percorso => Path.Combine(cartella, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(cartella))
        {
            Directory.Delete(cartella, recursive: true);
        }
    }

    [Fact]
    public void Carica_ConFileAssente_RitornaDefault()
    {
        Impostazioni risultato = GestoreImpostazioni.CaricaImpostazioni(Percorso);

        Assert.Equal("Mouse4", risultato.Attivatore);
        Assert.False(risultato.AvvioConWindows);
        Assert.Equal("large-v3-turbo", risultato.Modello);
        Assert.Empty(risultato.Vocabolario);
    }

    [Fact]
    public void SalvaPoiCarica_MantieneIValori()
    {
        var originali = new Impostazioni
        {
            Attivatore = "Mouse5",
            AvvioConWindows = true,
            Modello = "medium",
            Vocabolario = ["Vocalis", "Whisper"],
        };

        GestoreImpostazioni.SalvaImpostazioni(originali, Percorso);
        Impostazioni risultato = GestoreImpostazioni.CaricaImpostazioni(Percorso);

        Assert.Equal("Mouse5", risultato.Attivatore);
        Assert.True(risultato.AvvioConWindows);
        Assert.Equal("medium", risultato.Modello);
        Assert.Equal(["Vocalis", "Whisper"], risultato.Vocabolario);
    }

    [Fact]
    public void Carica_ConJsonCorrotto_RitornaDefault()
    {
        Directory.CreateDirectory(cartella);
        File.WriteAllText(Percorso, "{ questo non è json");

        Impostazioni risultato = GestoreImpostazioni.CaricaImpostazioni(Percorso);

        Assert.Equal("Mouse4", risultato.Attivatore);
    }

    [Fact]
    public void Carica_ConCampoMancante_UsaIlDefaultPerQuelCampo()
    {
        Directory.CreateDirectory(cartella);
        File.WriteAllText(Percorso, "{ \"Attivatore\": \"Mouse4\" }");

        Impostazioni risultato = GestoreImpostazioni.CaricaImpostazioni(Percorso);

        Assert.Equal("Mouse4", risultato.Attivatore);
        Assert.Equal("large-v3-turbo", risultato.Modello);
    }

    [Fact]
    public void Salva_NonLasciaIlFileTemporaneo()
    {
        GestoreImpostazioni.SalvaImpostazioni(new Impostazioni(), Percorso);

        Assert.True(File.Exists(Percorso));
        Assert.False(File.Exists(Percorso + ".tmp"));
    }
}
