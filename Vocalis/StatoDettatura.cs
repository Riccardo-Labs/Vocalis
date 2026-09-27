namespace Vocalis;

// Stato della dettatura in un dato momento: Inattivo -> Registrazione -> Trascrizione -> Inattivo.
public enum StatoDettatura
{
    Inattivo,
    Registrazione,
    Trascrizione, // audio in fase di trascrizione (Whisper, Fase 4)
}
