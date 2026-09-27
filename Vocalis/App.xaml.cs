using System.IO;
using System.Windows;
using NAudio.Wave;
using Vocalis.Attivazione;
using Vocalis.Audio;
using Vocalis.Trascrizione;
using Vocalis.UI;

namespace Vocalis;

public partial class App : Application
{
    // "Local\" = univoco per la sessione dell'utente corrente.
    private const string SingleInstanceMutexName = @"Local\Vocalis-SingleInstance";

    private Mutex? singleInstanceMutex;
    private bool ownsMutex;
    private IconaNotifica? iconaNotifica;
    private readonly HookMouse hookMouse = new();
    private readonly HookTastiera hookTastiera = new();
    private readonly RegistratoreAudio registratoreDettatura = new();
    private readonly CoordinatoreDettatura coordinatore;

    private static readonly string PercorsoModello = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vocalis", "models", "ggml-large-v3-turbo.bin");
#if DEBUG
    private readonly RegistratoreAudio registratoreTest = new();
    private readonly ScaricatoreModello scaricatoreTest = new();
    private Trascrittore? trascrittoreTest;
#endif

    public App()
    {
        // I campi sopra sono già inizializzati qui: l'ordine di dichiarazione conta.
        coordinatore = new CoordinatoreDettatura(hookMouse, hookTastiera, registratoreDettatura, PercorsoModello);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e); // Chiamata al metodo base per gestire l'avvio dell'applicazione

        // Garantisce che sia in esecuzione una sola istanza dell'applicazione.
        singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out ownsMutex);
        if (!ownsMutex)
        {
            MessageBox.Show(
                "Vocalis è già in esecuzione.\nCerca l'icona nell'area di notifica, vicino all'orologio.",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        iconaNotifica = new IconaNotifica();
        iconaNotifica.UscitaRichiesta += Shutdown;

        // StatoCambiato/Errore arrivano da Task.Run (thread del pool, non UI): serve Dispatcher.
        coordinatore.StatoCambiato += stato => Dispatcher.BeginInvoke(() => iconaNotifica?.ImpostaStato(stato switch
        {
            StatoDettatura.Registrazione => "Registrazione...",
            StatoDettatura.Trascrizione => "Trascrizione...",
            _ => "Pronto",
        }));
        coordinatore.AvanzamentoDownloadModello += percentuale => Dispatcher.BeginInvoke(() =>
            iconaNotifica?.ImpostaStato($"Scaricamento modello... {percentuale:P0}"));
        coordinatore.TrascrizioneCompletata += testo => Dispatcher.BeginInvoke(() =>
            MessageBox.Show(
                string.IsNullOrEmpty(testo) ? "(nessun testo riconosciuto)" : testo,
                "Vocalis — Trascrizione",
                MessageBoxButton.OK,
                MessageBoxImage.Information));
        coordinatore.Errore += messaggio => Dispatcher.BeginInvoke(() =>
            MessageBox.Show(
                $"Operazione non riuscita.\nVerifica microfono e connessione a internet.\n\nDettagli: {messaggio}",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Error));
        hookMouse.Avvia();
        hookTastiera.Avvia();
#if DEBUG
        iconaNotifica.TestRegistrazioneRichiesto += async () => await EseguiTestRegistrazioneAsync();
        iconaNotifica.TestDownloadModelloRichiesto += async () => await EseguiTestDownloadModelloAsync();
        // DataAvailable arriva su un thread di NAudio, non su quello della UI:
        // Dispatcher.BeginInvoke passa l'aggiornamento al thread giusto (stessa regola degli hook, vedi CLAUDE.md).
        registratoreTest.LivelloCambiato += livello =>
            Dispatcher.BeginInvoke(() => iconaNotifica?.ImpostaStato($"Registrazione test... livello {livello:P0}"));
#endif
        iconaNotifica.ImpostaStato("Pronto");
    }

#if DEBUG
    // Solo build Debug: registra 5 secondi, salva un .wav in %TEMP% e mostra il livello nel tooltip.
    private async Task EseguiTestRegistrazioneAsync()
    {
        try
        {
            registratoreTest.AvviaRegistrazione();
            await Task.Delay(TimeSpan.FromSeconds(5));
            byte[] audio = await registratoreTest.FermaRegistrazioneAsync();

            string percorso = Path.Combine(Path.GetTempPath(), "vocalis-test.wav");
            using (var scrittore = new WaveFileWriter(percorso, registratoreTest.FormatoAudio))
            {
                scrittore.Write(audio, 0, audio.Length);
            }

            if (!File.Exists(PercorsoModello))
            {
                MessageBox.Show(
                    "Modello non ancora scaricato: prova prima \"Test: scarica modello Whisper\".",
                    "Vocalis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            trascrittoreTest ??= new Trascrittore(PercorsoModello);

            var cronometro = System.Diagnostics.Stopwatch.StartNew();
            string testo;
            using (FileStream flusso = File.OpenRead(percorso))
            {
                testo = await trascrittoreTest.TrascriviAsync(flusso);
            }
            cronometro.Stop();

            MessageBox.Show(
                $"Trascrizione ({cronometro.ElapsedMilliseconds} ms):\n\n{testo}",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Es. nessun microfono di default disponibile: non deve far crashare l'app.
            MessageBox.Show(
                $"Registrazione o trascrizione non riuscita.\nVerifica che un microfono sia collegato e impostato come predefinito.\n\nDettagli: {ex.Message}",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            iconaNotifica?.ImpostaStato("Pronto");
        }
    }

    // Solo build Debug: scarica il modello Whisper se manca, mostrando l'avanzamento nel tooltip.
    private async Task EseguiTestDownloadModelloAsync()
    {
        try
        {
            if (File.Exists(PercorsoModello))
            {
                MessageBox.Show($"Il modello è già presente:\n{PercorsoModello}", "Vocalis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Il download resta sulla UI thread: non serve Dispatcher, a differenza degli hook.
            var avanzamento = new Progress<double>(percentuale =>
                iconaNotifica?.ImpostaStato($"Download modello... {percentuale:P0}"));

            await scaricatoreTest.AssicuraModelloAsync(PercorsoModello, avanzamento);

            MessageBox.Show($"Modello scaricato in:\n{PercorsoModello}", "Vocalis", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Download del modello non riuscito.\nVerifica la connessione a internet.\n\nDettagli: {ex.Message}",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            iconaNotifica?.ImpostaStato("Pronto");
        }
    }
#endif

    protected override void OnExit(ExitEventArgs e)
    {
        iconaNotifica?.Dispose();
        coordinatore.Dispose();
        hookMouse.Dispose();
        hookTastiera.Dispose();
#if DEBUG
        registratoreTest.Dispose();
#endif

        if (ownsMutex)
        {
            singleInstanceMutex?.ReleaseMutex();
        }
        singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }
}
