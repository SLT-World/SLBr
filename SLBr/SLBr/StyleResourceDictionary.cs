using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //MessageBox.Show($"{e.Source is string},{(string)e.Source}");
            if (!(e.Source is TabItem tabItem))
                return;
            if (Mouse.PrimaryDevice.MiddleButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            //MessageBox.Show($"{e.Source is string},{(string)e.Source},{e.Data.GetData(typeof(string))}");
            if (e.Source is TabItem tabItemTarget && e.Data.GetData(typeof(TabItem)) is TabItem tabItemSource && !tabItemTarget.Equals(tabItemSource) && tabItemTarget.Parent is TabControl tabControl)
                //Remove the last check if multi tabcontrols
            {
                int targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                tabControl.Items.Remove(tabItemSource);
                tabControl.Items.Insert(targetIndex, tabItemSource);
                tabItemSource.IsSelected = true;
            }
            /*else if (e.Source is string UrlTarget && e.Data.GetData(typeof(string)) is string UrlSource && !UrlTarget.Equals(UrlSource))
            {
                TabControl tabcontrol = sender as TabControl;
                MainWindow.Instance.CreateBrowserTab(Utils.FilterUrlForBrowser(UrlTarget, MainWindow.Instance.MainSave.Get("Search_Engine"), false));
            }*/
        }
    }
}
