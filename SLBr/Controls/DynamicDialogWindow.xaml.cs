using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public enum DialogInputType
    {
        Text,
        Label,
        Boolean
    }

    public class InputField : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        private string _Value = "";
        private bool _BoolValue = false;

        public string Name { get; set; } = "";
        public bool IsRequired { get; set; }
        public DialogInputType Type { get; set; } = DialogInputType.Text;

        public string Value
        {
            get => _Value;
            set
            {
                if (_Value != value)
                {
                    _Value = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool BoolValue
        {
            get => _BoolValue;
            set
            {
                if (_BoolValue != value)
                {
                    _BoolValue = value;
                    RaisePropertyChanged();
                }
            }
        }
    }

    /// <summary>
    /// Interaction logic for DynamicDialogWindow.xaml
    /// </summary>
    public partial class DynamicDialogWindow : Window
    {
        /*TODO: Expand features
         * Can add more input types
         * - Password
         * - URL (Validates the input is a valid url)
         * - Dropdown
         * - Date & time? Maybe useless
         */
        public ObservableCollection<InputField> InputFields { get; set; } = new ObservableCollection<InputField>();

        public DynamicDialogWindow(string _Title, string Question, List<InputField> DefaultFields, string Icon = "", string PositiveText = "OK", string NegativeText = "Cancel")
        {
            Title = _Title;

            InitializeComponent();
            InputsList.ItemsSource = InputFields;

            TitleText.Text = Question;
            foreach (InputField Field in DefaultFields)
                InputFields.Add(Field);
            ValidateInputs(null, null);

            if (!string.IsNullOrEmpty(Icon))
                TitleIcon.Text = Icon;

            PositiveButton.Visibility = string.IsNullOrEmpty(PositiveText) ? Visibility.Collapsed : Visibility.Visible;
            PositiveButton.Content = PositiveText;
            NegativeButton.Visibility = string.IsNullOrEmpty(NegativeText) ? Visibility.Collapsed : Visibility.Visible;
            NegativeButton.Content = NegativeText;

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
        private async void PositiveButton_Click(object sender, RoutedEventArgs e)
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
            TextBox FirstTextBox = InputStack.Children.OfType<TextBox>().FirstOrDefault();
            if (FirstTextBox != null)
            {
                FirstTextBox.SelectAll();
                FirstTextBox.Focus();
            }
        }

        private void ValidateInputs(object sender, KeyEventArgs e)
        {
            PositiveButton.IsEnabled = InputFields.All(i => i.Type != DialogInputType.Text || !i.IsRequired || !string.IsNullOrWhiteSpace(i.Value));
        }
    }
}
