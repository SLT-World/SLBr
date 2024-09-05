using CefSharp;
using SLBr.Controls;

namespace SLBr.Handlers
{
    public class JsDialogHandler : IJsDialogHandler
    {
        public bool OnBeforeUnloadDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string messageText, bool isReload, IJsDialogCallback callback)
        {
            return false;
        }

        public void OnDialogClosed(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }

        public bool OnJSDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, CefJsDialogType dialogType, string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage)
        {
            if (dialogType == CefJsDialogType.Alert)
            {
                var infoWindow = new InformationDialogWindow("Alert", $"{Utils.Host(originUrl)}", messageText);
                infoWindow.Topmost = true;
                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(true);
                    return true;
                }
            }
            else if (dialogType == CefJsDialogType.Confirm)
            {
                var infoWindow = new InformationDialogWindow("Confirmation", $"{Utils.Host(originUrl)}", messageText, "OK", "Cancel");
                infoWindow.Topmost = true;
                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(true);
                    return true;
                }
            }
            else if (dialogType == CefJsDialogType.Prompt)
            {
                var infoWindow = new PromptDialogWindow("Prompt", $"{Utils.Host(originUrl)}", messageText, defaultPromptText);
                infoWindow.Topmost = true;
                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(true, infoWindow.UserInput);
                    return true;
                }
            }
            /*var _InfoWindow = null;
            if (dialogType == CefJsDialogType.Alert)
                _InfoWindow = new InformationDialogWindow("Alert", $"{Utils.Host(originUrl)}", messageText);
            else if (dialogType == CefJsDialogType.Confirm)
                _InfoWindow = new InformationDialogWindow("Confirmation", $"{Utils.Host(originUrl)}", messageText, "OK", "Cancel");
            else if (dialogType == CefJsDialogType.Prompt)
                _InfoWindow = new PromptDialogWindow("Prompt", $"{Utils.Host(originUrl)}", messageText, defaultPromptText);
            _InfoWindow.Topmost = true;
            if (_InfoWindow.ShowDialog() == true)
            {
                if (dialogType == CefJsDialogType.Prompt)
                    callback.Continue(true, _InfoWindow.UserInput);
                else
                    callback.Continue(true);
                return true;
            }*/
            suppressMessage = true;
            return false;
        }

        public void OnResetDialogState(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }
    }
}
