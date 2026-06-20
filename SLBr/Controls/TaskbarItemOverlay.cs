/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace SLBr.Controls
{
    public class TaskbarItemOverlay
    {
        public static readonly DependencyProperty TemplateProperty = DependencyProperty.RegisterAttached("Template", typeof(DataTemplate), typeof(TaskbarItemOverlay), new PropertyMetadata(OnPropertyChanged));
        public static readonly DependencyProperty ProfileProperty = DependencyProperty.RegisterAttached("Profile", typeof(Profile), typeof(TaskbarItemOverlay), new PropertyMetadata(OnPropertyChanged));
        
        public static Profile GetProfile(DependencyObject _Object) =>
            (Profile)_Object.GetValue(ProfileProperty);

        public static void SetProfile(DependencyObject _Object, Profile Value) =>
            _Object.SetValue(ProfileProperty, Value);

        public static DataTemplate GetTemplate(DependencyObject _Object) =>
            (DataTemplate)_Object.GetValue(TemplateProperty);

        public static void SetTemplate(DependencyObject _Object, DataTemplate Value) =>
            _Object.SetValue(TemplateProperty, Value);

        private static void OnPropertyChanged(DependencyObject _Object, DependencyPropertyChangedEventArgs e)
        {
            TaskbarItemInfo ItemInfo = (TaskbarItemInfo)_Object;
            Profile? Profile = GetProfile(ItemInfo);
            DataTemplate Template = GetTemplate(ItemInfo);

            if (Template == null || Profile == null)
            {
                ItemInfo.Overlay = null;
                return;
            }

            RenderTargetBitmap Bitmap = new(16, 16, 96, 96, PixelFormats.Default);
            ContentControl Root = new()
            {
                ContentTemplate = Template,
                Content = Profile
            };
            Root.Arrange(new Rect(0, 0, 16, 16));
            Bitmap.Render(Root);

            ItemInfo.Overlay = Bitmap;
        }
    }
}
