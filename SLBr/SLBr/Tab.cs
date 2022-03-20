//Copyright © 2022 SLT World.All rights reserved.
//Use of this source code is governed by a GNU license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLBr
{
    public class Tab : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public bool IsSettingsTab
        {
            get { return DIsSettingsTab; }
            set
            {
                DIsSettingsTab = value;
                RaisePropertyChanged("IsSettingsTab");
            }
        }
        public bool DIsSettingsTab { get; set; }

        public string Header
        {
            get { return DHeader; }
            set
            {
                DHeader = value;
                RaisePropertyChanged("Header");
            }
        }
        public object Content
        {
            get { return DContent; }
            set
            {
                DContent = value;
                RaisePropertyChanged("Content");
            }
        }

        public string DHeader { get; set; }
        public object DContent { get; set; }
    }
}
