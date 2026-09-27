using System.IO;
using System.Windows;
using NAudio.Wave;
using Vocalis.Attivazione;
using Vocalis.Audio;
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
#if DEBUG
    private readonly RegistratoreAudio registratoreTest = new();
#endif

    public App()
    {
        // I campi sopra sono già inizializzati qui: l'ordine di dichiarazione conta.
        coordinatore = new CoordinatoreDettatura(hookMouse, hookTastiera, registratoreDettatura);
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
            StatoDettatura.Trascrizione => "Salvataggio...",
            _ => "Pronto",
        }));
        coordinatore.Errore += messaggio => Dispatcher.BeginInvoke(() =>
            MessageBox.Show(
                $"Registrazione non riuscita.\nVerifica che un microfono sia collegato e impostato come predefinito.\n\nDettagli: {messaggio}",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Error));
        hookMouse.Avvia();
        hookTastiera.Avvia();
#if DEBUG
        iconaNotifica.TestRegistrazioneRichiesto += async () => await EseguiTestRegistrazioneAsync();
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

            MessageBox.Show($"Test salvato in:\n{percorso}", "Vocalis", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Es. nessun microfono di default disponibile: non deve far crashare l'app.
            MessageBox.Show(
                $"Registrazione non riuscita.\nVerifica che un microfono sia collegato e impostato come predefinito.\n\nDettagli: {ex.Message}",
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
