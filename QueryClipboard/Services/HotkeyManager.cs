using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace QueryClipboard.Services
{
    public class HotkeyManager
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private IntPtr _windowHandle;
        private HwndSource? _source;

        public event EventHandler? HotkeyPressed;

        public void RegisterHotkey(IntPtr windowHandle, ModifierKeys modifiers, Key key)
        {
            // Unregister previous hotkey if any
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
            }

            _windowHandle = windowHandle;
            var helper = new WindowInteropHelper(System.Windows.Application.Current.MainWindow!);

            if (_source == null)
            {
                _source = HwndSource.FromHwnd(helper.Handle);
                _source?.AddHook(HwndHook);
            }

            uint fsModifiers = 0;
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                fsModifiers |= 0x0002; // MOD_CONTROL
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                fsModifiers |= 0x0001; // MOD_ALT
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                fsModifiers |= 0x0004; // MOD_SHIFT
            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                fsModifiers |= 0x0008; // MOD_WIN

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            if (!RegisterHotKey(windowHandle, HOTKEY_ID, fsModifiers, vk))
            {
                throw new InvalidOperationException("Nao foi possivel registrar o atalho. Ele pode estar em uso por outro aplicativo.");
            }
        }

        public void UnregisterHotkey()
        {
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
            }

            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Parses a modifier string like "Control+Alt" into ModifierKeys.
        /// </summary>
        public static ModifierKeys ParseModifiers(string modifierString)
        {
            var result = ModifierKeys.None;

            if (string.IsNullOrEmpty(modifierString))
                return result;

            var parts = modifierString.Split('+');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                switch (trimmed)
                {
                    case "Control":
                    case "Ctrl":
                        result |= ModifierKeys.Control;
                        break;
                    case "Alt":
                        result |= ModifierKeys.Alt;
                        break;
                    case "Shift":
                        result |= ModifierKeys.Shift;
                        break;
                    case "Windows":
                    case "Win":
                        result |= ModifierKeys.Windows;
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Parses a key string like "Q" into a Key enum.
        /// </summary>
        public static Key ParseKey(string keyString)
        {
            if (string.IsNullOrEmpty(keyString))
                return Key.Q; // default

            if (Enum.TryParse<Key>(keyString, true, out var key))
                return key;

            return Key.Q; // fallback
        }
    }
}
