/*using System.Windows;

namespace SLBr.Controls
{
    public class CredentialsDialogResult
    {
        public bool Accepted;
        public string Username;
        public string Password;

        public CredentialsDialogResult(bool _Accepted, string _Username, string _Password)
        {
            Accepted = _Accepted;
            Username = _Username;
            Password = _Password;
        }
    }
    public static class CredentialsDialog
    {
        public static CredentialsDialogResult Show(string Question)
        {
            CredentialsDialogWindow _CredentialsDialogWindow;
            bool DialogResult = false;
            string Username = "";
            string Password = "";
            Application.Current.Dispatcher.Invoke(() =>
            {
                _CredentialsDialogWindow = new CredentialsDialogWindow(Question, "\uec19");
                _CredentialsDialogWindow.Topmost = true;
                DialogResult = _CredentialsDialogWindow.ShowDialog().ToBool();
                Username = _CredentialsDialogWindow.Username;
                Password = _CredentialsDialogWindow.Password;
            });
            return new CredentialsDialogResult(DialogResult, Username, Password);
        }
    }
}*/
