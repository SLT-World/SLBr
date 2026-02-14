/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        public ColorPickerWindow(Color DefaultColor)
        {
            InitializeComponent();
            SelectedHue = DefaultColor;
            PreviewBox.Fill = new SolidColorBrush(DefaultColor);
            UserInputTextBox.Text = Utils.ColorToHex(DefaultColor);
            Utils.ColorToHSV(DefaultColor, out double h, out double s, out double v);

            SelectedHue = Utils.ColorFromHSV(h, 1, 1);
            HueBrush.Color = SelectedHue;
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

        private Color SelectedHue;
        private void HueClick(object sender, MouseButtonEventArgs e)
        {
            double Percent = e.GetPosition(HueBar).X / HueBar.ActualWidth;
            SelectedHue = Utils.ColorFromHSV(Percent * 360, 1, 1);
            HueBrush.Color = SelectedHue;
        }

        private void SVClick(object sender, MouseButtonEventArgs e)
        {
            Point Position = e.GetPosition(SVSquare);
            double Saturation = Position.X / SVSquare.ActualWidth;
            double Value = 1 - (Position.Y / SVSquare.ActualHeight);

            Color _Color = Utils.ColorFromHSV(Utils.GetHue(SelectedHue), Saturation, Value);
            PreviewBox.Fill = new SolidColorBrush(_Color);
            UserInputTextBox.Text = Utils.ColorToHex(_Color);
        }

        public SolidColorBrush UserInput
        {
            get { return (SolidColorBrush)PreviewBox.Fill; }
        }

        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            ApplyHexInput();
        }

        private void ApplyHexInput()
        {
            string Hex = UserInputTextBox.Text.Trim();
            Color _Color = Utils.HexToColor(Hex);
            if (_Color == Colors.Transparent)
                _Color = Colors.Black;
            PreviewBox.Fill = new SolidColorBrush(_Color);
            Utils.ColorToHSV(_Color, out double h, out double s, out double v);

            SelectedHue = Utils.ColorFromHSV(h, 1, 1);
            HueBrush.Color = SelectedHue;
        }
    }
}
