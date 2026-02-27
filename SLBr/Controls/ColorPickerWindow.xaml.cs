/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Controls;
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
            ApplyColor(DefaultColor);
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
            HexInputTextBox.SelectAll();
            HexInputTextBox.Focus();
        }

        private Color SelectedHue;
        private void HueClick(object sender, MouseButtonEventArgs e)
        {
            HueBar.Focus();
            Keyboard.ClearFocus();
            double Percent = e.GetPosition(HueBar).X / HueBar.ActualWidth;
            SelectedHue = Utils.ColorFromHSV(Percent * 360, 1, 1);
            HueBrush.Color = SelectedHue;
        }

        private void SVClick(object sender, MouseButtonEventArgs e)
        {
            SVSquare.Focus();
            Keyboard.ClearFocus();
            Point Position = e.GetPosition(SVSquare);
            double Saturation = Position.X / SVSquare.ActualWidth;
            double Value = 1 - (Position.Y / SVSquare.ActualHeight);

            Color _Color = Utils.ColorFromHSV(Utils.GetHue(SelectedHue), Saturation, Value);
            ApplyColor(_Color);
        }

        public SolidColorBrush UserInput
        {
            get { return (SolidColorBrush)PreviewBox.Fill; }
        }

        private void HexInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DisableTextChangedEvents)
                return;
            if (HexInputTextBox.Text.Length == 6)
                ApplyColor(Utils.HexToColor(HexInputTextBox.Text));
        }

        private void ApplyColor(Color _Color)
        {
            if (_Color == Colors.Transparent)
                _Color = Colors.Black;
            PreviewBox.Fill = new SolidColorBrush(_Color);
            if (!IsUserTypingRGBHSL)
                UpdateInputs();
            Utils.ColorToHSV(_Color, out double H, out _, out _);
            SelectedHue = Utils.ColorFromHSV(H, 1, 1);
            HueBrush.Color = SelectedHue;
        }

        bool DisableTextChangedEvents;

        private void ColorFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInputs();
        }

        private void UpdateInputs()
        {
            var Color = ((SolidColorBrush)PreviewBox.Fill).Color;
            DisableTextChangedEvents = true;
            switch (ColorFormatComboBox.SelectedIndex)
            {
                case 0:
                    HexInputContainer.Visibility = Visibility.Visible;
                    ThreeInputContainer.Visibility = Visibility.Collapsed;

                    HexInputTextBox.Text = Utils.ColorToHex(Color, false);
                    break;
                case 1:
                    FirstInputHint.Text = "R";
                    SecondInputHint.Text = "G";
                    ThirdInputHint.Text = "B";
                    HexInputContainer.Visibility = Visibility.Collapsed;
                    ThreeInputContainer.Visibility = Visibility.Visible;

                    FirstInputTextBox.Text = Color.R.ToString();
                    SecondInputTextBox.Text = Color.G.ToString();
                    ThirdInputTextBox.Text = Color.B.ToString();
                    break;
                case 2:
                    FirstInputHint.Text = "H";
                    SecondInputHint.Text = "S";
                    ThirdInputHint.Text = "L";
                    HexInputContainer.Visibility = Visibility.Collapsed;
                    ThreeInputContainer.Visibility = Visibility.Visible;

                    Utils.ColorToHSL(Color, out double H, out double S, out double L);

                    FirstInputTextBox.Text = ((int)H).ToString();
                    SecondInputTextBox.Text = ((int)(S * 100)).ToString();
                    ThirdInputTextBox.Text = ((int)(L * 100)).ToString();
                    break;
            }
            DisableTextChangedEvents = false;
        }

        private void ThreeInputTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void ThreeInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        bool IsUserTypingRGBHSL;

        private void ThreeInputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            IsUserTypingRGBHSL = true;
        }

        private void ThreeInputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            IsUserTypingRGBHSL = false;
            if (DisableTextChangedEvents)
                return;
            if (FirstInputTextBox.Text.Length == 0 || SecondInputTextBox.Text.Length == 0 || ThirdInputTextBox.Text.Length == 0)
                return;
            try
            {
                switch (ColorFormatComboBox.SelectedIndex)
                {
                    case 1:
                        byte R = (byte)Math.Clamp(int.Parse(FirstInputTextBox.Text), 0, 255);
                        byte G = (byte)Math.Clamp(int.Parse(SecondInputTextBox.Text), 0, 255);
                        byte B = (byte)Math.Clamp(int.Parse(ThirdInputTextBox.Text), 0, 255);

                        ApplyColor(Color.FromRgb(R, G, B));
                        break;
                    case 2:
                        double H = Math.Clamp(double.Parse(FirstInputTextBox.Text), 0, 360);
                        double S = Math.Clamp(double.Parse(SecondInputTextBox.Text), 0, 100) / 100.0;
                        double L = Math.Clamp(double.Parse(ThirdInputTextBox.Text), 0, 100) / 100.0;

                        ApplyColor(Utils.ColorFromHSL(H, S, L));
                        break;
                }
            }
            catch { }
            UpdateInputs();
        }
    }
}
