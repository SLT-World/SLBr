using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SLBr
{
    public class Theme : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion
        public Theme(string _Name, Color _PrimaryColor, Color _FontColor, Color _BorderColor, Color _UnselectedTabColor, Color _ControlFontColor)
        {
            Name = _Name;
            PrimaryColor = _PrimaryColor;
            FontColor = _FontColor;
            BorderColor = _BorderColor;
            UnselectedTabColor = _UnselectedTabColor;
            ControlFontColor = _ControlFontColor;
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

        private string DName { get; set; }
        private Color P { get; set; }
        private Color F { get; set; }
        private Color B { get; set; }
        private Color UT { get; set; }
        private Color CF { get; set; }
    }
}
