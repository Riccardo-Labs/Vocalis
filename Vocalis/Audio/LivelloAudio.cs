namespace Vocalis.Audio;

public static class LivelloAudio
{
    //OVERVIEW: Questa classe fornisce metodi per calcolare il livello audio corrente del microfono. Il livello audio è rappresentato come un valore compreso tra 0.0 (silenzio) e 1.0 (massimo volume).

    private const double MassimoValoreShort = 32768.0; // Valore massimo per un campione audio a 16 bit

    public static double CalcolaRMS(short[]? campioni)
    {
        if (campioni == null || campioni.Length == 0)
            return 0.0;

        double sommaQuadrati = 0.0;
        foreach (var campione in campioni)
        {
            sommaQuadrati += campione * campione;
        }

        double rms = Math.Sqrt(sommaQuadrati / campioni.Length); // formula RMS (Root Mean Square)
        return Math.Min(rms / MassimoValoreShort, 1.0); // normalizzazione 
    }

    /// <summary>
    /// Calcola l'RMS da un buffer grezzo di byte contenente campioni PCM a 16 bit
    /// (2 byte per campione, little-endian): è il formato che arriva dall'evento
    /// DataAvailable di WASAPI, prima di essere interpretato come short.
    /// </summary>
    public static double CalcolaRMS(byte[]? campioniGrezzi)
    {
        if (campioniGrezzi == null || campioniGrezzi.Length < 2)
            return 0.0;

        int numeroCampioni = campioniGrezzi.Length / 2; // un eventuale byte finale spaiato viene ignorato
        double sommaQuadrati = 0.0;
        for (int i = 0; i < numeroCampioni; i++)
        {
            short campione = BitConverter.ToInt16(campioniGrezzi, i * 2);
            sommaQuadrati += (double)campione * campione;
        }

        double rms = Math.Sqrt(sommaQuadrati / numeroCampioni); // formula RMS (Root Mean Square)
        return Math.Min(rms / MassimoValoreShort, 1.0); // normalizzazione 
    }
}