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
            App.Instance.CurrentFocusedWindow().GetBrowserTabWithId(TabId).Icon = new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", (App.Instance.CurrentTheme.DarkTitleBar ? "White Tab Icon.png" : "Black Tab Icon.png"))));
            //((Image)sender).Visibility = Visibility.Collapsed;
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                App.Instance.CurrentFocusedWindow().ButtonAction(sender, e);
            }));
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            TabItem tabItem = (TabItem)e.Source;
            if (Mouse.PrimaryDevice.MiddleButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
        }
        /*private void TabItem_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.All;
            }
            e.Handled = false;
        }*/

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            //MessageBox.Show(e.Data.GetDataPresent(DataFormats.FileDrop).ToString());//File drop
            //MessageBox.Show(e.Data.GetDataPresent(DataFormats.Xaml).ToString());
            //MessageBox.Show(e.Data.GetDataPresent(DataFormats.Html).ToString());//String
            //MessageBox.Show(e.Data.GetDataPresent(DataFormats.StringFormat).ToString());//String
            //MessageBox.Show((e.Data.GetData(typeof(TabItem)) != null).ToString());//Tab

            bool IsTab = e.Data.GetData(typeof(TabItem)) != null;
            bool IsString = e.Data.GetDataPresent(DataFormats.StringFormat);
            bool IsFileDrop = e.Data.GetDataPresent(DataFormats.FileDrop);

            if (IsTab)
            {
                TabItem TabItemSource = (TabItem)e.Data.GetData(typeof(TabItem));
                TabItem TabItemTarget = (TabItem)e.Source;

                int TabItemSourceId = int.Parse(TabItemSource.Tag.ToString());
                int TabItemTargetId = int.Parse(TabItemTarget.Tag.ToString());

                BrowserTabItem BrowserTabItemSource = App.Instance.CurrentFocusedWindow().GetBrowserTabWithId(TabItemSourceId);
                BrowserTabItem BrowserTabItemTarget = App.Instance.CurrentFocusedWindow().GetBrowserTabWithId(TabItemTargetId);

                if (TabItemTargetId != TabItemSourceId)
                {
                    int TargetIndex = App.Instance.CurrentFocusedWindow().Tabs.IndexOf(BrowserTabItemTarget);
                    bool IsOriginallySelected = TabItemSource.IsSelected;

                    App.Instance.CurrentFocusedWindow().Tabs.Remove(BrowserTabItemSource);
                    App.Instance.CurrentFocusedWindow().Tabs.Insert(TargetIndex, BrowserTabItemSource);
                    if (IsOriginallySelected)
                        App.Instance.CurrentFocusedWindow().BrowserTabs.SelectedIndex = TargetIndex;
                }
                e.Handled = true;
            }
            else if (IsFileDrop)
            {
                string[] FileLoadup = (string[])e.Data.GetData(DataFormats.FileDrop);
                App.Instance.CurrentFocusedWindow().NewBrowserTab(FileLoadup[0], 0, true);
                e.Handled = true;
            }
            else if (IsString)
            {
                string Url = (string)e.Data.GetData(DataFormats.StringFormat);
                App.Instance.CurrentFocusedWindow().NewBrowserTab(Utils.FilterUrlForBrowser(Url, App.Instance.MainSave.Get("Search_Engine")), 0, true);
                e.Handled = true;
            }

            //e.Source is TabItem tabItemTarget && e.Data.GetData(typeof(TabItem)) is TabItem tabItemSource && 
        }
    }
}
