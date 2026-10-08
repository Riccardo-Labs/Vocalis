using Microsoft.Win32;

namespace Vocalis.Sistema;

// Registra o rimuove Vocalis dall'avvio automatico di Windows (chiave di registro Run dell'utente corrente).
public static class AvvioAutomatico
{
    private const string ChiaveRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string NomeVoce = "Vocalis";

    public static bool EAttivo()
    {
        using RegistryKey? chiave = Registry.CurrentUser.OpenSubKey(ChiaveRun);
        return chiave?.GetValue(NomeVoce) != null;
    }

    // Il percorso è quello dell'exe che sta girando ora: attivalo dall'exe installato,
    // non da quello di sviluppo (bin\Debug), altrimenti Windows avvierà quello.
    public static void Imposta(bool attivo)
    {
        using RegistryKey chiave = Registry.CurrentUser.CreateSubKey(ChiaveRun);

        if (attivo)
        {
            // Virgolette attorno al percorso: servono se contiene spazi.
            chiave.SetValue(NomeVoce, $"\"{Environment.ProcessPath}\"");
        }
        else
        {
            chiave.DeleteValue(NomeVoce, throwOnMissingValue: false);
        }
    }
}
