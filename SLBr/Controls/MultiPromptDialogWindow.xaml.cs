using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SLBr.Controls
{
    public class InputField : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string Name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        private string _Value = "";
        public bool IsRequired { get; set; }
        public string Name { get; set; } = "";
        public string Value
        {
            get => _Value;
            set
            {
                _Value = value;
                RaisePropertyChanged(nameof(Value));
            }
        }
    }

    /// <summary>
    /// Interaction logic for MultiPromptDialogWindow.xaml
    /// </summary>
    public partial class MultiPromptDialogWindow : Window
    {
        public ObservableCollection<InputField> InputFields { get; set; }
            = new ObservableCollection<InputField>();

        public MultiPromptDialogWindow(string _Title, string Question, List<InputField> DefaultFields, string Icon = "")
        {
            Title = _Title;

            InitializeComponent();
            InputsList.ItemsSource = InputFields;

            QuestionText.Text = Question;
            foreach (InputField Field in DefaultFields)
            {
                InputFields.Add(Field);
            }
            ValidateInputs(null, null);

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

        private void ValidateInputs(object sender, KeyEventArgs e)
        {
            btnDialogOk.IsEnabled = InputFields.All(f => !f.IsRequired || !string.IsNullOrWhiteSpace(f.Value));
            //MessageBox.Show(string.Join(",", UserInputs));
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

        public List<string> UserInputs => InputFields.Select(i => i.Value).ToList();
    }
}
