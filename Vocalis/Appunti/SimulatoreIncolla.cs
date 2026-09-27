using System.Runtime.InteropServices;

namespace Vocalis.Appunti;

// Simula la pressione di Ctrl+V tramite SendInput: un'unica azione, non un ascolto continuo come
// gli hook, quindi niente thread dedicato. Limite noto: SendInput non raggiunge le finestre
// avviate come amministratore (UIPI) e non segnala l'errore — il testo resta negli appunti.
public static class SimulatoreIncolla
{
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static void SimulaCtrlV()
    {
        NativeMethods.INPUT[] eventi =
        [
            CreaEventoTasto(VK_CONTROL, giu: true),
            CreaEventoTasto(VK_V, giu: true),
            CreaEventoTasto(VK_V, giu: false),
            CreaEventoTasto(VK_CONTROL, giu: false),
        ];

        NativeMethods.SendInput((uint)eventi.Length, eventi, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT CreaEventoTasto(ushort tasto, bool giu)
    {
        return new NativeMethods.INPUT
        {
            type = INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = tasto,
                    dwFlags = giu ? 0u : KEYEVENTF_KEYUP,
                },
            },
        };
    }
}
