using System.Drawing;
using System.Windows.Forms;

namespace Vocalis.UI;

/// <summary>
/// Icona nell'area di notifica (vicino all'orologio) con il menu dell'app.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    // Windows tronca il tooltip dell'icona oltre questa lunghezza.
    private const int MaxTooltipLength = 127;

    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem statusItem;

    public event Action? ExitRequested;

    public TrayIcon()
    {
        // Prima voce del menu: solo testo di stato, non cliccabile.
        statusItem = new ToolStripMenuItem { Enabled = false };

        var menu = new ContextMenuStrip();
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Esci", null, (_, _) => ExitRequested?.Invoke());

        notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true,
        };
    }

    public void SetStatus(string status)
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
