/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using Microsoft.Win32;
using SLBr.WebView;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
            List<string> ImageFiles = [];
            string[] ExtensionsArray = FileExtensions.ToArray();
            for (int i = 0; i < ExtensionsArray.Length; i++)
            {
                IEnumerable<string> Extensions = ExtensionsArray[i].Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim());
                ImageFiles.AddRange(Directory.EnumerateFiles(WebViewManager.RuntimeSettings.DownloadFolderPath, "*.*", SearchOption.TopDirectoryOnly).Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant())).OrderByDescending(File.GetCreationTime).Take(20));
            }
            DownloadImages.Clear();
            foreach (var Image in ImageFiles)
                DownloadImages.Add(new DownloadImageEntry { Path = Image, File = Path.GetFileName(Image) });

            DownloadsColumn.Width = new GridLength(DownloadImages.Any() ? 500 : 0);

            if (Clipboard.ContainsImage())
            {
                BitmapSource RawClipboardImage = Clipboard.GetImage();
                if (RawClipboardImage == null)
                    return;
                RawClipboardImage.SafeFreeze();
                ClipboardImage.ImageSource = RawClipboardImage;
                ClipboardColumn.Width = new GridLength(160);

                string TempPath = Path.Combine(Path.GetTempPath(), $"clipboard_{Guid.NewGuid()}.png");
                ClipboardButton.Tag = TempPath;
                ClipboardFileName.Content = Path.GetFileName(TempPath);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        PngBitmapEncoder Encoder = new();
                        Encoder.Frames.Add(BitmapFrame.Create(RawClipboardImage));
                        using FileStream _Stream = new(TempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                        Encoder.Save(_Stream);
                        await _Stream.FlushAsync();
                    }
                    catch { }
                });
            }
            else
            {
                ClipboardColumn.Width = new GridLength(0);
                if (!DownloadImages.Any())
                {
                    AllFilesButton_Click(null, null);
                    DialogResult = true;
                    Close();
                }
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
            List<string> FilterParts = [];
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
            else if (FileExtensions?.Count != 0)
            {
                IEnumerable<string> Extensions = FileExtensions.Select(e => "*." + e.TrimStart('.'));
                string ExtensionPattern = string.Join(";", Extensions);
                FilterParts.Add($"Files ({ExtensionPattern})|{ExtensionPattern}");
            }
            else if (FileFilters?.Count != 0)
            {
                foreach (string _File in FileFilters)
                    FilterParts.Add($"{_File}|{_File}");
            }

            if (IncludeAllFiles)
                FilterParts.Add("All Files (*.*)|*.*");

            OpenFileDialog Dialog = new()
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
