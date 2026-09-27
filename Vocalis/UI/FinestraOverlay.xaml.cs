using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Vocalis.UI;

// Piccola finestra sempre in primo piano, in basso al centro, che mostra lo stato della dettatura
// senza mai rubare il focus all'app dove si sta scrivendo. Nessuna logica qui dentro: chi la usa
// le dice solo cosa mostrare (ImpostaTesto/MostraTemporaneo) o di sparire (Nascondi).
public partial class FinestraOverlay : Window
{
    private readonly DispatcherTimer timerNascondi = new();

    public FinestraOverlay()
    {
        InitializeComponent();

        // Gli stili estesi vanno impostati dopo che la finestra ha un handle nativo (HWND),
        // quindi qui e non nel costruttore: SourceInitialized è il primo momento in cui esiste.
        SourceInitialized += (_, _) => ImpostaStiliFinestra();
        Loaded += (_, _) => PosizionaBassoCentro();

        timerNascondi.Tick += (_, _) =>
        {
            timerNascondi.Stop();
            Hide();
        };
    }

    public void ImpostaTesto(string testo)
    {
        timerNascondi.Stop(); // un nuovo stato annulla l'auto-nascondi di uno precedente ancora in corso
        TestoStato.Text = testo;
        Show();
        PosizionaBassoCentro(); // la dimensione cambia col testo, riposiziona per restare ancorata in basso al centro
    }

    // Mostra il testo e sparisce da sola dopo "durata": per gli stati brevi (fatto, annullato, errore).
    public void MostraTemporaneo(string testo, TimeSpan durata)
    {
        ImpostaTesto(testo);
        timerNascondi.Interval = durata;
        timerNascondi.Start();
    }

    public void Nascondi()
    {
        timerNascondi.Stop();
        Hide();
    }

    private void ImpostaStiliFinestra()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        int stileAttuale = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(handle, NativeMethods.GWL_EXSTYLE, stileAttuale | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);
    }

    private void PosizionaBassoCentro()
    {
        const double margine = 16;
        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - ActualWidth) / 2;
        Top = SystemParameters.WorkArea.Bottom - ActualHeight - margine;
    }
}
