using System.IO;
using System.Windows;
using NAudio.Wave;
using Vocalis.Appunti;
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
    private readonly FinestraOverlay overlay = new();
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
        coordinatore.StatoCambiato += stato => Dispatcher.BeginInvoke(() =>
        {
            iconaNotifica?.ImpostaStato(stato switch
            {
                StatoDettatura.Registrazione => "Registrazione...",
                StatoDettatura.Trascrizione => "Trascrizione...",
                _ => "Pronto",
            });

            switch (stato)
            {
                case StatoDettatura.Registrazione:
                    overlay.ImpostaTesto("In ascolto...", IconaOverlay.Ascolto);
                    break;
                case StatoDettatura.Trascrizione:
                    overlay.ImpostaTesto("Sto trascrivendo...", IconaOverlay.Trascrizione);
                    break;
                // Inattivo non tocca l'overlay qui: TrascrizioneCompletata/Annullato/Errore
                // decidono loro il messaggio finale e per quanto resta visibile.
            }
        });
        coordinatore.AvanzamentoDownloadModello += percentuale => Dispatcher.BeginInvoke(() =>
            iconaNotifica?.ImpostaStato($"Scaricamento modello... {percentuale:P0}"));
        coordinatore.TrascrizioneCompletata += testo => Dispatcher.BeginInvoke(() =>
        {
            if (string.IsNullOrEmpty(testo))
            {
                overlay.MostraTemporaneo("Nessun testo riconosciuto", IconaOverlay.Nessuna, TimeSpan.FromSeconds(1.5));
            }
            else
            {
                // Il testo compare da solo dove stavi scrivendo: l'overlay non deve più dire nulla.
                overlay.Nascondi();
            }
        });
        coordinatore.Annullato += () => Dispatcher.BeginInvoke(() =>
            overlay.MostraTemporaneo("Annullato", IconaOverlay.Nessuna, TimeSpan.FromSeconds(1.5)));
        coordinatore.Errore += messaggio => Dispatcher.BeginInvoke(() =>
        {
            overlay.MostraTemporaneo("Errore", IconaOverlay.Nessuna, TimeSpan.FromSeconds(1.5));
            MessageBox.Show(
                $"Operazione non riuscita.\nVerifica microfono e connessione a internet.\n\nDettagli: {messaggio}",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        });
        hookMouse.Avvia();
        hookTastiera.Avvia();
#if DEBUG
        iconaNotifica.TestRegistrazioneRichiesto += async () => await EseguiTestRegistrazioneAsync();
        iconaNotifica.TestDownloadModelloRichiesto += async () => await EseguiTestDownloadModelloAsync();
        iconaNotifica.TestAppuntiRichiesto += EseguiTestAppunti;
        iconaNotifica.TestIncollaRichiesto += async () => await EseguiTestIncollaAsync();
        iconaNotifica.TestOverlayRichiesto += EseguiTestOverlay;
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

    // Solo build Debug: ciclo completo leggi/scrivi/ripristina, senza ancora simulare Ctrl+V.
    private void EseguiTestAppunti()
    {
        string? contenutoPrecedente = GestoreAppunti.LeggiTestoSeDisponibile();

        bool riuscito = GestoreAppunti.ImpostaTesto("Testo di prova di Vocalis — se lo vedi con Ctrl+V ma non in Win+V, funziona.");
        if (!riuscito)
        {
            MessageBox.Show("Scrittura negli appunti non riuscita.", "Vocalis", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        MessageBox.Show(
            "Scritto negli appunti. Prova Ctrl+V da qualche parte, poi premi OK per ripristinare quello che c'era prima.",
            "Vocalis",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        if (contenutoPrecedente != null)
        {
            GestoreAppunti.ImpostaTesto(contenutoPrecedente);
            MessageBox.Show("Contenuto precedente ripristinato. Prova Ctrl+V di nuovo per verificare.", "Vocalis", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Non c'era testo negli appunti prima (vuoti o contenuto non testuale): nessun ripristino.", "Vocalis", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // Solo build Debug: incolla automaticamente un testo di prova, poi ripristina gli appunti.
    // 3 secondi di margine per spostare il focus su un editor prima che parta.
    private async Task EseguiTestIncollaAsync()
    {
        iconaNotifica?.ImpostaStato("Vai sull'app di destinazione... incollo tra 3s");
        await Task.Delay(TimeSpan.FromSeconds(3));

        string? contenutoPrecedente = GestoreAppunti.LeggiTestoSeDisponibile();
        GestoreAppunti.ImpostaTesto("Testo incollato automaticamente da Vocalis.");

        await Task.Delay(TimeSpan.FromMilliseconds(100)); // tempo all'app di destinazione di registrare il focus
        SimulatoreIncolla.SimulaCtrlV();
        await Task.Delay(TimeSpan.FromMilliseconds(300)); // tempo di completare l'incolla prima di ripristinare

        if (contenutoPrecedente != null)
        {
            GestoreAppunti.ImpostaTesto(contenutoPrecedente);
        }

        iconaNotifica?.ImpostaStato("Pronto");
    }

    // Solo build Debug: mostra l'overlay per 2 secondi con un testo di prova, poi lo nasconde da solo.
    private void EseguiTestOverlay()
    {
        overlay.MostraTemporaneo("In ascolto...", IconaOverlay.Ascolto, TimeSpan.FromSeconds(2));
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
        overlay.Close();
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
