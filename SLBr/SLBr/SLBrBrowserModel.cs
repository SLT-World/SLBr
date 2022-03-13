// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
//using Microsoft.mshtml;

namespace SLBr
{
    public class SLBrBrowserModel
    {
        public enum EngineType
        {
            Blink,
            Trident,
            Gecko,
            EdgeHTML,
            WebKit,
        }

        public EngineType _EngineType;
        public ChromiumWebBrowser _Blink;
        public WebBrowser _Trident;
        public float ZoomLevel;
        public float ZoomLevelIncrement;

        public SLBrBrowserModel(EngineType _EngineType, string Url, float _ZoomLevelIncrement = 0, object Custom = null)
        {
            this._EngineType = _EngineType;
            ZoomLevelIncrement = _ZoomLevelIncrement;
            switch (_EngineType)
            {
                case EngineType.Blink:
                    if (Custom != null)
                        _Blink = Custom as ChromiumWebBrowser;
                    else
                        Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            _Blink = new ChromiumWebBrowser();
                        }));
                    //MessageBox.Show(_Blink.ActualWidth.ToString());
                    _Blink.Address = Url;
                    //_Blink.BeginInit();
                    //ZoomLevel = float.Parse(_Blink.ZoomLevel.ToString());
                    //_Blink.ZoomLevelIncrement = ZoomLevelIncrement;
                    break;
                case EngineType.Trident:
                    _Trident = new WebBrowser();
                    //_Trident.Navigate(Url);
                    break;
            }
            //MessageBox.Show(_Blink.Address);
            //Navigate(Url);
        }

        public string Url()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    return _Blink.Address;
                case EngineType.Trident:
                    return _Trident.Source.AbsoluteUri;
            }
            return string.Empty;
        }
        public string Title()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    return _Blink.Title;
                case EngineType.Trident:
                    return /*((dynamic)_Trident.Document).Title;-*/(string)_Trident.InvokeScript("eval", "document.title.toString()");
            }
            return string.Empty;
        }

        /*public object GetEngine()
        {
            switch (_BrowserType)
            {
                case EngineType.Blink:
                    return _Blink;
                case EngineType.Trident:
                    return _Trident;
            }
            return null;
        }*/


        public void Dispose()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    _Blink.Dispose();
                    break;
                case EngineType.Trident:
                    _Trident.Dispose();
                    break;
            }
        }

        public void Navigate(string Url)
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    _Blink.Address = Url;
                    break;
                case EngineType.Trident:
                    _Trident.Navigate(Url);
                    break;
            }
        }
        public void Back()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    _Blink.Back();
                    break;
                case EngineType.Trident:
                    _Trident.GoBack();
                    break;
            }
        }
        public void Forward()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    _Blink.Forward();
                    break;
                case EngineType.Trident:
                    _Trident.GoForward();
                    break;
            }
        }
        public void Refresh()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    _Blink.Reload();
                    break;
                case EngineType.Trident:
                    _Trident.Refresh();
                    break;
            }
        }
        public void Stop()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    if (_Blink.IsBrowserInitialized)
                        _Blink.Stop();
                    break;
                case EngineType.Trident:
                    _Trident.InvokeScript("eval", "document.execCommand('Stop');");
                    break;
            }
        }
        public void Print()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    _Blink.Print();
                    break;
                /*case EngineType.Trident:
                    mshtml.IHTMLDocument2 doc = _Trident.Document as mshtml.IHTMLDocument2;
                    doc.execCommand("Print", true, null);
                    break;*/
            }
        }
        public void ShowDevTools()
        {
            //MessageBox.Show("e");
            switch (_EngineType)
            {
                case EngineType.Blink:
                    if (_Blink.IsBrowserInitialized)
                        _Blink.ShowDevTools();
                    break;
                /*case EngineType.Trident:
                    mshtml.IHTMLDocument2 doc = _Trident.Document as mshtml.IHTMLDocument2;
                    doc.execCommand("Print", true, null);
                    break;*/
            }
        }

        public bool CanGoBack()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    return _Blink.CanGoBack;
                case EngineType.Trident:
                    return _Trident.CanGoBack;
            }
            return true;
        }
        public bool CanGoForward()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    return _Blink.CanGoForward;
                case EngineType.Trident:
                    return _Trident.CanGoForward;
            }
            return true;
        }
        public bool IsLoaded()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    return _Blink.IsLoaded;
                case EngineType.Trident:
                    return _Trident.IsLoaded;
            }
            return true;
        }

        public void SetZoom(float Value)
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    ZoomLevel = Value;
                    _Blink.SetZoomLevel(Value);
                    break;
                /*case EngineType.Trident:
                    mshtml.IHTMLDocument2 doc = _Trident.Document as mshtml.IHTMLDocument2;
                    doc.parentWindow.execScript("document.body.style.zoom=" + Value.ToString().Replace(",", ".") + ";");
                    break;*/
            }
        }
        public void Find(string Text, bool Forward, bool MatchCase, bool FindNext)
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    _Blink.Find(Text, Forward, MatchCase, FindNext);
                    break;
            }
        }
        public void StopFinding(bool ClearSelection)
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    _Blink.StopFinding(ClearSelection);
                    break;
            }
        }

        public string Source()
        {
            switch (_EngineType)
            {
                case EngineType.Blink:
                    string Source = string.Empty;
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        _Blink.GetMainFrame().EvaluateScriptAsync(@"document.getElementsByTagName('html')[0]").ContinueWith(t =>
                        {
                            if (t.Result != null && t.Result.Result != null)
                            {
                                var result = t.Result.Result.ToString();
                                Source = result;
                            }
                        });
                    }));
                    return Source;
                /*case EngineType.Trident:
                    return (_Trident.Document as mshtml.IHTMLDocument2).body.outerHTML;*/
            }
            return "";
        }
    }
}
