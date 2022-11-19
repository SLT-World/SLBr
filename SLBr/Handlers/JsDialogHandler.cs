using CefSharp;
using SLBr.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                var infoWindow = new InformationDialogWindow("Alert", $"{Utils.Host(originUrl)} says", messageText);
                infoWindow.Topmost = true;

                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(true);
                    return true;
                }
            }
            else if (dialogType == CefJsDialogType.Confirm)
            {
                var infoWindow = new InformationDialogWindow("Confirmation", $"{Utils.Host(originUrl)} says", messageText, "OK", "Cancel");
                infoWindow.Topmost = true;

                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(true);
                    return true;
                }
            }
            else if (dialogType == CefJsDialogType.Prompt)
            {
                var infoWindow = new PromptDialogWindow("Prompt", $"{Utils.Host(originUrl)} says", messageText, defaultPromptText);
                infoWindow.Topmost = true;

                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(true, infoWindow.UserInput);
                    return true;
                }
            }
            suppressMessage = true;
            return false;
        }

        public void OnResetDialogState(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }
    }
}
