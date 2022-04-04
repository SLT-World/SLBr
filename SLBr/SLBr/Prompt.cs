// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a GNU license that can be found in the LICENSE file.
using System.ComponentModel;
using System.Windows;

namespace SLBr
{
    public class Prompt : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public bool CloseOnTabSwitch
        {
            get { return PCloseOnTabSwitch; }
            set
            {
                PCloseOnTabSwitch = value;
                RaisePropertyChanged("CloseOnTabSwitch");
            }
        }
        public string Content
        {
            get { return PContent; }
            set
            {
                PContent = value;
                RaisePropertyChanged("Content");
            }
        }
        public Visibility ButtonVisibility
        {
            get { return PButtonVisibility; }
            set
            {
                PButtonVisibility = value;
                RaisePropertyChanged("ButtonVisibility");
            }
        }
        public string ButtonContent
        {
            get { return PButtonContent; }
            set
            {
                PButtonContent = value;
                RaisePropertyChanged("ButtonContent");
            }
        }
        public string ButtonTag
        {
            get { return PButtonTag; }
            set
            {
                PButtonTag = value;
                RaisePropertyChanged("ButtonTag");
            }
        }
        public string ButtonToolTip
        {
            get { return PButtonToolTip; }
            set
            {
                PButtonToolTip = value;
                RaisePropertyChanged("ButtonToolTip");
            }
        }
        public string CloseButtonTag
        {
            get { return PCloseButtonTag; }
            set
            {
                PCloseButtonTag = value;
                RaisePropertyChanged("CloseButtonTag");
            }
        }
        public Visibility IconVisibility
        {
            get { return PIconVisibility; }
            set
            {
                PIconVisibility = value;
                RaisePropertyChanged("IconVisibility");
            }
        }
        public string IconText
        {
            get { return PIconText; }
            set
            {
                PIconText = value;
                RaisePropertyChanged("IconText");
            }
        }
        public string IconRotation
        {
            get { return PIconRotation; }
            set
            {
                PIconRotation = value;
                RaisePropertyChanged("IconRotation");
            }
        }

        public bool PCloseOnTabSwitch { get; set; }
        public string PContent { get; set; }
        public Visibility PButtonVisibility { get; set; }
        public string PButtonContent { get; set; }
        public string PButtonTag { get; set; }
        public string PButtonToolTip { get; set; }
        public string PCloseButtonTag { get; set; }
        public Visibility PIconVisibility { get; set; }
        public string PIconText { get; set; }
        public string PIconRotation { get; set; }
    }
}
