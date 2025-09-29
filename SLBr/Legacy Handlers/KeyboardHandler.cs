using CefSharp;
using System.Windows.Input;

namespace SLBr.Handlers
{
    public class KeyboardHandler : IKeyboardHandler
    {
        FastHashSet<HotKey> Keys = new FastHashSet<HotKey>();

        public void AddKey(Action Function, int _KeyCode, bool HasControl = false, bool HasShift = false, bool HasAlt = false)
        {
            Keys.Add(new HotKey(Function, _KeyCode, HasControl, HasShift, HasAlt));
        }

        public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
        {
            return true;
        }

        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            if (type == KeyType.RawKeyDown)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    bool HasControl = modifiers == CefEventFlags.ControlDown;
                    bool HasShift = modifiers == CefEventFlags.ShiftDown;
                    bool HasAlt = modifiers == CefEventFlags.AltDown;
                    int WPFKeyCode = (int)KeyInterop.KeyFromVirtualKey(windowsKeyCode);
                    foreach (HotKey Key in Keys)
                    {
                        if (Key.KeyCode == WPFKeyCode && Key.Control == HasControl && Key.Shift == HasShift && Key.Alt == HasAlt)
                        {
                            Key.Callback();
                            break;
                        }
                    }
                });
            }
            return false;
        }
    }
}
