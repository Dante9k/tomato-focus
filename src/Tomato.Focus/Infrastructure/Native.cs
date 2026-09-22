using System;
using System.Runtime.InteropServices;

namespace Tomato
{
    internal static class Native
    {
        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int index, int value);
        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
        [DllImport("user32.dll")]
        internal static extern bool DestroyIcon(IntPtr handle);
    }
}
