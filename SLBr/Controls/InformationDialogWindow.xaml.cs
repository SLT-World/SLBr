using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for InformationPopup.xaml
    /// </summary>
    public partial class InformationDialogWindow : Window
    {
        public InformationDialogWindow(string _Title, string Question, string Description, string PositiveText = "OK", string NegativeText = "", string _FluentIconsText = "")
        {
            InitializeComponent();

            Title = _Title;

            QuestionText.Content = Question;
            DescriptionText.Text = Description;
            ApplyTheme(MainWindow.Instance.GetTheme());

            FluentIconsText.Text = _FluentIconsText;

            PositiveButton.Visibility = string.IsNullOrEmpty(PositiveText) ? Visibility.Collapsed : Visibility.Visible;
            PositiveButton.Content = PositiveText;
            NegativeButton.Visibility = string.IsNullOrEmpty(NegativeText) ? Visibility.Collapsed : Visibility.Visible;
            NegativeButton.Content = NegativeText;
        }
        public void ApplyTheme(Theme _Theme)
        {
            //Resources["PrimaryBrush"] = new SolidColorBrush(_Theme.PrimaryColor);
            //Resources["FontBrush"] = new SolidColorBrush(_Theme.FontColor);
            //Resources["BorderBrush"] = new SolidColorBrush(_Theme.BorderColor);
            //Resources["UnselectedTabBrush"] = new SolidColorBrush(_Theme.UnselectedTabColor);
            //Resources["ControlFontBrush"] = new SolidColorBrush(_Theme.ControlFontColor);

            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["UnselectedTabBrushColor"] = _Theme.UnselectedTabColor;
            Resources["ControlFontBrushColor"] = _Theme.ControlFontColor;

            //Storyboard s = (Storyboard)Resources["ToBorderBrushColor"];
            //s.SetValue(value)

            //Resources["ToBorderBrushColor"] = new Storyboard(new ColorAnimation()).Color;
        }
        private void DialogOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
