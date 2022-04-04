// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp;
using System;
using System.Collections.Generic;

namespace SLBr
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

    class KeyboardHandler : IKeyboardHandler
    {
        List<HotKey> Keys = new List<HotKey>();

        public void AddKey(Action Function, int _KeyCode, bool HasControl = false, bool HasShift = false, bool HasAlt = false)
        {
            Keys.Add(new HotKey(Function, _KeyCode, HasControl, HasShift, HasAlt));
        }

        public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
        {
            if (type == KeyType.RawKeyDown)
            {
                bool HasControl = modifiers == CefEventFlags.ControlDown;
                bool HasShift = modifiers == CefEventFlags.ShiftDown;
                bool HasAlt = modifiers == CefEventFlags.AltDown;

                //MessageBox.Show($"{windowsKeyCode},{HasControl},{HasShift},{HasAlt}");
                foreach (HotKey Key in Keys)
                {
                    //MessageBox.Show($"{Key.KeyCode},{Key.Control},{Key.Shift},{Key.Alt}");
                    if (Key.KeyCode == windowsKeyCode && Key.Control == HasControl && Key.Shift == HasShift && Key.Alt == HasAlt)
                    {
                        MainWindow.Instance.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            Key.Callback();
                        }));
                    }
                }

                //MessageBox.Show($"{windowsKeyCode},{HasControl},{HasShift},{HasAlt}");

            }

            return false;
        }

        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            return false;
        }
    }
}
