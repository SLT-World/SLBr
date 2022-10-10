using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SLBr
{
    public partial class StyleResourceDictionaryCode
    {
        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            int TabId = int.Parse(((Image)sender).Tag.ToString());
            //((Image)sender).Source = new BitmapImage(new Uri("https://example.com/abc.png"));
            MainWindow.Instance.GetBrowserTabWithId(TabId).Icon = new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", (MainWindow.Instance.GetTheme().DarkTitleBar ? "White Tab Icon.png" : "Black Tab Icon.png"))));
            //((Image)sender).Visibility = Visibility.Collapsed;
        }

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
            TabItem tabItem = (TabItem)e.Source;
            //if (!(e.Source is TabItem tabItem))
            //    return;
            if (Mouse.PrimaryDevice.MiddleButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            //MessageBox.Show(e.Source.GetType().ToString());
            if (e.Source is TabItem tabItemTarget && e.Data.GetData(typeof(TabItem)) is TabItem tabItemSource && !tabItemTarget.Equals(tabItemSource) && tabItemTarget.Parent is TabControl tabControl)
            //Remove the last check if multi tabcontrols
            {
                int targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                //int targetIndex = MainWindow.Instance.Tabs[tabControl.Items.IndexOf(tabItemTarget)];
                BrowserTabItem _TabItemSource = MainWindow.Instance.Tabs[targetIndex];
                MainWindow.Instance.Tabs.Remove(_TabItemSource);
                MainWindow.Instance.Tabs.Insert(targetIndex, _TabItemSource);
                tabControl.SelectedIndex = targetIndex;
                //tabItemSource.IsSelected = true;
            }
        }
    }
}
