using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Animation;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for ReportWindow.xaml
    /// </summary>
    public partial class ReportWindow : Window
    {
        public ReportWindow()
        {
            InitializeComponent();
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

        private async void ReportBugButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(new TextRange(ExplanationRichTextBox.Document.ContentStart, ExplanationRichTextBox.Document.ContentEnd).Text.Trim()))
                return;
            App.Instance.DiscordWebhookSendInfo($"**Bug Report**\n" +
                $"> - Version: `{App.Instance.ReleaseVersion}`\n\n" +
                $"Urgent: `{UrgentCheckBox.IsChecked.ToBool()}`\n\n" +
                $"Message: ```{new TextRange(ExplanationRichTextBox.Document.ContentStart, ExplanationRichTextBox.Document.ContentEnd).Text.Trim()} ```\n" +
                $"Steps to reproduce: ```{new TextRange(STRRichTextBox.Document.ContentStart, STRRichTextBox.Document.ContentEnd).Text.Trim()} ```\n");
            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.125)
            });
            await Task.Delay(0125);
            Close();
        }
    }
}
