// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr
{
    class JSBindingHandler
    {
        //Functions in this script are accessible by JS using the code `slbr.FUNCTION()`

        /*public string executableLocation() =>
            MainWindow.Instance.ExecutableLocation;
        public string chromiumVersion =>
            Cef.ChromiumVersion;

        public bool hasDebugger() =>
            Utils.HasDebugger();
        public string cleanUrl(string Url) =>
            Utils.CleanUrl(Url);*/

        public string SearchProviderPrefix()
        {
            return MainWindow.Instance.MainSave.Get("Search_Engine");
            //return "Bruh";
        }
        public string SayHello(string name) { return $"Hello {name}!"; }

        public void Back()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Undo();
            }));
        }
        public void Forward()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Redo();
            }));
        }
        public void Refresh()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Refresh();
            }));
        }

        public void PromptExample()
        {
            Prompt("Example Message", "", "");
        }

        public void Prompt(string Message, string ButtonUrl, string ButtonMessage)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.Prompt(Message, ButtonMessage.Trim().Length > 0 ? true : false, ButtonMessage, $"24<,>{ButtonUrl}");
            }));
        }
    }
}
