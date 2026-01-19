using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace SLBr.Controls
{
    public class TaskbarItemOverlay
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached("Text", typeof(object), typeof(TaskbarItemOverlay), new PropertyMetadata(OnPropertyChanged));
        public static readonly DependencyProperty TemplateProperty = DependencyProperty.RegisterAttached("Template", typeof(DataTemplate), typeof(TaskbarItemOverlay), new PropertyMetadata(OnPropertyChanged));
        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.RegisterAttached("Background", typeof(Brush), typeof(TaskbarItemOverlay), new PropertyMetadata(Brushes.Transparent, OnPropertyChanged));
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.RegisterAttached("Foreground", typeof(Brush), typeof(TaskbarItemOverlay), new PropertyMetadata(Brushes.White, OnPropertyChanged));

        public static Brush GetForeground(DependencyObject _DependencyObject) =>
            (Brush)_DependencyObject.GetValue(ForegroundProperty);

        public static void SetForeground(DependencyObject _DependencyObject, Brush Value) =>
            _DependencyObject.SetValue(ForegroundProperty, Value);

        public static Brush GetBackground(DependencyObject _DependencyObject) =>
            (Brush)_DependencyObject.GetValue(BackgroundProperty);

        public static void SetBackground(DependencyObject _DependencyObject, Brush Value) =>
            _DependencyObject.SetValue(BackgroundProperty, Value);

        public static object GetContent(DependencyObject _DependencyObject) =>
            _DependencyObject.GetValue(ContentProperty);

        public static void SetContent(DependencyObject _DependencyObject, object Value) =>
            _DependencyObject.SetValue(ContentProperty, Value);

        public static DataTemplate GetTemplate(DependencyObject _DependencyObject) =>
            (DataTemplate)_DependencyObject.GetValue(TemplateProperty);

        public static void SetTemplate(DependencyObject _DependencyObject, DataTemplate Template) =>
            _DependencyObject.SetValue(TemplateProperty, Template);

        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            TaskbarItemInfo ItemInfo = (TaskbarItemInfo)dependencyObject;
            object? Text = GetContent(ItemInfo);
            Brush? Background = GetBackground(ItemInfo);
            Brush? Foreground = GetForeground(ItemInfo);
            DataTemplate Template = GetTemplate(ItemInfo);

            if (Template == null || Text == null || Background == null || Foreground == null)
            {
                ItemInfo.Overlay = null;
                return;
            }

            RenderTargetBitmap Bitmap = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Default);
            ContentControl Root = new ContentControl
            {
                ContentTemplate = Template,
                Content = new TaskbarOverlayModel
                {
                    Text = Text,
                    Background = Background,
                    Foreground = Foreground
                }
            };
            Root.Arrange(new Rect(0, 0, 16, 16));
            Bitmap.Render(Root);

            ItemInfo.Overlay = Bitmap;
        }
    }

    public class TaskbarOverlayModel
    {
        public object Text { get; set; }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
    }
}
