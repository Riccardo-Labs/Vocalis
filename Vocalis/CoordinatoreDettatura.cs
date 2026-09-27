using System.IO;
using NAudio.Wave;
using Vocalis.Attivazione;
using Vocalis.Audio;

namespace Vocalis;

// L'unico punto che sa COSA fare quando arriva un evento di attivazione: collega l'hook del mouse,
// la macchina a stati e il registratore audio. Non conosce la UI (tray, finestre) — espone solo
// eventi puliti (stato cambiato, errore) e lascia a chi lo usa decidere come mostrarli.
public sealed class CoordinatoreDettatura : IDisposable
{
    private readonly HookMouse hookMouse;
    private readonly HookTastiera hookTastiera;
    private readonly RegistratoreAudio registratore;
    private readonly MacchinaStatiDettatura macchina = new();

    // Mouse e tastiera girano su due thread di hook separati: senza questo, un clic ed Esc
    // capitati nello stesso istante potrebbero toccare macchina/registratore in contemporanea.
    // SemaphoreSlim è come un lock, ma può essere atteso con await (il normale "lock" no).
    private readonly SemaphoreSlim semaforo = new(1, 1);

    public event Action<StatoDettatura>? StatoCambiato;
    public event Action<string>? Errore;

    public CoordinatoreDettatura(HookMouse hookMouse, HookTastiera hookTastiera, RegistratoreAudio registratore)
    {
        this.hookMouse = hookMouse;
        this.hookTastiera = hookTastiera;
        this.registratore = registratore;

        hookMouse.PulsanteLateraleCliccato += OnPulsanteLaterale;

        hookTastiera.EscPremuto += OnEscPremuto;
        // Interpellata dal thread dell'hook tastiera per decidere se bloccare Esc: solo durante la registrazione.
        hookTastiera.DeveBloccareEsc = () => macchina.Stato == StatoDettatura.Registrazione;
    }

    // Chiamato dall'hook: deve tornare subito, quindi il lavoro vero (async, anche solo la
    // creazione di WasapiCapture) va spostato fuori dal thread dell'hook con Task.Run.
    private void OnPulsanteLaterale(bool premuto)
    {
        if (!premuto)
            return; // reagiamo solo alla pressione (down), non al rilascio

        _ = Task.Run(GestisciClicAsync);
    }

    private async Task GestisciClicAsync()
    {
        await semaforo.WaitAsync();
        try
        {
            bool cambiato = macchina.GestisciClic();
            if (!cambiato)
                return; // clic ignorato: eravamo in Trascrizione

            StatoCambiato?.Invoke(macchina.Stato);

            if (macchina.Stato == StatoDettatura.Registrazione)
            {
                registratore.AvviaRegistrazione();
            }
            else if (macchina.Stato == StatoDettatura.Trascrizione)
            {
                await FermaESalvaAsync();
            }
        }
        catch (Exception ex)
        {
            // Es. nessun microfono disponibile: torniamo a uno stato consistente invece di restare bloccati.
            macchina.Annulla();
            StatoCambiato?.Invoke(macchina.Stato);
            Errore?.Invoke(ex.Message);
        }
        finally
        {
            semaforo.Release();
        }
    }

    private void OnEscPremuto(bool premuto)
    {
        if (!premuto)
            return; // reagiamo solo alla pressione, non al rilascio

        _ = Task.Run(GestisciAnnullaAsync);
    }

    private async Task GestisciAnnullaAsync()
    {
        await semaforo.WaitAsync();
        try
        {
            bool annullato = macchina.Annulla();
            if (!annullato)
                return; // non stavamo registrando: Esc non fa nulla

            StatoCambiato?.Invoke(macchina.Stato); // torna subito Inattivo

            await registratore.FermaRegistrazioneAsync(); // ferma la cattura e scarta l'audio, non lo salviamo
        }
        catch (Exception ex)
        {
            Errore?.Invoke(ex.Message);
        }
        finally
        {
            semaforo.Release();
        }
    }

    private async Task FermaESalvaAsync()
    {
        byte[] audio = await registratore.FermaRegistrazioneAsync();

        // TODO Fase 4: qui andrà la trascrizione vera con Whisper. Per ora, come prova che
        // l'attivazione da mouse funziona davvero, salviamo un .wav.
        string percorso = Path.Combine(Path.GetTempPath(), "vocalis-attivazione.wav");
        using (var scrittore = new WaveFileWriter(percorso, registratore.FormatoAudio))
        {
            scrittore.Write(audio, 0, audio.Length);
        }

        macchina.FineTrascrizione();
        StatoCambiato?.Invoke(macchina.Stato);
    }

    public void Dispose()
    {
        hookMouse.PulsanteLateraleCliccato -= OnPulsanteLaterale;
        hookTastiera.EscPremuto -= OnEscPremuto;
        hookTastiera.DeveBloccareEsc = null;
        semaforo.Dispose();
    }
}
