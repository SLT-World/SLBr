using System.Windows;
using System.Windows.Media.Animation;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for CredentialsDialog.xaml
    /// </summary>
    public partial class CredentialsDialogWindow : Window
    {
        public CredentialsDialogWindow(string Question, string Icon = "")
        {
            InitializeComponent();

            QuestionText.Content = Question;
            if (!string.IsNullOrEmpty(Icon))
                QuestionIcon.Text = Icon;
			ApplyTheme(App.Instance.CurrentTheme);
            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.125)
            });
        }

        public void ApplyTheme(Theme _Theme)
        {
            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;
        }
        private async void DialogOk_Click(object sender, RoutedEventArgs e)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.125)
            });
            await Task.Delay(0125);
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
			get { return PasswordTextBox.Password; }
		}
	}
}
