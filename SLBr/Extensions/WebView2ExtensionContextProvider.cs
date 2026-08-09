/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.WebView;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SLBr.Extensions
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class WebView2ExtensionContextProvider(IWebView View)
    {
        private readonly IWebView TargetView = View;

        public string GetActiveTab()
        {
            var Payload = new
            {
                url = TargetView.Address,
                title = TargetView.Title,
                isLoading = TargetView.IsLoading,
                width = (int)TargetView.Control.ActualWidth,
                height = (int)TargetView.Control.ActualHeight,
                audible = TargetView.AudioPlaying,
                muted = TargetView.IsMuted,
            };

            return JsonSerializer.Serialize(Payload);
        }
    }
}
