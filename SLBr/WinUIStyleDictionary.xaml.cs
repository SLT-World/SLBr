using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SLBr
{
    public partial class WinUIStyleDictionary
    {
        private void TabIcon_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            int TabId = int.Parse(((Image)sender).Tag.ToString()!);
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
            //Interferes with close button
            //if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed || Mouse.PrimaryDevice.MiddleButton == MouseButtonState.Pressed)
            if (Mouse.PrimaryDevice.MiddleButton == MouseButtonState.Pressed)
            {
                TabItem _TabItem = (TabItem)e.Source;
                DragDrop.DoDragDrop(_TabItem, _TabItem, DragDropEffects.All);
            }
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            MainWindow FocusedWindow = App.Instance.CurrentFocusedWindow();
            BrowserTabItem CurrentTab = FocusedWindow.GetBrowserTabWithId(int.Parse(((TabItem)e.Source).Tag.ToString()!));
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                int TargetIndex = FocusedWindow.Tabs.IndexOf(CurrentTab);
                string[] Files = (string[])e.Data.GetData(DataFormats.FileDrop);
                for (int i = 0; i < Files.Length; i++)
                {
                    if (i == 0)
                        CurrentTab.Content?.Address = Files[i];
                    else
                        FocusedWindow.NewTab(Files[i], false, TargetIndex + 1);
                }
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string Data = (string)e.Data.GetData(DataFormats.StringFormat);
                CurrentTab.Content?.Address = Utils.FilterUrlForBrowser(Data, App.Instance.DefaultSearchProvider.SearchUrl);
                e.Handled = true;
            }
        }

        private void TabItem_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(TabItem)))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void HeaderPanel_DragOver(object sender, DragEventArgs e)
        {
            Panel Panel = (Panel)sender;

            if (e.Data.GetDataPresent(typeof(TabItem)))
            {
                ShowInsertIndicator(Panel, GetInsertIndex(Panel, e.GetPosition(Panel), App.Instance.CurrentFocusedWindow()));
                e.Effects = DragDropEffects.Move;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                ShowInsertIndicator(Panel, GetInsertIndex(Panel, e.GetPosition(Panel), App.Instance.CurrentFocusedWindow()));
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                HideInsertIndicator(Panel);
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void HeaderPanel_DragLeave(object sender, DragEventArgs e) =>
            HideInsertIndicator((Panel)sender);

        private void HeaderPanel_Drop(object sender, DragEventArgs e)
        {
            Panel Panel = (Panel)sender;
            MainWindow FocusedWindow = App.Instance.CurrentFocusedWindow();
            HideInsertIndicator(Panel);
            if (e.Data.GetDataPresent(typeof(TabItem)))
            {
                TabItem SourceTabItem = (TabItem)e.Data.GetData(typeof(TabItem))!;

                int OldIndex = FocusedWindow.Tabs.IndexOf(FocusedWindow.GetBrowserTabWithId(int.Parse(SourceTabItem.Tag.ToString()!)));
                int NewIndex = GetInsertIndex(Panel, e.GetPosition(Panel), FocusedWindow);

                if (OldIndex == NewIndex || OldIndex == NewIndex - 1)
                    return;

                if (NewIndex > OldIndex)
                    NewIndex--;

                FocusedWindow.Tabs.Move(OldIndex, NewIndex);
                FocusedWindow.TabsUI.SelectedIndex = NewIndex;
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] Files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string File in Files)
                    FocusedWindow.NewTab(File, true, GetInsertIndex(Panel, e.GetPosition(Panel), FocusedWindow));
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string Data = (string)e.Data.GetData(DataFormats.StringFormat);
                FocusedWindow.NewTab(Utils.FilterUrlForBrowser(Data, App.Instance.DefaultSearchProvider.SearchUrl), true, GetInsertIndex(Panel, e.GetPosition(Panel), FocusedWindow));
                e.Handled = true;
            }
        }

        private void ShowInsertIndicator(Panel Panel, int Index)
        {
            TabControl _TabControl = Panel.TemplatedParent as TabControl;
            Border InsertIndicator = _TabControl?.Template.FindName("InsertIndicator", _TabControl) as Border;
            InsertIndicator?.Visibility = Visibility.Visible;

            bool Vertical = _TabControl.TabStripPlacement == Dock.Left || _TabControl.TabStripPlacement == Dock.Right;

            double Offset = 0;
            if (Index < Panel.Children.Count)
            {
                FrameworkElement TargetTab = (FrameworkElement)Panel.Children[Index];
                Point Position = TargetTab.TranslatePoint(new Point(0, 0), Panel);
                Offset = Vertical ? Position.Y : Position.X;
            }
            else if (Panel.Children.Count > 0)
            {
                FrameworkElement LastTab = (FrameworkElement)Panel.Children[^1];
                Point Position = LastTab.TranslatePoint(new Point(LastTab.ActualWidth, 0), Panel);
                Offset = Vertical ? Position.Y : Position.X;
            }
            InsertIndicator.Margin = Vertical ? new Thickness(7.5, Offset + 1.25, 7.5, 0) : new Thickness(Offset + 1.25, 7.5, 0, 7.5);
        }

        private void HideInsertIndicator(Panel Panel)
        {
            TabControl _TabControl = Panel.TemplatedParent as TabControl;
            Border InsertIndicator = _TabControl?.Template.FindName("InsertIndicator", _TabControl) as Border;
            InsertIndicator?.Visibility = Visibility.Collapsed;
        }

        private int GetInsertIndex(Panel Panel, Point MousePosition, MainWindow CurrentWindow)
        {
            bool Vertical = Panel.TemplatedParent is TabControl _TabControl && (_TabControl.TabStripPlacement == Dock.Left || _TabControl.TabStripPlacement == Dock.Right);
            int Index = Panel.Children.Count;
            for (int i = 0; i < Panel.Children.Count; i++)
            {
                FrameworkElement TargetTab = (FrameworkElement)Panel.Children[i];
                Point TabPosition = TargetTab.TranslatePoint(new Point(0, 0), Panel);
                double Midpoint = Vertical ? TabPosition.Y + TargetTab.ActualHeight / 2 : TabPosition.X + TargetTab.ActualWidth / 2;
                double MouseAxis = Vertical ? MousePosition.Y : MousePosition.X;
                if (MouseAxis < Midpoint)
                {
                    Index = i;
                    break;
                }
            }
            int MaxIndex = Panel.Children.Count;
            for (int i = 0; i < Panel.Children.Count; i++)
            {
                if (CurrentWindow.Tabs[i]?.ParentWindow == null)
                {
                    MaxIndex = i;
                    break;
                }
            }
            return Math.Clamp(Index, CurrentWindow.Tabs[0]?.ParentWindow != null ? 0 : 1, MaxIndex);
        }

        CancellationTokenSource TabHoverToken;

        private async void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            TabHoverToken?.Cancel();
            TabHoverToken = new CancellationTokenSource();

            FrameworkElement Element = (FrameworkElement)sender;
            BrowserTabItem? Tab = Element.DataContext as BrowserTabItem;
            if (Tab == null || Tab?.ParentWindow == null) return;

            try
            {
                await Task.Delay(400, TabHoverToken.Token);
                Tab.ParentWindow.ShowPreview(Tab, Element);
            }
            catch { }
        }

        private void TabItem_MouseLeave(object sender, MouseEventArgs e)
        {
            TabHoverToken?.Cancel();
            FrameworkElement Element = (FrameworkElement)sender;
            BrowserTabItem? Tab = Element.DataContext as BrowserTabItem;
            if (Tab == null || Tab?.ParentWindow == null) return;
            Tab?.ParentWindow.ShowPreview(null);
        }

        private void TabItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TabHoverToken?.Cancel();
            FrameworkElement Element = (FrameworkElement)sender;
            BrowserTabItem? Tab = Element.DataContext as BrowserTabItem;
            if (Tab == null || Tab?.ParentWindow == null) return;
            Tab?.ParentWindow.ShowPreview(null);
        }
    }
}
