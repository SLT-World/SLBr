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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for CredentialsDialog.xaml
    /// </summary>
    public partial class CredentialsDialogWindow : Window
    {
        public CredentialsDialogWindow(string Question)
        {
            InitializeComponent();

            lblQuestion.Content = Question;
			ApplyTheme(App.Instance.CurrentTheme);
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

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			UsernameTextBox.SelectAll();
			UsernameTextBox.Focus();
		}

		public string Username
		{
			get { return UsernameTextBox.Text; }
		}
		public string Password
		{
			get { return PasswordTextBox.Text; }
		}
	}
}
