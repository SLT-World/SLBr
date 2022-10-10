using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLBr.Controls
{
    public class CredentialsDialogResult
    {
        public bool Accepted;
        public string Username;
        public string Password;

        public CredentialsDialogResult(bool? _Accepted, string _Username, string _Password)
        {
            Accepted = (bool)_Accepted;
            Username = _Username;
            Password = _Password;
        }
    }
    public static class CredentialsDialog
    {
        public static CredentialsDialogResult Show(string Question)
        {
            CredentialsDialogWindow _CredentialsDialogWindow = new CredentialsDialogWindow(Question);
            _CredentialsDialogWindow.Topmost = true;
            return new CredentialsDialogResult(_CredentialsDialogWindow.ShowDialog(), _CredentialsDialogWindow.Username, _CredentialsDialogWindow.Password);
        }
    }
}
