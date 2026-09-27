using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Vocalis.Audio;

// Cattura audio dal microfono di default tramite WASAPI e lo accumula in memoria,
// già nel formato richiesto da Whisper: 16kHz, mono, 16-bit PCM.
public sealed class RegistratoreAudio : IDisposable
{
    private static readonly WaveFormat FormatoRichiesto = new(rate: 16000, bits: 16, channels: 1);

    private WasapiCapture? capture;
    private MemoryStream? bufferAudio;
    private TaskCompletionSource<byte[]>? registrazioneCompletata;

    // Formato con cui i dati vengono effettivamente catturati (dopo l'eventuale conversione di WASAPI).
    public WaveFormat? FormatoAudio => capture?.WaveFormat;

    // Livello RMS (0.0-1.0) dell'ultimo pacchetto audio ricevuto, per pilotare un indicatore di volume.
    public event Action<double>? LivelloCambiato;

    public void AvviaRegistrazione()
    {
        capture = new WasapiCapture
        {
            // In modalità condivisa, Windows converte da solo dal formato nativo del microfono
            // (spesso 48kHz stereo) a quello richiesto qui: evitiamo di scrivere un resampler a mano.
            WaveFormat = FormatoRichiesto,
        };
        bufferAudio = new MemoryStream();
        // Creato subito, non al momento dello stop: se il microfono si scollega da solo mentre
        // si registra, WASAPI ferma la cattura per conto suo, prima ancora che l'utente clicchi.
        // Se aspettassimo a crearlo dentro FermaRegistrazioneAsync(), quel primo stop andrebbe
        // perso e il secondo clic resterebbe in attesa di un evento che non arriva più.
        registrazioneCompletata = new TaskCompletionSource<byte[]>();

        capture.DataAvailable += (_, e) =>
        {
            // e.Buffer contiene i dati audio grezzi ricevuti in questo "pacchetto";
            // e.BytesRecorded dice quanti byte di e.Buffer sono validi (il buffer può essere più grande).
            bufferAudio.Write(e.Buffer, 0, e.BytesRecorded);

            var campioni = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, campioni, e.BytesRecorded);
            LivelloCambiato?.Invoke(LivelloAudio.CalcolaRMS(campioni));
        };

        capture.RecordingStopped += (_, e) =>
        {
            // WASAPI conferma lo stop qui, non subito dopo StopRecording(): per questo FermaRegistrazioneAsync è async.
            // e.Exception non è null se lo stop è dovuto a un errore (es. microfono scollegato):
            // in quel caso propaghiamo l'errore invece di far sembrare tutto andato bene.
            if (e.Exception != null)
            {
                registrazioneCompletata?.TrySetException(e.Exception);
            }
            else
            {
                registrazioneCompletata?.TrySetResult(bufferAudio?.ToArray() ?? []);
            }
        };

        capture.StartRecording();
    }

    // Ferma la registrazione e restituisce l'audio catturato, a 16kHz mono 16-bit PCM.
    public Task<byte[]> FermaRegistrazioneAsync()
    {
        capture?.StopRecording(); // se la cattura si era già fermata da sola (errore), non fa nulla
        return registrazioneCompletata?.Task ?? Task.FromResult<byte[]>([]);
    }

    public void Dispose()
    {
        capture?.Dispose();
        bufferAudio?.Dispose();
    }
}
