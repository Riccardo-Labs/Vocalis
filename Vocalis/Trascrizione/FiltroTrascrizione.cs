using System.Text.RegularExpressions;

namespace Vocalis.Trascrizione;

// Whisper marca il silenzio con tag come [BLANK_AUDIO], o inventa frasi tipiche di sottotitoli
// su segmenti senza parlato. Questa classe ripulisce l'output prima che arrivi a chi lo usa.
public static class FiltroTrascrizione
{
    private static readonly Regex MarcatoriTraQuadre = new(@"\[[^\]]*\]", RegexOptions.Compiled);

    private static readonly string[] FrasiInventate =
    [
        "sottotitoli e revisione a cura di qtss",
        "sottotitoli a cura di",
        "grazie per la visione",
        "grazie per l'attenzione",
        "grazie della visione",
        "grazie a tutti per la visione",
        "iscrivetevi al canale",
    ];

    public static string Ripulisci(string? testo)
    {
        if (string.IsNullOrWhiteSpace(testo))
            return string.Empty;

        string risultato = MarcatoriTraQuadre.Replace(testo, string.Empty);

        foreach (string frase in FrasiInventate)
        {
            risultato = Regex.Replace(risultato, Regex.Escape(frase), string.Empty, RegexOptions.IgnoreCase);
        }

        return risultato.Trim();
    }
}
