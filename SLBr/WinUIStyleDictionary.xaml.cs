using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace SLBr
{
    public partial class WinUIStyleDictionary
    {
        private void TabIcon_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            int TabId = int.Parse(((Image)sender).Tag.ToString());
            foreach (MainWindow _Window in App.Instance.AllWindows)
            {
                BrowserTabItem CurrentTab = _Window.GetBrowserTabWithId(TabId);
                if (CurrentTab != null)
                {
                    CurrentTab.Icon = App.Instance.TabIcon;
                    break;
                }
            }
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            App.Instance.CurrentFocusedWindow().ButtonAction(sender, e);
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            TabItem _TabItem = (TabItem)e.Source;
            if (Mouse.PrimaryDevice.MiddleButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(_TabItem, _TabItem, DragDropEffects.All);
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            MainWindow FocusedWindow = App.Instance.CurrentFocusedWindow();

            int TabItemTargetId = int.Parse(((TabItem)e.Source).Tag.ToString());
            int TargetIndex = FocusedWindow.Tabs.IndexOf(FocusedWindow.GetBrowserTabWithId(TabItemTargetId));
            if (e.Data.GetData(typeof(TabItem)) != null)
            {
                TabItem TabItemSource = (TabItem)e.Data.GetData(typeof(TabItem));

                int TabItemSourceId = int.Parse(TabItemSource.Tag.ToString());

                if (TabItemTargetId != TabItemSourceId)
                {
                    BrowserTabItem BrowserTabItemSource = FocusedWindow.GetBrowserTabWithId(TabItemSourceId);

                    bool IsOriginallySelected = TabItemSource.IsSelected;

                    if (IsOriginallySelected)
                        FocusedWindow.TabsUI.SelectedIndex = TargetIndex;
                    FocusedWindow.Tabs.Remove(BrowserTabItemSource);
                    FocusedWindow.Tabs.Insert(TargetIndex, BrowserTabItemSource);
                    if (IsOriginallySelected)
                        FocusedWindow.TabsUI.SelectedIndex = TargetIndex;
                }
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] FileLoadup = (string[])e.Data.GetData(DataFormats.FileDrop);
                FocusedWindow.NewTab(FileLoadup[0], true, TargetIndex + 1);
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string Url = (string)e.Data.GetData(DataFormats.StringFormat);
                FocusedWindow.NewTab(Utils.FilterUrlForBrowser(Url, App.Instance.GlobalSave.Get("SearchEngine")), true, TargetIndex + 1);
                e.Handled = true;
            }
        }
    }
}
