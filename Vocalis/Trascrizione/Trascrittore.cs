using System.IO;
using System.Text;
using Whisper.net;

namespace Vocalis.Trascrizione;

// Carica un modello Whisper e trascrive audio in italiano. Whisper.net sceglie da solo il
// runtime migliore disponibile tra quelli installati (Vulkan su GPU, altrimenti CPU).
public sealed class Trascrittore : IDisposable
{
    private readonly WhisperFactory fabbrica;
    private readonly WhisperProcessor processore;

    public Trascrittore(string percorsoModello)
    {
        fabbrica = WhisperFactory.FromPath(percorsoModello);
        processore = fabbrica.CreateBuilder()
            .WithLanguage("it")
            .Build();
    }

    public async Task<string> TrascriviAsync(Stream flussoWav)
    {
        var testo = new StringBuilder();

        await foreach (SegmentData segmento in processore.ProcessAsync(flussoWav))
        {
            testo.Append(segmento.Text);
        }

        return FiltroTrascrizione.Ripulisci(testo.ToString());
    }

    public void Dispose()
    {
        processore.Dispose();
        fabbrica.Dispose();
    }
}
