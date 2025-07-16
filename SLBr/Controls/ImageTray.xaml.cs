using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for ImageTray.xaml
    /// </summary>
    public partial class ImageTray : Window
    {
        public ObservableCollection<DownloadImageEntry> DownloadImages { get; set; } = new();

        public class DownloadImageEntry
        {
            public string Path { get; set; }
            public string File { get; set; }
        }

        public string SelectedFilePath { get; private set; }

        public ImageTray()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var ImageFiles = Directory.EnumerateFiles(App.Instance.GlobalSave.Get("DownloadPath"), "*.*", SearchOption.TopDirectoryOnly)
                                      .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg") || f.EndsWith(".gif") || f.EndsWith(".bmp"))
                                      .OrderByDescending(File.GetCreationTime)
                                      .Take(20);
            DownloadImages.Clear();
            foreach (var Image in ImageFiles)
                DownloadImages.Add(new DownloadImageEntry { Path = Image, File = Path.GetFileName(Image) });

            if (Clipboard.ContainsImage())
            {
                var Encoder = new PngBitmapEncoder();
                Encoder.Frames.Add(BitmapFrame.Create(Clipboard.GetImage()));
                string TempPath = Path.Combine(Path.GetTempPath(), $"clipboard_{Guid.NewGuid()}.png");
                using (var _Stream = new FileStream(TempPath, FileMode.Create))
                    Encoder.Save(_Stream);
                ClipboardButton.Tag = TempPath;
                ClipboardImage.ImageSource = new BitmapImage(new Uri(TempPath));
                ClipboardColumn.Width = new GridLength(160);
                ClipboardFileName.Content = Path.GetFileName(TempPath);
            }
            else
            {
                ClipboardColumn.Width = new GridLength(0);
            }
        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedFilePath = (sender as Button)?.Tag?.ToString();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AllFilesButton_Click(object sender, RoutedEventArgs e)
        {
            //Use System.Drawing.Common
            /*ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();

            string FilterBuilder = "Image Files|";
            foreach (ImageCodecInfo info in encoders)
                FilterBuilder += (!FilterBuilder.EndsWith("|") ? ";" : "") + info.FilenameExtension.ToLower();
            FilterBuilder += (!FilterBuilder.EndsWith("|") ? ";" : "") + "*.svg";*/
            var Dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp",//FilterBuilder,
                Multiselect = false
            };
            if (Dialog.ShowDialog() == true)
            {
                SelectedFilePath = Dialog.FileName;
                DialogResult = true;
                Close();
            }
        }

        private void DownloadsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            DownloadsScrollViewer.ScrollToHorizontalOffset(DownloadsScrollViewer.HorizontalOffset - e.Delta / 3);
            e.Handled = true;
        }
    }
}
