using CefSharp;
using SLBr.Controls;
using System.Windows;

namespace SLBr.Handlers
{
    public class JsDialogHandler : IJsDialogHandler
    {
        public bool OnBeforeUnloadDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string messageText, bool isReload, IJsDialogCallback callback)
        {
            InformationDialogWindow InfoWindow = new InformationDialogWindow("Confirmation", isReload ? "Reload site?" : "Leave site?", "Changes made may not be saved.", "", isReload ? "Reload" : "Leave", "Cancel");
            InfoWindow.Topmost = true;
            bool Result = InfoWindow.ShowDialog() == true;
            callback.Continue(Result);
            return true;
        }

        public void OnDialogClosed(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }

        public bool OnJSDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, CefJsDialogType dialogType, string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage)
        {
            if (dialogType == CefJsDialogType.Alert)
            {
                InformationDialogWindow InfoWindow = new InformationDialogWindow("Alert", $"{Utils.Host(originUrl)}", messageText);
                InfoWindow.Topmost = true;
                if (InfoWindow.ShowDialog() == true)
                {
                    callback.Continue(true);
                    return true;
                }
            }
            else if (dialogType == CefJsDialogType.Confirm)
            {
                InformationDialogWindow InfoWindow = new InformationDialogWindow("Confirmation", $"{Utils.Host(originUrl)}", messageText, "", "OK", "Cancel");
                InfoWindow.Topmost = true;
                if (InfoWindow.ShowDialog() == true)
                {
                    callback.Continue(true);
                    return true;
                }
            }
            else if (dialogType == CefJsDialogType.Prompt)
            {
                PromptDialogWindow InfoWindow = new PromptDialogWindow("Prompt", $"{Utils.Host(originUrl)}", messageText, defaultPromptText);
                InfoWindow.Topmost = true;
                if (InfoWindow.ShowDialog() == true)
                {
                    callback.Continue(true, InfoWindow.UserInput);
                    return true;
                }
            }
            suppressMessage = true;
            return false;
        }

        public void OnResetDialogState(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                foreach (Window Window in Application.Current.Windows)
                {
                    if (Window is InformationDialogWindow || Window is PromptDialogWindow)
                        Window.Close();
                }
            });
        }
    }
}
