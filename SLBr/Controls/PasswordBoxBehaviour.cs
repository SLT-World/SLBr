using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SLBr.Controls
{
    public static class PasswordBoxBehaviour
    {
        //TODO: Address in-memory vulnerability.
        private static bool IsUpdating = false;

        /*public static readonly DependencyProperty BindablePasswordProperty = DependencyProperty.RegisterAttached("BindablePassword", typeof(string), typeof(PasswordBoxBehaviour), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindablePasswordChanged));
        public static string GetBindablePassword(DependencyObject _Object) => (string)_Object.GetValue(BindablePasswordProperty);
        public static void SetBindablePassword(DependencyObject _Object, string Value) => _Object.SetValue(BindablePasswordProperty, Value);*/

        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(PasswordBoxBehaviour), new PropertyMetadata(false, OnEnableChanged));
        public static bool GetEnable(DependencyObject _Object) => (bool)_Object.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject _Object, bool Value) => _Object.SetValue(EnableProperty, Value);

        private static void OnEnableChanged(DependencyObject _Object, DependencyPropertyChangedEventArgs e)
        {
            if (_Object is PasswordBox _PasswordBox)
            {
                if ((bool)e.NewValue)
                {
                    //_PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
                    _PasswordBox.Loaded += PasswordBox_Loaded;
                    _PasswordBox.LostFocus += PasswordBox_LostFocus;
                }
                else
                {
                    //_PasswordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                    _PasswordBox.Loaded -= PasswordBox_Loaded;
                    _PasswordBox.LostFocus -= PasswordBox_LostFocus;
                }
            }
        }
        private static void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox _PasswordBox)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Keyboard.FocusedElement is DependencyObject Element && Utils.FindAncestorOfType<PasswordBox>(Element) == _PasswordBox)
                        return;
                    if (_PasswordBox.Template?.FindName("RevealButton", _PasswordBox) is ToggleButton RevealButton && RevealButton.IsChecked == true)
                    {
                        IsUpdating = true;
                        RevealButton.IsChecked = false;
                        if (_PasswordBox.Template?.FindName("RevealTextBox", _PasswordBox) is TextBox _TextBox)
                            _TextBox.Clear();
                        IsUpdating = false;
                        GC.Collect(0, GCCollectionMode.Optimized);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private static void PasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox _PasswordBox && _PasswordBox.Template != null)
            {
                if (_PasswordBox.Template.FindName("RevealButton", _PasswordBox) is ToggleButton RevealButton)
                {
                    RevealButton.Checked += (s, args) => SyncCaretToTextBox(_PasswordBox);
                    RevealButton.Unchecked += (s, args) => SyncCaretToPasswordBox(_PasswordBox);
                }
                if (_PasswordBox.Template.FindName("RevealTextBox", _PasswordBox) is TextBox _TextBox)
                {
                    _TextBox.TextChanged += (s, args) =>
                    {
                        if (IsUpdating)
                            return;
                        IsUpdating = true;
                        _PasswordBox.Password = _TextBox.Text;
                        IsUpdating = false;
                    };
                }
            }
        }

        private static void SyncCaretToTextBox(PasswordBox _PasswordBox)
        {
            /*if (string.IsNullOrEmpty(GetBindablePassword(_PasswordBox)) && !string.IsNullOrEmpty(_PasswordBox.Password))
            {
                IsUpdating = true;
                SetBindablePassword(_PasswordBox, _PasswordBox.Password);
                IsUpdating = false;
            }
            if (_PasswordBox.Template.FindName("RevealTextBox", _PasswordBox) is TextBox _TextBox)
            {
                (int Start, int Length) = GetPasswordBoxSelection(_PasswordBox);
                _TextBox.Dispatcher.BeginInvoke(() =>
                {
                    _TextBox.Focus();
                    _TextBox.Select(Start, Length);
                }, System.Windows.Threading.DispatcherPriority.Render);
            }*/
            if (_PasswordBox.Template.FindName("RevealTextBox", _PasswordBox) is TextBox _TextBox)
            {
                IsUpdating = true;
                _TextBox.Text = _PasswordBox.Password;
                IsUpdating = false;

                (int Start, int Length) = GetPasswordBoxSelection(_PasswordBox);
                _TextBox.Dispatcher.BeginInvoke(() =>
                {
                    _TextBox.Focus();
                    _TextBox.Select(Start, Length);
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private static void SyncCaretToPasswordBox(PasswordBox _PasswordBox)
        {
            if (Keyboard.FocusedElement is DependencyObject Element && Utils.FindAncestorOfType<PasswordBox>(Element) != _PasswordBox)
                return;
            if (_PasswordBox.Template.FindName("RevealTextBox", _PasswordBox) is TextBox _TextBox)
            {
                int SelectionStart = _TextBox.SelectionStart;
                int SelectionLength = _TextBox.SelectionLength;
                _PasswordBox.Focus();
                SetPasswordBoxSelection(_PasswordBox, SelectionStart, SelectionLength);
            }
        }

        private static readonly PropertyInfo SelectionProperty = typeof(PasswordBox).GetProperty("Selection", BindingFlags.Instance | BindingFlags.NonPublic);

        private static (int, int) GetPasswordBoxSelection(PasswordBox _PasswordBox)
        {
            try
            {
                object Selection = SelectionProperty.GetValue(_PasswordBox, null);

                Type ITextRangeType = Selection.GetType().GetInterfaces().FirstOrDefault(x => x.Name == "ITextRange");

                object StartPosition = ITextRangeType.GetProperty("Start").GetMethod.Invoke(Selection, null);
                object EndPosition = ITextRangeType.GetProperty("End").GetMethod.Invoke(Selection, null);

                MethodInfo GetOffsetMethod = StartPosition.GetType().GetProperty("Offset", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod;

                int RawStart = (int)GetOffsetMethod.Invoke(StartPosition, null);
                int RawEnd = (int)GetOffsetMethod.Invoke(EndPosition, null);

                int Start = Math.Min(RawStart, RawEnd);
                int Length = Math.Abs(RawStart - RawEnd);
                if (Start < 0)
                    Start = 0;
                return (Start, Length);
            }
            catch { }
            return (0, 0);
        }

        private static void SetPasswordBoxSelection(PasswordBox _PasswordBox, int Start, int Length)
        {
            _PasswordBox.GetType().GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(_PasswordBox, [Start, Length]);
        }

        /*private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IsUpdating)
                return;
            if (sender is PasswordBox _PasswordBox)
            {
                IsUpdating = true;
                SetBindablePassword(_PasswordBox, _PasswordBox.Password);
                IsUpdating = false;
            }
        }

        private static void OnBindablePasswordChanged(DependencyObject _Object, DependencyPropertyChangedEventArgs e)
        {
            if (IsUpdating)
                return;
            if (_Object is PasswordBox _PasswordBox)
            {
                IsUpdating = true;
                _PasswordBox.Password = e.NewValue?.ToString() ?? string.Empty;
                IsUpdating = false;
            }
        }*/
    }
}
