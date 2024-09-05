using System.Windows;
using System.Windows.Media.Animation;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for PromptDialogWindow.xaml
    /// </summary>
    public partial class PromptDialogWindow : Window
    {
        public PromptDialogWindow(string _Title, string Question, string Message, string DefaultInputText, string Icon = "")
        {
            Title = _Title;

            InitializeComponent();
            QuestionText.Content = Question;
            MessageText.Text = Message;
            UserInputTextBox.Text = DefaultInputText;
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
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
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
            UserInputTextBox.SelectAll();
            UserInputTextBox.Focus();
        }

        public string UserInput
        {
            get { return UserInputTextBox.Text; }
        }
    }
}
