using System.Drawing;
using System.Windows.Forms;

namespace Vocalis.UI;

// Icona nell'area di notifica (vicino all'orologio) con il menu dell'app.
public sealed class IconaNotifica : IDisposable
{
    // Windows tronca il tooltip dell'icona oltre questa lunghezza.
    private const int MaxTooltipLength = 127;

    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem statusItem;

    public event Action? UscitaRichiesta;

#if DEBUG
    // Solo build Debug: voce di menu per provare la registrazione senza aspettare l'attivazione da mouse.
    public event Action? TestRegistrazioneRichiesto;

    // Solo build Debug: voce di menu per provare il download del modello Whisper.
    public event Action? TestDownloadModelloRichiesto;

    // Solo build Debug: voce di menu per provare la scrittura negli appunti.
    public event Action? TestAppuntiRichiesto;
#endif

    public IconaNotifica()
    {
        // Prima voce del menu: solo testo di stato, non cliccabile.
        statusItem = new ToolStripMenuItem { Enabled = false };

        var menu = new ContextMenuStrip();
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
#if DEBUG
        menu.Items.Add("Test: registra 5 secondi → salva .wav", null, (_, _) => TestRegistrazioneRichiesto?.Invoke());
        menu.Items.Add("Test: scarica modello Whisper", null, (_, _) => TestDownloadModelloRichiesto?.Invoke());
        menu.Items.Add("Test: scrivi negli appunti", null, (_, _) => TestAppuntiRichiesto?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
#endif
        menu.Items.Add("Esci", null, (_, _) => UscitaRichiesta?.Invoke());

        notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true,
        };
    }

    public void ImpostaStato(string status)
    {
        statusItem.Text = status;

        var tooltip = $"Vocalis — {status}";
        notifyIcon.Text = tooltip.Length > MaxTooltipLength ? tooltip[..MaxTooltipLength] : tooltip;
    }

    public void Dispose()
    {
        // Nasconde l'icona prima di distruggerla, altrimenti resta un'icona "fantasma".
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
    }
}
