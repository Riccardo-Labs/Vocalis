using System.Runtime.InteropServices;

namespace Vocalis.Attivazione;

// Dichiarazioni P/Invoke verso user32.dll/kernel32.dll per gli hook globali di mouse e tastiera.
// Nessuna logica qui dentro: solo la "traduzione" delle firme C che servono a Windows.
internal static class NativeMethods
{
    // Tipo di hook: WH_MOUSE_LL = hook del mouse a basso livello (vede gli eventi di tutto il sistema).
    internal const int WH_MOUSE_LL = 14;

    // Messaggi Windows per i pulsanti laterali del mouse (mouse4/mouse5).
    internal const int WM_XBUTTONDOWN = 0x020B;
    internal const int WM_XBUTTONUP = 0x020C;

    // XBUTTON1 = mouse4 ("Indietro"), XBUTTON2 = mouse5 ("Avanti").
    internal const int XBUTTON1 = 0x0001;
    internal const int XBUTTON2 = 0x0002;

    // Flag dentro MSLLHOOKSTRUCT.flags: bit acceso se l'evento è stato generato da SendInput
    // (software), non da un vero movimento fisico del mouse.
    internal const int LLMHF_INJECTED = 0x0001;

    // Tipo di hook per la tastiera a basso livello.
    internal const int WH_KEYBOARD_LL = 13;

    internal const int WM_KEYDOWN = 0x0100;
    internal const int WM_KEYUP = 0x0101;

    // Codice virtuale di Ctrl (non distingue sinistro/destro: entrambi arrivano come VK_CONTROL).
    internal const int VK_CONTROL = 0x11;

    internal const int VK_ESCAPE = 0x1B;

    // Flag dentro KBDLLHOOKSTRUCT.flags: bit acceso se l'evento è stato generato da SendInput.
    internal const int LLKHF_INJECTED = 0x0010;

    // Firma della funzione C# che Windows chiama per ogni evento: nCode/wParam/lParam sono i tre
    // argomenti "grezzi" che manda Windows.
    internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentThreadId();

    // Manda un messaggio alla coda del thread indicato: usato per spedirgli WM_QUIT
    // e fermare in modo pulito il suo Application.Run(), da un altro thread.
    [DllImport("user32.dll")]
    internal static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    internal const uint WM_QUIT = 0x0012;

    // Dati che Windows passa per ogni evento del mouse a basso livello (layout fisso, deciso da
    // Windows: l'ordine e i tipi dei campi devono combaciare esattamente con la struct C originale).
    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData; // Per i pulsanti laterali: contiene XBUTTON1 o XBUTTON2 nei bit alti.
        public uint flags; // Bit LLMHF_INJECTED qui dentro.
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int x;
        public int y;
    }

    // Dati che Windows passa per ogni evento della tastiera a basso livello.
    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        public uint vkCode; // Codice virtuale del tasto, es. VK_ESCAPE.
        public uint scanCode;
        public uint flags; // Bit LLKHF_INJECTED qui dentro.
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
