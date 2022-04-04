using System;
using System.Windows;

namespace SLBr
{
    public partial class StyleResourceDictionary : ResourceDictionary
    {
        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.Instance.ButtonAction(sender, e);
            }));
        }
    }
}
