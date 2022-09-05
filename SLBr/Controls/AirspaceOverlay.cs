using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace SLBr.Controls
{
    public class AirspaceOverlay : Decorator
    {
        private readonly Window _transparentInputWindow;
        private Window _parentWindow;

        public AirspaceOverlay()
        {
            _transparentInputWindow = CreateTransparentWindow();
            _transparentInputWindow.PreviewMouseDown += TransparentInputWindow_PreviewMouseDown;
        }

        public object OverlayChild
        {
            get { return _transparentInputWindow.Content; }
            set { _transparentInputWindow.Content = value; }
        }

        private static Window CreateTransparentWindow()
        {
            var transparentInputWindow = new Window();

            //Make the window itself transparent, with no style.
            transparentInputWindow.Background = Brushes.Transparent;
            transparentInputWindow.AllowsTransparency = true;
            transparentInputWindow.WindowStyle = WindowStyle.None;

            //Hide from taskbar until it becomes a child
            transparentInputWindow.ShowInTaskbar = false;

            //HACK: This window and it's child controls should never have focus, as window styling of an invisible window 
            //will confuse user.
            transparentInputWindow.Focusable = false;

            return transparentInputWindow;
        }

        void TransparentInputWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _parentWindow.Focus();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateOverlaySize();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_transparentInputWindow.Visibility != Visibility.Visible)
            {
                UpdateOverlaySize();
                _transparentInputWindow.Show();
                _parentWindow = GetParentWindow(this);
                _transparentInputWindow.Owner = _parentWindow;
                _parentWindow.LocationChanged += ParentWindow_LocationChanged;
                _parentWindow.SizeChanged += ParentWindow_SizeChanged;
            }
        }

        private static Window GetParentWindow(DependencyObject o)
        {
            var parent = VisualTreeHelper.GetParent(o);
            if (parent != null)
                return GetParentWindow(parent);
            var fe = o as FrameworkElement;
            if (fe is Window)
                return fe as Window;
            if (fe != null && fe.Parent != null)
                return GetParentWindow(fe.Parent);
            throw new ApplicationException("A window parent could not be found for " + o);
        }

        private void ParentWindow_LocationChanged(object sender, EventArgs e)
        {
            UpdateOverlaySize();
        }

        private void ParentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateOverlaySize();
        }

        private void UpdateOverlaySize()
        {
            var hostTopLeft = PointToScreen(new Point(0, 0));
            _transparentInputWindow.Left = hostTopLeft.X;
            _transparentInputWindow.Top = hostTopLeft.Y;
            _transparentInputWindow.Width = ActualWidth;
            _transparentInputWindow.Height = ActualHeight;
        }
    }
}
