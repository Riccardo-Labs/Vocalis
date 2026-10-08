using System.IO;

namespace Vocalis.Trascrizione;

// I modelli Whisper sono file "ggml-<nome>.bin" nella cartella models\. Qui si traduce tra nome
// (quello che compare nelle impostazioni, es. "large-v3-turbo") e nome del file.
public static class CatalogoModelli
{
    public const string NomePredefinito = "large-v3-turbo";

    private const string Prefisso = "ggml-";
    private const string Estensione = ".bin";

    public static string NomeFile(string nome) => $"{Prefisso}{nome}{Estensione}";

    // "ggml-medium.bin" → "medium". Per file che non sono modelli (es. ".bin.tmp") ritorna null.
    public static string? NomeDaFile(string nomeFile)
    {
        if (!nomeFile.StartsWith(Prefisso, StringComparison.OrdinalIgnoreCase) ||
            !nomeFile.EndsWith(Estensione, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string nome = nomeFile[Prefisso.Length..^Estensione.Length];
        return nome.Length > 0 ? nome : null;
    }

    // Se il modello scelto non è tra quelli presenti, ripiega sul predefinito: il download
    // automatico sa scaricare solo quello, quindi un nome qualsiasi non va mai usato alla cieca.
    public static string Risolvi(string? nomeScelto, IEnumerable<string> disponibili)
    {
        string? trovato = disponibili.FirstOrDefault(d => string.Equals(d, nomeScelto, StringComparison.OrdinalIgnoreCase));
        return trovato ?? NomePredefinito;
    }

    // Unica parte che legge il disco: l'elenco dei modelli già scaricati, in ordine alfabetico.
    public static IReadOnlyList<string> ElencaDisponibili(string cartellaModelli)
    {
        if (!Directory.Exists(cartellaModelli))
        {
            return [];
        }

        return Directory.GetFiles(cartellaModelli)
            .Select(percorso => NomeDaFile(Path.GetFileName(percorso)))
            .OfType<string>()
            .Order()
            .ToList();
    }
}
