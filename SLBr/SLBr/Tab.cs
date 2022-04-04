//Copyright © 2022 SLT World.All rights reserved.
//Use of this source code is governed by a GNU license that can be found in the LICENSE file.

using CefSharp;
using CefSharp.Wpf;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SLBr
{
    public class Tab : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        /*private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }*/

        protected void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            field = value;
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public Tab(string _Address)
        {
            Address = _Address;
            Blink = new ChromiumWebBrowser(_Address);
        }

        private string _IsSettingsTab;
        public string IsSettingsTab
        {
            get { return _IsSettingsTab; }
            set { Set(ref _IsSettingsTab, value); }
        }

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { Set(ref _Title, value); }
        }

        private string _Address;
        public string Address
        {
            get { return _Address; }
            set { Set(ref _Address, value); }
        }

        private ChromiumWebBrowser _Blink;
        public ChromiumWebBrowser Blink
        {
            get { return _Blink; }
            set { Set(ref _Blink, value); }
        }

        private async Task<object> EvaluateJavaScript(string s)
        {
            try
            {
                var response = await _Blink.EvaluateScriptAsync(s);
                if (response.Success && response.Result is IJavascriptCallback)
                {
                    response = await ((IJavascriptCallback)response.Result).ExecuteAsync("This is a callback from EvaluateJavaScript");
                }

                return response.Success ? (response.Result ?? "null") : response.Message;
            }
            catch (Exception e)
            {
                return "Error while evaluating Javascript: " + e.Message;
            }
        }
        private void ExecuteJavaScript(string s)
        {
            try
            {
                Blink.ExecuteScriptAsync(s);
            }
            catch (Exception e)
            {
                //return "Error while evaluating Javascript: " + e.Message;
            }
        }
    }
}
