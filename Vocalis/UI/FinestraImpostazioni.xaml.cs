using System.IO;
using System.Windows;
using Vocalis.Attivazione;
using Vocalis.Dati;
using Vocalis.Sistema;
using Vocalis.Trascrizione;

namespace Vocalis.UI;

// Finestra delle impostazioni. Per ora modifica solo l'attivatore, come testo libero.
public partial class FinestraImpostazioni : Window
{
    private readonly Impostazioni impostazioni;

    public FinestraImpostazioni(Impostazioni impostazioni, IReadOnlyList<string> modelliDisponibili)
    {
        InitializeComponent();
        this.impostazioni = impostazioni;
        CampoAttivatore.Text = impostazioni.Attivatore;
        CampoAvvioConWindows.IsChecked = impostazioni.AvvioConWindows;

        CampoModello.ItemsSource = modelliDisponibili;
        // Se il modello salvato non è tra quelli presenti, mostra quello che l'app userebbe davvero.
        CampoModello.SelectedItem = CatalogoModelli.Risolvi(impostazioni.Modello, modelliDisponibili);
    }

    // Emesso dopo un salvataggio riuscito: l'App lo usa per applicare subito le nuove impostazioni.
    public event Action? ImpostazioniSalvate;

    // Aggiorna lo stesso oggetto Impostazioni che ha l'App (non una copia) e lo scrive su disco.
    private void Salva_Click(object sender, RoutedEventArgs e)
    {
        if (!Attivatore.TentaConverti(CampoAttivatore.Text, out _))
        {
            MessageBox.Show(
                "Attivatore non valido. Per ora sono ammessi: Mouse4, Mouse5.",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        impostazioni.Attivatore = CampoAttivatore.Text.Trim();
        impostazioni.AvvioConWindows = CampoAvvioConWindows.IsChecked == true;
        // Elenco vuoto (nessun modello scaricato): niente selezione, il valore salvato resta com'è.
        if (CampoModello.SelectedItem is string modello)
        {
            impostazioni.Modello = modello;
        }

        try
        {
            AvvioAutomatico.Imposta(impostazioni.AvvioConWindows);
            GestoreImpostazioni.SalvaImpostazioni(impostazioni, GestoreImpostazioni.PercorsoPredefinito);
            ImpostazioniSalvate?.Invoke();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            MessageBox.Show(
                $"Impossibile salvare le impostazioni.\n\nDettagli: {ex.Message}",
                "Vocalis",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
