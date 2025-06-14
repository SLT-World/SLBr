using CefSharp;
using SLBr.Controls;
using System.Windows;

namespace SLBr.Handlers
{
    public class DialogHandler: IDialogHandler
    {
        bool IDialogHandler.OnFileDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, IReadOnlyCollection<string> acceptFilters, IReadOnlyCollection<string> acceptExtensions, IReadOnlyCollection<string> acceptDescriptions, IFileDialogCallback callback)
        {
            return OnFileDialog(chromiumWebBrowser, browser, mode, title, defaultFilePath, acceptFilters, acceptExtensions, acceptDescriptions, callback);
        }
        protected virtual bool OnFileDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, IReadOnlyCollection<string> acceptFilters, IReadOnlyCollection<string> acceptExtensions, IReadOnlyCollection<string> acceptDescriptions, IFileDialogCallback callback)
        {
            if (bool.Parse(App.Instance.GlobalSave.Get("QuickImage")) && acceptFilters.FirstOrDefault() == "image/*")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var Picker = new ImageTray();
                    if (Picker.ShowDialog() == true && !string.IsNullOrEmpty(Picker.SelectedFilePath))
                        callback.Continue(new List<string> { Picker.SelectedFilePath });
                    else
                        callback.Cancel();
                });
                return true;
            }
            return false;
        }
    }
}
