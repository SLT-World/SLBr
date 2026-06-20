/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace SLBr.Controls
{
    public class NonTopmostPopup : Popup
    {
        private bool? CurrentTopMost;
        private bool HasInitialized;
        private Window ParentWindow;

        public NonTopmostPopup()
        {
            Loaded += OnPopupLoaded;
            Unloaded += OnPopupUnloaded;
        }

        void OnPopupLoaded(object sender, RoutedEventArgs e)
        {
            if (HasInitialized)
                return;
            HasInitialized = true;
            Child?.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnChildPreviewMouseLeftButtonDown), true);
            ParentWindow = Window.GetWindow(this);
            if (ParentWindow == null)
                return;
            ParentWindow.Activated += OnParentWindowActivated;
            ParentWindow.Deactivated += OnParentWindowDeactivated;
        }

        private void OnPopupUnloaded(object sender, RoutedEventArgs e)
        {
            if (ParentWindow == null)
                return;
            ParentWindow.Activated -= OnParentWindowActivated;
            ParentWindow.Deactivated -= OnParentWindowDeactivated;
        }

        void OnParentWindowActivated(object sender, EventArgs e)
        {
            SetTopmostState(true);
        }

        void OnParentWindowDeactivated(object sender, EventArgs e)
        {
            SetTopmostState(false);
        }

        void OnChildPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetTopmostState(true);
            if (!ParentWindow.IsActive)
                ParentWindow.Activate();
        }

        protected override void OnOpened(EventArgs e)
        {
            SetTopmostState(false);
            base.OnOpened(e);
        }

        private void SetTopmostState(bool _IsTopMost)
        {
            if (CurrentTopMost.HasValue && CurrentTopMost == _IsTopMost)
                return;
            if (Child == null)
                return;
            if (PresentationSource.FromVisual(Child) is not HwndSource Source)
                return;
            nint Hwnd = Source.Handle;
            if (!DllUtils.GetWindowRect(Hwnd, out DllUtils.RECT _Rect))
                return;
            int _Width = (int)Width;
            int _Height = (int)Height;
            if (_IsTopMost)
                DllUtils.SetWindowPos(Hwnd, DllUtils.HWND_TOPMOST, _Rect.Left, _Rect.Top, _Width, _Height, DllUtils.TOPMOST_FLAGS);
            else
            {
                DllUtils.SetWindowPos(Hwnd, DllUtils.HWND_BOTTOM, _Rect.Left, _Rect.Top, _Width, _Height, DllUtils.TOPMOST_FLAGS);
                DllUtils.SetWindowPos(Hwnd, DllUtils.HWND_TOP, _Rect.Left, _Rect.Top, _Width, _Height, DllUtils.TOPMOST_FLAGS);
                DllUtils.SetWindowPos(Hwnd, DllUtils.HWND_NOTOPMOST, _Rect.Left, _Rect.Top, _Width, _Height, DllUtils.TOPMOST_FLAGS);
            }
            CurrentTopMost = _IsTopMost;
        }
    }
}
