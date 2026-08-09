/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.WebView;
using System.Windows;

namespace SLBr.Extensions
{
    public class CefExtensionContextProvider(IWebView View)
    {
        private readonly IWebView TargetView = View;

        public object GetActiveTab()
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                return new
                {
                    url = TargetView.Address,
                    title = TargetView.Title,
                    isLoading = TargetView.IsLoading,
                    width = (int)TargetView.Control.ActualWidth,
                    height = (int)TargetView.Control.ActualHeight,
                    audible = TargetView.AudioPlaying,
                    muted = TargetView.IsMuted,
                };
            });
        }
    }
}
