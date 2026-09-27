using System.IO;
using System.Windows;
using NAudio.Wave;
using Vocalis.Audio;
using Vocalis.UI;

namespace Vocalis;

public partial class App : Application
{
    // "Local\" = univoco per la sessione dell'utente corrente.
    private const string SingleInstanceMutexName = @"Local\Vocalis-SingleInstance";

    private Mutex? singleInstanceMutex;
    private bool ownsMutex;
    private IconaNotifica? trayIcon;
#if DEBUG
    private readonly RegistratoreAudio registratoreTest = new();
#endif

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e); // Chiamata al metodo base per gestire l'avvio dell'applicazione

        // permette di garantire che solo una istanza dell'applicazione sia in esecuzione.
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
        
        trayIcon = new IconaNotifica();
        trayIcon.UscitaRichiesta += () => Shutdown();
#if DEBUG
        trayIcon.TestRegistrazioneRichiesto += async () => await EseguiTestRegistrazioneAsync();
        // DataAvailable arriva su un thread di NAudio, non su quello della UI:
        // Dispatcher.BeginInvoke passa l'aggiornamento al thread giusto (stessa regola degli hook, vedi CLAUDE.md).
        registratoreTest.LivelloCambiato += livello =>
            Dispatcher.BeginInvoke(() => trayIcon?.ImpostaStato($"Registrazione test... livello {livello:P0}"));
#endif
        trayIcon.ImpostaStato("Pronto");
    }

#if DEBUG
    /// <summary>Solo build Debug: registra 5 secondi, salva un .wav in %TEMP% e mostra il livello nel tooltip.</summary>
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
            trayIcon?.ImpostaStato("Pronto");
        }
    }
#endif

    protected override void OnExit(ExitEventArgs e)
    {
        trayIcon?.Dispose();
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
