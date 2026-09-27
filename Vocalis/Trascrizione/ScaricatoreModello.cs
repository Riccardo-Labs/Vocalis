using System.IO;
using System.Net.Http;

namespace Vocalis.Trascrizione;

// Scarica il modello Whisper da Hugging Face se non è già presente sul disco. Scrive su un file
// .tmp e lo rinomina solo a download completato, così un'interruzione non lascia un modello rotto.
public sealed class ScaricatoreModello
{
    private const string UrlModello = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo.bin";

    private static readonly HttpClient HttpClient = new();

    public async Task<string> AssicuraModelloAsync(string percorsoModello, IProgress<double>? avanzamento = null)
    {
        if (File.Exists(percorsoModello))
            return percorsoModello;

        string? cartella = Path.GetDirectoryName(percorsoModello);
        if (!string.IsNullOrEmpty(cartella))
            Directory.CreateDirectory(cartella);

        string percorsoTemporaneo = percorsoModello + ".tmp";

        using HttpResponseMessage risposta = await HttpClient.GetAsync(UrlModello, HttpCompletionOption.ResponseHeadersRead);
        risposta.EnsureSuccessStatusCode();

        long? dimensioneTotale = risposta.Content.Headers.ContentLength;

        await using (Stream flussoRete = await risposta.Content.ReadAsStreamAsync())
        await using (FileStream flussoFile = File.Create(percorsoTemporaneo))
        {
            byte[] buffer = new byte[81920];
            long totaleScaricato = 0;
            int byteLetti;

            while ((byteLetti = await flussoRete.ReadAsync(buffer)) > 0)
            {
                await flussoFile.WriteAsync(buffer.AsMemory(0, byteLetti));
                totaleScaricato += byteLetti;

                if (dimensioneTotale.HasValue)
                {
                    avanzamento?.Report((double)totaleScaricato / dimensioneTotale.Value);
                }
            }
        }

        // Solo ora che il file è completo lo rinominiamo nel nome vero: un'interruzione prima di
        // questo punto lascia al massimo un .tmp incompleto, mai un .bin che sembra a posto ma non lo è.
        File.Move(percorsoTemporaneo, percorsoModello);
        return percorsoModello;
    }
}
