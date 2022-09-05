using CefSharp;
using System;
using System.Windows.Input;

namespace SLBr.Handlers
{
    class HotKey
    {
        public HotKey(Action _Callback, int _KeyCode, bool HasControl, bool HasShift, bool HasAlt)
        {
            Callback = _Callback;
            KeyCode = _KeyCode;
            Control = HasControl;
            Shift = HasShift;
            Alt = HasAlt;
        }

        public int KeyCode;
        public bool Control;
        public bool Shift;
        public bool Alt;

        public Action Callback;//Function
    }

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
                MainWindow.Instance.Dispatcher.BeginInvoke(new Action(delegate
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
                }));
            }
            return false;
        }
    }
}
