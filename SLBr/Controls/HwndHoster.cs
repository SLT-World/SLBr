using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;

namespace SLBr.Controls
{
    public class HwndHoster : HwndHost
    {
        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(int dwExStyle,
                                              string lpszClassName,
                                              string lpszWindowName,
                                              int style,
                                              int x, int y,
                                              int width, int height,
                                              IntPtr hwndParent,
                                              IntPtr hMenu,
                                              IntPtr hInst,
                                              [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(HandleRef hWnd, int index, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int index, IntPtr dwNewLong);

        private const int WS_CHILD = 0x40000000,
            WS_VISIBLE = 0x10000000,
            LBS_NOTIFY = 0x00000001,
            HOST_ID = 0x00000002,
            LISTBOX_ID = 0x00000001,
            WS_VSCROLL = 0x00200000,
            WS_BORDER = 0x00800000,
            WS_CLIPCHILDREN = 0x02000000,
            WM_SIZE = 0x0005,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004;

        private const uint WS_EX_NOACTIVATE = 0x08000000;
        private const int GWL_EXSTYLE = -20;

        private IntPtr hwndHost;
        private static bool DesignMode;
        private int disposeSignaled;
        public bool IsDisposed
        {
            get
            {
                return Interlocked.CompareExchange(ref disposeSignaled, 1, 1) == 1;
            }
        }

        static HwndHoster()
        {
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

            //managedCefBrowserAdapter = ManagedCefBrowserAdapter.Create(this, false);
            SizeChanged += OnSizeChanged;
            //IsVisibleChanged += OnIsVisibleChanged;
            Loaded += HwndHoster_Loaded;
        }

        private void HwndHoster_Loaded(object sender, RoutedEventArgs e)
        {
            EnumChildWindows(hwndHost, new EnumWindowsProc(EnumChildProc), IntPtr.Zero);
            SetWindowPos(firstChildHwnd, IntPtr.Zero, 0, 0, (int)ActualWidth, (int)ActualHeight, SWP_NOZORDER | SWP_NOMOVE);
        }

        Window sourceWindow;

        private void PresentationSourceChangedHandler(object sender, SourceChangedEventArgs args)
        {
            if (args.NewSource != null)
            {
                var source = (HwndSource)args.NewSource;
                var matrix = source.CompositionTarget.TransformToDevice;
                var window = source.RootVisual as Window;
                if (window != null)
                {
                    sourceWindow = window;

                    if (CleanupElement == null)
                        CleanupElement = window;
                    else if (CleanupElement is Window parent && parent != window)
                        CleanupElement = window;
                }
            }
            else if (args.OldSource != null)
            {
                var window = args.OldSource.RootVisual as Window;
                if (window != null)
                    sourceWindow = null;
            }
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            if (hwndHost == IntPtr.Zero)
            {
                hwndHost = CreateWindowEx(0, "static", "",
                            WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WM_SIZE,
                            0, 0,
                            (int)ActualWidth, (int)ActualHeight,
                            hwndParent.Handle,
                            (IntPtr)HOST_ID,
                            IntPtr.Zero,
                            0);
                //if (GetWindowRect(firstChildHwnd, out rect))
                //{
                //SizeText.Text = $"First Child Window Handle: {firstChildHwnd}\nWidth: {(int)e.NewSize.Width}\nHeight: {(int)e.NewSize.Height}";
                //}
            }
            return new HandleRef(null, hwndHost);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_SETFOCUS = 0x0007;
            const int WM_MOUSEACTIVATE = 0x0021;
            const int WM_SIZE = 0x0005;
            switch (msg)
            {
                case WM_SETFOCUS:
                case WM_MOUSEACTIVATE:
                    {
                        return IntPtr.Zero;
                    }
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
        int WindowInitialized;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InternalDispose(bool disposing)
        {
            Interlocked.Exchange(ref WindowInitialized, 0);
            if (disposing)
            {
                SizeChanged -= OnSizeChanged;
                Loaded -= HwndHoster_Loaded;
                PresentationSource.RemoveSourceChangedHandler(this, PresentationSourceChangedHandler);
                sourceWindow = null;
                if (CleanupElement != null)
                    CleanupElement.Unloaded -= OnCleanupElementUnloaded;
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
            var owner = (HwndHoster)sender;
            var oldValue = (FrameworkElement)args.OldValue;
            var newValue = (FrameworkElement)args.NewValue;

            owner.OnCleanupElementChanged(oldValue, newValue);
        }

        protected virtual void OnCleanupElementChanged(FrameworkElement oldValue, FrameworkElement newValue)
        {
            if (oldValue != null)
                oldValue.Unloaded -= OnCleanupElementUnloaded;
            if (newValue != null)
                newValue.Unloaded += OnCleanupElementUnloaded;
        }

        private void OnCleanupElementUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        //RECT rect;
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            EnumChildWindows(hwndHost, new EnumWindowsProc(EnumChildProc), IntPtr.Zero);
            if (firstChildHwnd != IntPtr.Zero)
                SetWindowPos(firstChildHwnd, IntPtr.Zero, 0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height, SWP_NOZORDER | SWP_NOMOVE);
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var isVisible = (bool)args.NewValue;
            if (isVisible)
                SetWindowPos(hwndHost, IntPtr.Zero, 0, 0, (int)ActualWidth, (int)ActualHeight, SWP_NOZORDER | SWP_NOMOVE);
            else
                SetWindowPos(hwndHost, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOMOVE);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public const int WM_SETTEXT = 0x000C;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        private IntPtr firstChildHwnd = IntPtr.Zero;
        private bool EnumChildProc(IntPtr hWnd, IntPtr lParam)
        {
            if (firstChildHwnd == IntPtr.Zero)
                firstChildHwnd = hWnd;
            return true;
        }

        // Delegate for EnumChildWindows callback
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
