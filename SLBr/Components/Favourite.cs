// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using System.ComponentModel;

namespace SLBr
{
    public class Favourite : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public string Name
        {
            get { return DName; }
            set
            {
                DName = value;
                RaisePropertyChanged("Name");
            }
        }
        public string Arguments
        {
            get { return DArguments; }
            set
            {
                DArguments = value;
                RaisePropertyChanged("Arguments");
            }
        }
        public string Address
        {
            get { return DUrl; }
            set
            {
                DUrl = value;
                RaisePropertyChanged("Url");
            }
        }

        private string DName { get; set; }
        private string DArguments { get; set; }
        private string DUrl { get; set; }
    }
}
