namespace Vocalis;

// Regole pure per passare da uno stato all'altro della dettatura: decide solo COSA succede a un
// evento (clic, Esc, fine trascrizione), non tocca mai mouse, tastiera o microfono.
public sealed class MacchinaStatiDettatura
{
    public StatoDettatura Stato { get; private set; } = StatoDettatura.Inattivo;

    // Toggle: true se il clic ha cambiato stato, false se ignorato (succede solo in Trascrizione).
    public bool GestisciClic()
    {
        switch (Stato)
        {
            case StatoDettatura.Inattivo:
                Stato = StatoDettatura.Registrazione;
                return true;

            case StatoDettatura.Registrazione:
                Stato = StatoDettatura.Trascrizione;
                return true;

            default:
                return false;
        }
    }

    // true se ha annullato una registrazione in corso, false se non c'era nulla da annullare.
    public bool Annulla()
    {
        if (Stato != StatoDettatura.Registrazione)
            return false;

        Stato = StatoDettatura.Inattivo;
        return true;
    }

    public void FineTrascrizione()
    {
        Stato = StatoDettatura.Inattivo;
    }
}
