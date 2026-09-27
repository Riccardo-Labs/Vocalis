using System.IO;
using NAudio.Wave;
using Vocalis.Appunti;
using Vocalis.Attivazione;
using Vocalis.Audio;
using Vocalis.Trascrizione;

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
    private readonly ScaricatoreModello scaricatoreModello = new();
    private readonly string percorsoModello;
    private Trascrittore? trascrittore;

    // Mouse e tastiera girano su due thread di hook separati: senza questo, un clic ed Esc
    // capitati nello stesso istante potrebbero toccare macchina/registratore in contemporanea.
    // SemaphoreSlim è come un lock, ma può essere atteso con await (il normale "lock" no).
    private readonly SemaphoreSlim semaforo = new(1, 1);

    public event Action<StatoDettatura>? StatoCambiato;
    public event Action<double>? AvanzamentoDownloadModello;
    public event Action<string>? TrascrizioneCompletata;
    public event Action<string>? Errore;

    public CoordinatoreDettatura(HookMouse hookMouse, HookTastiera hookTastiera, RegistratoreAudio registratore, string percorsoModello)
    {
        this.hookMouse = hookMouse;
        this.hookTastiera = hookTastiera;
        this.registratore = registratore;
        this.percorsoModello = percorsoModello;

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
                await FermaETrascriviAsync();
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

    private async Task FermaETrascriviAsync()
    {
        byte[] audio = await registratore.FermaRegistrazioneAsync();

        if (trascrittore == null)
        {
            var avanzamento = new Progress<double>(percentuale => AvanzamentoDownloadModello?.Invoke(percentuale));
            await scaricatoreModello.AssicuraModelloAsync(percorsoModello, avanzamento);
            trascrittore = new Trascrittore(percorsoModello);
        }

        // L'audio passa da un file .wav temporaneo (Whisper.net legge da uno Stream) e viene
        // cancellato subito dopo: non serve tenerlo, non è il testo dettato a dover restare.
        string percorsoTemporaneo = Path.Combine(Path.GetTempPath(), $"vocalis-{Guid.NewGuid():N}.wav");
        try
        {
            using (var scrittore = new WaveFileWriter(percorsoTemporaneo, registratore.FormatoAudio))
            {
                scrittore.Write(audio, 0, audio.Length);
            }

            string testo;
            using (FileStream flusso = File.OpenRead(percorsoTemporaneo))
            {
                testo = await trascrittore.TrascriviAsync(flusso);
            }

            macchina.FineTrascrizione();
            StatoCambiato?.Invoke(macchina.Stato);
            TrascrizioneCompletata?.Invoke(testo);

            if (!string.IsNullOrEmpty(testo))
            {
                await IncollaAsync(testo);
            }
        }
        finally
        {
            File.Delete(percorsoTemporaneo);
        }
    }

    private static async Task IncollaAsync(string testo)
    {
        string? contenutoPrecedente = GestoreAppunti.LeggiTestoSeDisponibile();

        if (!GestoreAppunti.ImpostaTesto(testo))
            return; // niente da incollare se la scrittura negli appunti fallisce

        await Task.Delay(TimeSpan.FromMilliseconds(100)); // tempo all'app di destinazione di registrare il focus
        SimulatoreIncolla.SimulaCtrlV();
        await Task.Delay(TimeSpan.FromMilliseconds(300)); // tempo di completare l'incolla prima di ripristinare

        if (contenutoPrecedente != null)
        {
            GestoreAppunti.ImpostaTesto(contenutoPrecedente);
        }
    }

    public void Dispose()
    {
        hookMouse.PulsanteLateraleCliccato -= OnPulsanteLaterale;
        hookTastiera.EscPremuto -= OnEscPremuto;
        hookTastiera.DeveBloccareEsc = null;
        trascrittore?.Dispose();
        semaforo.Dispose();
    }
}
