using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;

namespace SLBr.Controls
{
    public class HwndHoster : HwndHost
    {
        private IntPtr HwndHost;
        private static bool DesignMode;
        private int disposeSignaled;
        public bool IsDisposed
        {
            get
            {
                return Interlocked.CompareExchange(ref disposeSignaled, 1, 1) == 1;
            }
        }

        public HwndHoster()
        {
            DesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
            if (!DesignMode)
                NoInliningConstructor();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void NoInliningConstructor()
        {
            Focusable = true;
            FocusVisualStyle = null;
            PresentationSource.AddSourceChangedHandler(this, PresentationSourceChangedHandler);
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            SizeChanged += OnSizeChanged;
            Loaded += HwndHoster_Loaded;
        }

        private void HwndHoster_Loaded(object sender, RoutedEventArgs e)
        {
            DllUtils.EnumChildWindows(HwndHost, new DllUtils.EnumWindowsProc(EnumChildProc), IntPtr.Zero);
            DllUtils.SetWindowPos(FirstChildHwnd, IntPtr.Zero, 0, 0, (int)ActualWidth, (int)ActualHeight, DllUtils.SWP_NOZORDER | DllUtils.SWP_NOMOVE);
        }

        private void PresentationSourceChangedHandler(object sender, SourceChangedEventArgs args)
        {
            if (args.NewSource != null)
            {
                if (((HwndSource)args.NewSource).RootVisual is Window _Window)
                {
                    if (CleanupElement == null)
                        CleanupElement = _Window;
                    else if (CleanupElement is Window Parent && Parent != _Window)
                        CleanupElement = _Window;
                }
            }
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            if (HwndHost == IntPtr.Zero)
                HwndHost = DllUtils.CreateWindowEx(0, "static", "", DllUtils.WS_CHILD | DllUtils.WS_VISIBLE | DllUtils.WS_CLIPCHILDREN | DllUtils.WM_SIZE, 0, 0, (int)ActualWidth, (int)ActualHeight, hwndParent.Handle, DllUtils.HOST_ID, IntPtr.Zero, 0);
            return new HandleRef(null, HwndHost);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DllUtils.DestroyWindow(hwnd.Handle);
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case DllUtils.WM_SETFOCUS:
                case DllUtils.WM_MOUSEACTIVATE:
                    return IntPtr.Zero;
            }
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref disposeSignaled, 1, 0) != 0)
                return;
            if (!DesignMode)
                InternalDispose(disposing);
            base.Dispose(disposing);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InternalDispose(bool disposing)
        {
            if (disposing)
            {
                SizeChanged -= OnSizeChanged;
                Loaded -= HwndHoster_Loaded;
                PresentationSource.RemoveSourceChangedHandler(this, PresentationSourceChangedHandler);
                CleanupElement?.Unloaded -= OnCleanupElementUnloaded;
            }
        }

        public FrameworkElement CleanupElement
        {
            get { return (FrameworkElement)GetValue(CleanupElementProperty); }
            set { SetValue(CleanupElementProperty, value); }
        }

        public static readonly DependencyProperty CleanupElementProperty =
            DependencyProperty.Register(nameof(CleanupElement), typeof(FrameworkElement), typeof(HwndHoster), new PropertyMetadata(null, OnCleanupElementChanged));

        private static void OnCleanupElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((HwndHoster)sender).OnCleanupElementChanged((FrameworkElement)args.OldValue, (FrameworkElement)args.NewValue);
        }

        protected virtual void OnCleanupElementChanged(FrameworkElement oldValue, FrameworkElement newValue)
        {
            oldValue?.Unloaded -= OnCleanupElementUnloaded;
            newValue?.Unloaded += OnCleanupElementUnloaded;
        }

        private void OnCleanupElementUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DllUtils.EnumChildWindows(HwndHost, new DllUtils.EnumWindowsProc(EnumChildProc), IntPtr.Zero);
            if (FirstChildHwnd != IntPtr.Zero)
                DllUtils.SetWindowPos(FirstChildHwnd, IntPtr.Zero, 0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height, DllUtils.SWP_NOZORDER | DllUtils.SWP_NOMOVE);
        }

        private IntPtr FirstChildHwnd = IntPtr.Zero;
        private bool EnumChildProc(IntPtr hWnd, IntPtr lParam)
        {
            if (FirstChildHwnd == IntPtr.Zero)
                FirstChildHwnd = hWnd;
            return true;
        }
    }
}
