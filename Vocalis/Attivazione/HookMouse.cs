using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Vocalis.Attivazione;

// Hook globale del mouse a basso livello (WH_MOUSE_LL), su un thread dedicato con il proprio ciclo
// di messaggi: se questo thread si bloccasse, il mouse di tutto il sistema ne risentirebbe.
// Il clic di mouse4 (XBUTTON1) viene bloccato, sia down che up: non arriva più alle altre app
// (niente "Indietro" nel browser).
public sealed class HookMouse : IDisposable
{
    // Il delegate va tenuto in un campo: se il garbage collector lo raccoglie mentre Windows
    // lo sta ancora chiamando da codice nativo, l'app crasha in modo imprevedibile.
    private readonly NativeMethods.HookProc callback;

    private Thread? thread;
    private IntPtr hookHandle = IntPtr.Zero;
    private uint idThread;

    // Pulsante laterale di attivazione (1 = mouse4, 2 = mouse5). Scritto dal thread UI quando cambiano
    // le impostazioni, letto dal thread dell'hook: volatile garantisce che la lettura veda sempre il valore aggiornato.
    private volatile int pulsanteAttivazione = NativeMethods.XBUTTON1;

    public event Action<bool>? PulsanteLateraleCliccato;

    public void ImpostaPulsante(int pulsante)
    {
        pulsanteAttivazione = pulsante;
    }

    public HookMouse()
    {
        callback = CallbackHook;
    }

    public void Avvia()
    {
        thread = new Thread(CicloHook) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    private void CicloHook()
    {
        idThread = NativeMethods.GetCurrentThreadId(); // serve a Dispose() per fermare questo thread da fuori

        IntPtr modulo = NativeMethods.GetModuleHandle(null);
        hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, callback, modulo, dwThreadId: 0);

        if (hookHandle == IntPtr.Zero)
        {
            Console.WriteLine($"[HookMouse] SetWindowsHookEx fallita, errore Win32: {Marshal.GetLastWin32Error()}");
            return;
        }

        // Un hook a basso livello riceve gli eventi solo finché il thread che l'ha installato
        // continua a "pompare" messaggi. Application.Run() esce quando arriva WM_QUIT (vedi Dispose).
        Application.Run();
    }

    // Chiamata da Windows per OGNI evento del mouse su tutto il sistema: deve tornare subito.
    private IntPtr CallbackHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        int messaggio = (int)wParam;

        if (nCode >= 0 && (messaggio == NativeMethods.WM_XBUTTONDOWN || messaggio == NativeMethods.WM_XBUTTONUP))
        {
            var dati = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            bool iniettato = (dati.flags & NativeMethods.LLMHF_INJECTED) != 0;
            int pulsante = (int)(dati.mouseData >> 16); // XBUTTON1/2 occupano i 16 bit alti di mouseData

            if (!iniettato && pulsante == pulsanteAttivazione)
            {
                bool premuto = messaggio == NativeMethods.WM_XBUTTONDOWN;
                PulsanteLateraleCliccato?.Invoke(premuto);

                // Non chiamare CallNextHookEx: è così che si "consuma" l'evento e non arriva
                // più alle altre app. Il valore restituito non conta, basta che sia diverso da zero.
                return (IntPtr)1;
            }
        }

        // Qualunque altro evento (movimento, click sinistro/destro, mouse5...) prosegue normale.
        return NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(hookHandle);
            hookHandle = IntPtr.Zero;
        }

        if (idThread != 0)
        {
            // Sveglia Application.Run() sul thread dedicato: riceve WM_QUIT, il ciclo esce da solo.
            NativeMethods.PostThreadMessage(idThread, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            thread?.Join(TimeSpan.FromSeconds(2));
        }
    }
}
