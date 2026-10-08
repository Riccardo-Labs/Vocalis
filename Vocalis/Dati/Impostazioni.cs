namespace Vocalis.Dati;

// Dati delle impostazioni utente, così come finiscono in settings.json.
// Ogni proprietà ha un valore di default: se il file manca o non contiene un campo, vale quello.
public sealed class Impostazioni
{
    // Pulsante o tasto che avvia/ferma la registrazione.
    public string Attivatore { get; set; } = "Mouse4";

    public bool AvvioConWindows { get; set; }

    // Nome del modello Whisper da usare (large-v3-turbo o medium).
    public string Modello { get; set; } = "large-v3-turbo";

    // Termini che Whisper tende a sbagliare, scritti a mano dall'utente.
    public List<string> Vocabolario { get; set; } = [];
}
