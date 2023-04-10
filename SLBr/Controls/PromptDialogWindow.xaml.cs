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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for PromptDialogWindow.xaml
    /// </summary>
    public partial class PromptDialogWindow : Window
    {
        public PromptDialogWindow(string _Title, string Question, string Message, string DefaultInputText)
        {
            InitializeComponent();

            Title = _Title;

            QuestionText.Content = Question;
            MessageText.Text = Message;
            UserInputTextBox.Text = DefaultInputText;
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
            UserInputTextBox.SelectAll();
            UserInputTextBox.Focus();
        }

        public string UserInput
        {
            get { return UserInputTextBox.Text; }
        }
    }
}
