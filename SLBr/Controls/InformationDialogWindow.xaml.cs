using System.Windows;
using System.Windows.Media.Animation;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for InformationPopup.xaml
    /// </summary>
    public partial class InformationDialogWindow : Window
    {
        public InformationDialogWindow(string _Title, string Question, string Description, string _Icon = "", string PositiveText = "OK", string NegativeText = "", string _FluentIconsText = "")
        {
            InitializeComponent();

            Title = _Title;

            QuestionText.Content = Question;
            if (!string.IsNullOrEmpty(_Icon))
                QuestionIcon.Text = _Icon;
            DescriptionText.Text = Description;
            ApplyTheme(App.Instance.CurrentTheme);

            IconText.Text = _FluentIconsText;
            IconText.Visibility = string.IsNullOrEmpty(_FluentIconsText) ? Visibility.Collapsed : Visibility.Visible;

            PositiveButton.Visibility = string.IsNullOrEmpty(PositiveText) ? Visibility.Collapsed : Visibility.Visible;
            PositiveButton.Content = PositiveText;
            NegativeButton.Visibility = string.IsNullOrEmpty(NegativeText) ? Visibility.Collapsed : Visibility.Visible;
            NegativeButton.Content = NegativeText;
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
            //if (this.IsModal())
            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.125)
            });
            await Task.Delay(0125);
            DialogResult = true;
        }
    }
}
