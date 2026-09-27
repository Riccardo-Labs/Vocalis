using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Vocalis.Attivazione;

// Hook globale della tastiera a basso livello (WH_KEYBOARD_LL), stesso pattern di HookMouse:
// thread dedicato con il proprio ciclo di messaggi. Traccia Esc. A differenza di mouse4 (sempre
// bloccato), Esc va bloccato solo durante la registrazione: DeveBloccareEsc lascia decidere a
// chi usa questa classe.
public sealed class HookTastiera : IDisposable
{
    private readonly NativeMethods.HookProc callback;

    private Thread? thread;
    private IntPtr hookHandle = IntPtr.Zero;
    private uint idThread;

    public event Action<bool>? EscPremuto;

    // Interpellata ad ogni evento Esc per decidere se bloccarlo. Nessuna funzione impostata
    // (o che ritorna false) = Esc passa normale alle altre app, come se l'hook non ci fosse.
    public Func<bool>? DeveBloccareEsc { get; set; }

    public HookTastiera()
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
        hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, callback, modulo, dwThreadId: 0);

        if (hookHandle == IntPtr.Zero)
        {
            Console.WriteLine($"[HookTastiera] SetWindowsHookEx fallita, errore Win32: {Marshal.GetLastWin32Error()}");
            return;
        }

        Application.Run();
    }

    private IntPtr CallbackHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        int messaggio = (int)wParam;

        if (nCode >= 0 && (messaggio == NativeMethods.WM_KEYDOWN || messaggio == NativeMethods.WM_KEYUP))
        {
            var dati = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            bool iniettato = (dati.flags & NativeMethods.LLKHF_INJECTED) != 0;

            if (!iniettato && dati.vkCode == NativeMethods.VK_ESCAPE)
            {
                bool premuto = messaggio == NativeMethods.WM_KEYDOWN;
                EscPremuto?.Invoke(premuto);

                if (DeveBloccareEsc?.Invoke() == true)
                {
                    return (IntPtr)1; // consuma l'evento: non arriva alle altre app
                }
            }
        }

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
            NativeMethods.PostThreadMessage(idThread, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            thread?.Join(TimeSpan.FromSeconds(2));
        }
    }
}
