using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Vocalis.UI;

// Icone mostrabili accanto al testo dell'overlay: ognuna ha la propria animazione.
public enum IconaOverlay
{
    Nessuna,
    Ascolto,
    Trascrizione,
}

// Piccola finestra sempre in primo piano, in basso al centro, che mostra lo stato della dettatura
// senza mai rubare il focus all'app dove si sta scrivendo. Nessuna logica qui dentro: chi la usa
// le dice solo cosa mostrare (ImpostaTesto/MostraTemporaneo) o di sparire (Nascondi).
public partial class FinestraOverlay : Window
{
    // Barra, ritardo di partenza, durata di mezzo ciclo: sfalsati così le barre non si muovono
    // tutte insieme (sembrerebbe artificiale, un vero equalizzatore è "disordinato").
    private static readonly (string NomeBarra, double RitardoMs, double DurataMs)[] ConfigurazioneBarre =
    [
        ("Barra1", 0, 700),
        ("Barra2", 170, 610),
        ("Barra3", 90, 560),
        ("Barra4", 260, 660),
        ("Barra5", 130, 730),
    ];

    private readonly DispatcherTimer timerNascondi = new();
    private readonly ScaleTransform[] scaleBarre;

    public FinestraOverlay()
    {
        InitializeComponent();

        scaleBarre =
        [
            (ScaleTransform)Barra1.RenderTransform,
            (ScaleTransform)Barra2.RenderTransform,
            (ScaleTransform)Barra3.RenderTransform,
            (ScaleTransform)Barra4.RenderTransform,
            (ScaleTransform)Barra5.RenderTransform,
        ];

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

    public void ImpostaTesto(string testo, IconaOverlay icona = IconaOverlay.Nessuna)
    {
        timerNascondi.Stop(); // un nuovo stato annulla l'auto-nascondi di uno precedente ancora in corso
        TestoStato.Text = testo;
        MostraIcona(icona);
        Show();
        PosizionaBassoCentro(); // la dimensione cambia col testo/icona, riposiziona per restare ancorata in basso al centro
    }

    // Mostra il testo e sparisce da sola dopo "durata": per gli stati brevi (annullato, errore, ecc.).
    public void MostraTemporaneo(string testo, IconaOverlay icona, TimeSpan durata)
    {
        ImpostaTesto(testo, icona);
        timerNascondi.Interval = durata;
        timerNascondi.Start();
    }

    public void Nascondi()
    {
        timerNascondi.Stop();
        FermaTutteLeAnimazioni();
        Hide();
    }

    private void MostraIcona(IconaOverlay icona)
    {
        FermaTutteLeAnimazioni();

        IconaAscolto.Visibility = icona == IconaOverlay.Ascolto ? Visibility.Visible : Visibility.Collapsed;
        IconaTrascrizione.Visibility = icona == IconaOverlay.Trascrizione ? Visibility.Visible : Visibility.Collapsed;

        switch (icona)
        {
            case IconaOverlay.Ascolto:
                AvviaAnimazioneAscolto();
                break;
            case IconaOverlay.Trascrizione:
                AvviaAnimazioneTrascrizione();
                break;
        }
    }

    // BeginAnimation applica l'animazione direttamente sulla trasformazione, senza passare da uno
    // Storyboard: è la tecnica più diretta e affidabile per animare una singola proprietà da codice.
    private void AvviaAnimazioneAscolto()
    {
        for (int i = 0; i < scaleBarre.Length; i++)
        {
            var (_, ritardoMs, durataMs) = ConfigurazioneBarre[i];
            var animazione = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(durataMs),
                BeginTime = TimeSpan.FromMilliseconds(ritardoMs),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase(),
            };
            scaleBarre[i].BeginAnimation(ScaleTransform.ScaleYProperty, animazione);
        }
    }

    // Rotazione costante e continua: il classico spinner di caricamento.
    private void AvviaAnimazioneTrascrizione()
    {
        var animazione = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromMilliseconds(900),
            RepeatBehavior = RepeatBehavior.Forever,
        };
        RotazioneTrascrizione.BeginAnimation(RotateTransform.AngleProperty, animazione);
    }

    private void FermaTutteLeAnimazioni()
    {
        foreach (var scala in scaleBarre)
        {
            scala.BeginAnimation(ScaleTransform.ScaleYProperty, null); // null rimuove l'animazione e ripristina il valore base
        }

        RotazioneTrascrizione.BeginAnimation(RotateTransform.AngleProperty, null);
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
