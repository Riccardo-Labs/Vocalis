using System.Runtime.InteropServices;

namespace Vocalis.Appunti;

// Scrive testo negli appunti di Windows, marcato per non finire nella cronologia (Win+V) né
// nella sincronizzazione cloud: il testo dettato è privato, non deve lasciare traccia lì.
public static class GestoreAppunti
{
    private static readonly uint FormatoEscludiMonitoraggio =
        NativeMethods.RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
    private static readonly uint FormatoCronologia =
        NativeMethods.RegisterClipboardFormat("CanIncludeInClipboardHistory");
    private static readonly uint FormatoCloud =
        NativeMethods.RegisterClipboardFormat("CanUploadToCloudClipboard");

    // Restituisce il testo attualmente negli appunti, o null se non c'è testo (appunti vuoti,
    // o contenuto non testuale come un'immagine: in quel caso non c'è nulla da leggere qui,
    // e chi chiama semplicemente non proverà a ripristinarlo dopo).
    public static string? LeggiTestoSeDisponibile()
    {
        if (!NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_UNICODETEXT))
            return null;

        if (!NativeMethods.OpenClipboard(IntPtr.Zero))
            return null;

        try
        {
            IntPtr handle = NativeMethods.GetClipboardData(NativeMethods.CF_UNICODETEXT);
            if (handle == IntPtr.Zero)
                return null;

            IntPtr puntatore = NativeMethods.GlobalLock(handle);
            if (puntatore == IntPtr.Zero)
                return null;

            try
            {
                return Marshal.PtrToStringUni(puntatore);
            }
            finally
            {
                NativeMethods.GlobalUnlock(handle);
            }
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    public static bool ImpostaTesto(string testo)
    {
        if (!NativeMethods.OpenClipboard(IntPtr.Zero))
            return false;

        try
        {
            NativeMethods.EmptyClipboard();

            IntPtr handleTesto = AllocaTesto(testo);
            if (handleTesto == IntPtr.Zero || NativeMethods.SetClipboardData(NativeMethods.CF_UNICODETEXT, handleTesto) == IntPtr.Zero)
                return false;

            // Questi tre formati non hanno un contenuto significativo: la loro sola presenza dice
            // a Windows "non processare questo contenuto per cronologia/cloud". Un handle nullo
            // verrebbe interpretato come "dati forniti più tardi" (rendering ritardato) e farebbe
            // attendere chi legge: per questo allochiamo comunque un buffer vero, anche se vuoto.
            NativeMethods.SetClipboardData(FormatoEscludiMonitoraggio, AllocaDword(0));
            NativeMethods.SetClipboardData(FormatoCronologia, AllocaDword(0));
            NativeMethods.SetClipboardData(FormatoCloud, AllocaDword(0));

            return true;
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    private static IntPtr AllocaTesto(string testo)
    {
        int byteNecessari = (testo.Length + 1) * 2; // UTF-16: 2 byte a carattere, +1 per il terminatore finale
        IntPtr handle = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)byteNecessari);
        if (handle == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr puntatore = NativeMethods.GlobalLock(handle);
        if (puntatore == IntPtr.Zero)
            return IntPtr.Zero;

        Marshal.Copy(testo.ToCharArray(), 0, puntatore, testo.Length);
        Marshal.WriteInt16(puntatore, testo.Length * 2, 0); // terminatore
        NativeMethods.GlobalUnlock(handle);

        return handle;
    }

    private static IntPtr AllocaDword(int valore)
    {
        IntPtr handle = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)sizeof(int));
        if (handle == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr puntatore = NativeMethods.GlobalLock(handle);
        if (puntatore != IntPtr.Zero)
        {
            Marshal.WriteInt32(puntatore, valore);
            NativeMethods.GlobalUnlock(handle);
        }

        return handle;
    }
}
