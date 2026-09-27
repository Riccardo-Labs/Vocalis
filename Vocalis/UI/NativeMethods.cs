using System.Runtime.InteropServices;

namespace Vocalis.UI;

// Dichiarazioni P/Invoke verso user32.dll per dare alla finestra overlay gli stili estesi che
// WPF non espone: niente focus tastiera, niente comparsa in Alt+Tab/barra applicazioni.
internal static class NativeMethods
{
    internal const int GWL_EXSTYLE = -20;
    internal const int WS_EX_NOACTIVATE = 0x08000000; // la finestra non riceve mai il focus tastiera
    internal const int WS_EX_TOOLWINDOW = 0x00000080; // non compare in Alt+Tab né nella barra applicazioni

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
