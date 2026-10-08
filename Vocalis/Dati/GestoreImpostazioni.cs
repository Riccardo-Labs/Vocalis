using System.IO;
using System.Text.Json;

namespace Vocalis.Dati;

// Legge e scrive settings.json. Il percorso è un parametro così i test possono usare una cartella temporanea.
public static class GestoreImpostazioni
{
    public static readonly string PercorsoPredefinito = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vocalis", "settings.json");

    // WriteIndented: il file resta leggibile e correggibile a mano.
    private static readonly JsonSerializerOptions OpzioniJson = new() { WriteIndented = true };

    // File assente, vuoto o corrotto: ritorna le impostazioni di default invece di far crashare l'app.
    public static Impostazioni CaricaImpostazioni(string percorso)
    {
        try
        {
            if (!File.Exists(percorso))
            {
                return new Impostazioni();
            }

            string json = File.ReadAllText(percorso);
            return JsonSerializer.Deserialize<Impostazioni>(json) ?? new Impostazioni();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new Impostazioni();
        }
    }

    // Scrive prima su un .tmp e poi lo rinomina: se l'app si ferma a metà scrittura,
    // settings.json resta quello di prima invece di diventare un file tagliato.
    public static void SalvaImpostazioni(Impostazioni impostazioni, string percorso)
    {
        string? cartella = Path.GetDirectoryName(percorso);
        if (!string.IsNullOrEmpty(cartella))
        {
            Directory.CreateDirectory(cartella);
        }

        string temporaneo = percorso + ".tmp";
        File.WriteAllText(temporaneo, JsonSerializer.Serialize(impostazioni, OpzioniJson));
        File.Move(temporaneo, percorso, overwrite: true);
    }
}
