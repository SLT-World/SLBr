using System.ComponentModel;
using System.Windows.Media;

namespace SLBr.Controls
{
    public class MessageOptions
    {
        public byte[] icon { get; set; }
        public string body { get; set; }
        public string tag { get; set; }
        public bool canReply { get; set; }
        public bool silent { get; set; }
        public bool requireInteraction { get; set; }
    }

    public class Theme : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion
        public Theme(string _Name, Color _PrimaryColor, Color _FontColor, Color _BorderColor, Color _UnselectedTabColor, Color _ControlFontColor, bool _DarkTitleBar = false, bool _DarkWebPage = false)
        {
            Name = _Name;
            PrimaryColor = _PrimaryColor;
            FontColor = _FontColor;
            BorderColor = _BorderColor;
            UnselectedTabColor = _UnselectedTabColor;
            ControlFontColor = _ControlFontColor;
            DarkTitleBar = _DarkTitleBar;
            DarkWebPage = _DarkWebPage;
        }
        public string Name
        {
            get { return DName; }
            set
            {
                DName = value;
                RaisePropertyChanged("Name");
            }
        }
        public Color PrimaryColor
        {
            get { return P; }
            set
            {
                P = value;
                RaisePropertyChanged("PrimaryColor");
            }
        }
        public Color FontColor
        {
            get { return F; }
            set
            {
                F = value;
                RaisePropertyChanged("FontColor");
            }
        }
        public Color BorderColor
        {
            get { return B; }
            set
            {
                B = value;
                RaisePropertyChanged("BorderColor");
            }
        }
        public Color UnselectedTabColor
        {
            get { return UT; }
            set
            {
                UT = value;
                RaisePropertyChanged("UnselectedTabColor");
            }
        }
        public Color ControlFontColor
        {
            get { return CF; }
            set
            {
                CF = value;
                RaisePropertyChanged("ControlFontColor");
            }
        }
        public bool DarkTitleBar
        {
            get { return DTB; }
            set
            {
                DTB = value;
                RaisePropertyChanged("DarkTitleBar");
            }
        }
        public bool DarkWebPage
        {
            get { return DWP; }
            set
            {
                DWP = value;
                RaisePropertyChanged("DarkWebPage");
            }
        }

        private string DName { get; set; }
        private Color P { get; set; }
        private Color F { get; set; }
        private Color B { get; set; }
        private Color UT { get; set; }
        private Color CF { get; set; }
        private bool DTB { get; set; }
        private bool DWP { get; set; }
    }

    public class ActionStorage : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        public ActionStorage(string _Name, string _Arguments, string _Tooltip)
        {
            Name = _Name;
            Arguments = _Arguments;
            Tooltip = _Tooltip;
        }

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
        public string Tooltip
        {
            get { return DTooltip; }
            set
            {
                DTooltip = value;
                RaisePropertyChanged("Tooltip");
            }
        }

        private string DName { get; set; }
        private string DArguments { get; set; }
        private string DTooltip { get; set; }
    }
}
