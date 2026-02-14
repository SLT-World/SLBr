/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
        public IReadOnlyCollection<string> FileExtensions;
        public IReadOnlyCollection<string> FileDescriptions;
        public IReadOnlyCollection<string> FileFilters;
        public bool IncludeAllFiles = false;

        public ImageTray()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> ImageFiles = new();
            string[] ExtensionsArray = FileExtensions.ToArray();
            for (int i = 0; i < ExtensionsArray.Length; i++)
            {
                IEnumerable<string> Extensions = ExtensionsArray[i].Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim());
                ImageFiles.AddRange(Directory.EnumerateFiles(App.Instance.GlobalSave.Get("DownloadPath"), "*.*", SearchOption.TopDirectoryOnly).Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant())).OrderByDescending(File.GetCreationTime).Take(20));
            }
            DownloadImages.Clear();
            foreach (var Image in ImageFiles)
                DownloadImages.Add(new DownloadImageEntry { Path = Image, File = Path.GetFileName(Image) });

            if (Clipboard.ContainsImage())
            {
                PngBitmapEncoder Encoder = new PngBitmapEncoder();
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
                ClipboardColumn.Width = new GridLength(0);
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
            List<string> FilterParts = new();
            if (FileDescriptions != null && FileExtensions != null && FileDescriptions.Count == FileExtensions.Count)
            {
                string[] DescriptionArray = FileDescriptions.ToArray();
                string[] ExtensionArray = FileExtensions.ToArray();

                for (int i = 0; i < FileDescriptions.Count; i++)
                {
                    IEnumerable<string> Extensions = ExtensionArray[i].Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Select(e => e.TrimStart('.').Trim());
                    string ExtensionPattern = string.Join(";", Extensions.Select(e => "*." + e));

                    FilterParts.Add($"{DescriptionArray[i]} ({ExtensionPattern})|{ExtensionPattern}");
                }
            }
            else if (FileExtensions?.Any() == true)
            {
                IEnumerable<string> Extensions = FileExtensions.Select(e => "*." + e.TrimStart('.'));
                string ExtensionPattern = string.Join(";", Extensions);
                FilterParts.Add($"Files ({ExtensionPattern})|{ExtensionPattern}");
            }
            else if (FileFilters?.Any() == true)
            {
                foreach (string _File in FileFilters)
                    FilterParts.Add($"{_File}|{_File}");
            }

            if (IncludeAllFiles)
                FilterParts.Add("All Files (*.*)|*.*");


            Clipboard.SetText(string.Join("|", FilterParts));
            OpenFileDialog Dialog = new OpenFileDialog
            {
                Filter = string.Join("|", FilterParts),
                Multiselect = false
            };
            if (Dialog.ShowDialog() == true)
            {
                SelectedFilePath = Dialog.FileName;
                DialogResult = true;
                Close();
            }
        }
    }
}
