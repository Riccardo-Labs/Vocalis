using System.Windows;
using Vocalis.UI;

namespace Vocalis;

public partial class App : Application
{
    // "Local\" = univoco per la sessione dell'utente corrente.
    private const string SingleInstanceMutexName = @"Local\Vocalis-SingleInstance";

    private Mutex? singleInstanceMutex;
    private bool ownsMutex;
    private IconaNotifica? trayIcon;

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
        trayIcon.ImpostaStato("Pronto");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        trayIcon?.Dispose();

        if (ownsMutex)
        {
            singleInstanceMutex?.ReleaseMutex();
        }
        singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }
}
