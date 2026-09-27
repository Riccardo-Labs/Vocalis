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
    // Controlla periodicamente se il mouse è passato a un altro schermo mentre l'overlay è visibile:
    // solo così riesce a "seguirlo" in tempo reale, non solo al momento in cui compare la prima volta.
    private readonly DispatcherTimer timerSegueMouse = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly ScaleTransform[] scaleBarre;
    private string? nomeSchermoAttuale;

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

        timerSegueMouse.Tick += (_, _) =>
        {
            string schermoSottoIlMouse = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).DeviceName;
            if (schermoSottoIlMouse != nomeSchermoAttuale)
            {
                PosizionaBassoCentro();
            }
        };
    }

    public void ImpostaTesto(string testo, IconaOverlay icona = IconaOverlay.Nessuna)
    {
        timerNascondi.Stop(); // un nuovo stato annulla l'auto-nascondi di uno precedente ancora in corso
        TestoStato.Text = testo;
        MostraIcona(icona);
        Show();
        PosizionaBassoCentro(); // la dimensione cambia col testo/icona, riposiziona per restare ancorata in basso al centro
        timerSegueMouse.Start();
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
        timerSegueMouse.Stop();
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

    // Si posiziona sullo schermo dove si trova il mouse in questo momento, non sempre su quello
    // primario: utile con più monitor. WinForms lavora in pixel fisici, WPF in unità indipendenti
    // dalla risoluzione (DIP): li converto con lo scaling di sistema. Funziona bene se tutti i
    // monitor hanno lo stesso scaling; con scaling diversi tra schermi andrebbe rifinito per-monitor.
    private void PosizionaBassoCentro()
    {
        const double margine = 16;

        System.Drawing.Point puntoMouse = System.Windows.Forms.Cursor.Position;
        System.Windows.Forms.Screen schermo = System.Windows.Forms.Screen.FromPoint(puntoMouse);
        nomeSchermoAttuale = schermo.DeviceName;

        System.Drawing.Rectangle areaSchermo = schermo.WorkingArea;
        double scala = VisualTreeHelper.GetDpi(this).DpiScaleX;

        double areaLeft = areaSchermo.Left / scala;
        double areaTop = areaSchermo.Top / scala;
        double areaWidth = areaSchermo.Width / scala;
        double areaHeight = areaSchermo.Height / scala;

        Left = areaLeft + (areaWidth - ActualWidth) / 2;
        Top = areaTop + areaHeight - ActualHeight - margine;
    }
}
