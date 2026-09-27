using System.Windows;
using Vocalis.UI;

namespace Vocalis;

public partial class App : Application
{
    // "Local\" = univoco per la sessione dell'utente corrente.
    private const string SingleInstanceMutexName = @"Local\Vocalis-SingleInstance";

    private Mutex? singleInstanceMutex;
    private bool ownsMutex;
    private TrayIcon? trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Una sola istanza: due Vocalis aperti registrerebbero e incollerebbero due volte.
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

        trayIcon = new TrayIcon();
        trayIcon.ExitRequested += () => Shutdown();
        trayIcon.SetStatus("Pronto");
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
