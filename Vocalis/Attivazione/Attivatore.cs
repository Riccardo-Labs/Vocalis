namespace Vocalis.Attivazione;

// Traduce il nome salvato in settings.json ("Mouse4", "Mouse5") nel numero del pulsante laterale
// che Windows usa negli eventi del mouse (1 = XBUTTON1, 2 = XBUTTON2). Logica pura, senza Windows.
public static class Attivatore
{
    public static bool TentaConverti(string? nome, out int pulsante)
    {
        switch (nome?.Trim().ToLowerInvariant())
        {
            case "mouse4":
                pulsante = 1;
                return true;
            case "mouse5":
                pulsante = 2;
                return true;
            default:
                pulsante = 0;
                return false;
        }
    }
}
