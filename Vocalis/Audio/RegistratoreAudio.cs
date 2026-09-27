using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Vocalis.Audio;

/// <summary>
/// Cattura audio dal microfono di default tramite WASAPI e lo accumula in memoria,
/// già nel formato richiesto da Whisper: 16kHz, mono, 16-bit PCM.
/// </summary>
public sealed class RegistratoreAudio : IDisposable
{
    private static readonly WaveFormat FormatoRichiesto = new(rate: 16000, bits: 16, channels: 1);

    private WasapiCapture? capture;
    private MemoryStream? bufferAudio;
    private TaskCompletionSource? registrazioneFermata;

    /// <summary>Il formato con cui i dati vengono effettivamente catturati (dopo l'eventuale conversione di WASAPI).</summary>
    public WaveFormat? FormatoAudio => capture?.WaveFormat;

    /// <summary>Livello RMS (0.0-1.0) dell'ultimo pacchetto audio ricevuto, per pilotare un indicatore di volume.</summary>
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

        capture.DataAvailable += (_, e) =>
        {
            // e.Buffer contiene i dati audio grezzi ricevuti in questo "pacchetto";
            // e.BytesRecorded dice quanti byte di e.Buffer sono validi (il buffer può essere più grande).
            bufferAudio.Write(e.Buffer, 0, e.BytesRecorded);

            var campioni = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, campioni, e.BytesRecorded);
            LivelloCambiato?.Invoke(LivelloAudio.CalcolaRMS(campioni));
        };

        capture.RecordingStopped += (_, _) =>
        {
            // WASAPI conferma lo stop qui, non subito dopo StopRecording(): per questo FermaRegistrazioneAsync è async.
            registrazioneFermata?.TrySetResult();
        };

        capture.StartRecording();
    }

    /// <summary>Ferma la registrazione e restituisce l'audio catturato, a 16kHz mono 16-bit PCM.</summary>
    public async Task<byte[]> FermaRegistrazioneAsync()
    {
        registrazioneFermata = new TaskCompletionSource();
        capture?.StopRecording();
        await registrazioneFermata.Task;

        return bufferAudio?.ToArray() ?? [];
    }

    public void Dispose()
    {
        capture?.Dispose();
        bufferAudio?.Dispose();
    }
}
