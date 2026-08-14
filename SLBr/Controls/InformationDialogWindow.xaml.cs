/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

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

            QuestionText.Text = Question;
            if (!string.IsNullOrEmpty(_Icon))
            {
                char FirstCharacter = _Icon[0];
                bool IsIcon = _Icon.Length == 1 || (FirstCharacter >= 0xE000 && FirstCharacter <= 0xF8FF) || (FirstCharacter >= 0x2600 && FirstCharacter <= 0x26FF);
                if (IsIcon)
                    QuestionIcon.Text = _Icon;
                else
                {
                    QuestionIcon.Visibility = Visibility.Collapsed;
                    QuestionImageIcon.Visibility = Visibility.Visible;
                    QuestionImageIcon.Source = new BitmapImage(new Uri(_Icon));
                }
            }
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

        private async void PositiveButton_Click(object sender, RoutedEventArgs e)
        {
            PositiveButton.IsEnabled = false;
            NegativeButton.IsEnabled = false;
            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.125)
            });
            await Task.Delay(125);
            DialogResult = true;
        }
    }
}
