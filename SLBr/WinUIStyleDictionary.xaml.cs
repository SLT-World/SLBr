using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinUI;

namespace SLBr
{
    public partial class WinUIStyleDictionary
    {
        private void TabIcon_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            BrowserTabItem CurrentTab = (BrowserTabItem)((Image)e.Source).DataContext;
            if (CurrentTab != null)
                CurrentTab.Icon = App.Instance.TabIcon;
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
            BrowserTabItem CurrentTab = (BrowserTabItem)((TabItem)e.Source).DataContext;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                int NewIndex = FocusedWindow.Tabs.IndexOf(CurrentTab) + 1;
                string[] Files = (string[])e.Data.GetData(DataFormats.FileDrop);
                for (int i = 0; i < Files.Length; i++)
                {
                    if (i == 0 && CurrentTab.Type == BrowserTabType.Navigation)
                        CurrentTab.Content?.Address = Files[i];
                    else
                        FocusedWindow.NewTab(Files[i], false, NewIndex, CurrentTab.Content != null ? CurrentTab.Content.Private : false, CurrentTab.TabGroup);
                }
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string Data = (string)e.Data.GetData(DataFormats.StringFormat);
                string Url = Utils.FilterUrlForBrowser(Data, App.Instance.DefaultSearchProvider.SearchUrl);
                if (CurrentTab.Type == BrowserTabType.Navigation)
                    CurrentTab.Content?.Address = Url;
                else
                    FocusedWindow.NewTab(Url, false, FocusedWindow.Tabs.IndexOf(CurrentTab) + 1, CurrentTab.Content != null ? CurrentTab.Content.Private : false, CurrentTab.TabGroup);
                e.Handled = true;
            }
            else if (CurrentTab.Type == BrowserTabType.Group && e.Data.GetData(typeof(TabItem)) != null)
            {
                TabItem TabItemSource = (TabItem)e.Data.GetData(typeof(TabItem));
                BrowserTabItem Tab = (BrowserTabItem)TabItemSource.DataContext;
                Tab.TabGroup = CurrentTab.TabGroup;
                int OldIndex = FocusedWindow.Tabs.IndexOf(Tab);
                int NewIndex = FocusedWindow.Tabs.IndexOf(CurrentTab) + 1;
                if (OldIndex == NewIndex || OldIndex == NewIndex - 1)
                    return;
                if (NewIndex > OldIndex)
                    NewIndex--;
                FocusedWindow.Tabs.Move(OldIndex, NewIndex);
                FocusedWindow.TabsUI.SelectedIndex = NewIndex;
                e.Handled = true;
            }
        }

        private void TabItem_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(TabItem)) || (((FrameworkElement)sender).DataContext is BrowserTabItem Tab && Tab.Type == BrowserTabType.Group))
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
                BrowserTabItem Tab = (BrowserTabItem)SourceTabItem.DataContext;
                int OldIndex = FocusedWindow.Tabs.IndexOf(Tab);

                List<BrowserTabItem> VisibleTabs = FocusedWindow.Tabs.Where(i => i.TabGroup == null || i.Type != BrowserTabType.Navigation || !i.TabGroup.IsCollapsed).ToList();

                int NewIndex = GetInsertIndex(Panel, e.GetPosition(Panel), FocusedWindow);
                BrowserTabItem TargetTab = NewIndex > VisibleTabs.Count - 1 ? VisibleTabs.Last() : VisibleTabs[NewIndex];
                if (NewIndex > FocusedWindow.Tabs.Count - 1)
                    NewIndex = FocusedWindow.Tabs.IndexOf(TargetTab) + 1;
                else if (FocusedWindow.Tabs[NewIndex] != TargetTab)
                    NewIndex = FocusedWindow.Tabs.IndexOf(TargetTab);
                if (OldIndex == NewIndex || OldIndex == NewIndex - 1)
                    return;

                if (NewIndex > OldIndex)
                    NewIndex--;

                FocusedWindow.Tabs.Move(OldIndex, NewIndex);
                FocusedWindow.TabsUI.SelectedIndex = NewIndex;
                if (FocusedWindow.TabGroups.Count != 0)
                {
                    BrowserTabItem LeftTab = null;
                    BrowserTabItem RightTab = null;
                    if (NewIndex > 0)
                        LeftTab = FocusedWindow.Tabs[NewIndex - 1];
                    if (NewIndex < FocusedWindow.Tabs.Count - 1)
                        //WARNING: Functions as intended, do not modify.
                        RightTab = FocusedWindow.Tabs[NewIndex + 1];

                    if (LeftTab != null && RightTab != null && LeftTab.TabGroup != null && (/*LeftTab.Type == BrowserTabType.Group || */LeftTab.TabGroup == RightTab.TabGroup))
                        Tab.TabGroup = LeftTab.TabGroup;
                    else
                        Tab.TabGroup = null;
                }
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
            List<UIElement> VisibleElements = Panel.Children.OfType<UIElement>().Where(i => i.Visibility == Visibility.Visible).ToList();
            TabControl _TabControl = Panel.TemplatedParent as TabControl;
            Border InsertIndicator = _TabControl?.Template.FindName("InsertIndicator", _TabControl) as Border;
            InsertIndicator?.Visibility = Visibility.Visible;

            bool Vertical = _TabControl.TabStripPlacement == Dock.Left || _TabControl.TabStripPlacement == Dock.Right;

            double Offset = 0;
            if (Index < VisibleElements.Count)
            {
                FrameworkElement TargetTab = (FrameworkElement)VisibleElements[Index];
                Point Position = TargetTab.TranslatePoint(new Point(0, 0), Panel);
                Offset = Vertical ? Position.Y : Position.X;
            }
            else if (VisibleElements.Count > 0)
            {
                FrameworkElement LastTab = (FrameworkElement)VisibleElements[^1];
                Point Position = LastTab.TranslatePoint(new Point(LastTab.ActualWidth, LastTab.ActualHeight), Panel);
                Offset = Vertical ? Position.Y : Position.X;
            }
            if (Window.GetWindow(_TabControl) is MainWindow FocusedWindow && FocusedWindow.TabGroups.Count != 0)
            {
                List<BrowserTabItem> VisibleTabs = FocusedWindow.Tabs.Where(i => i.TabGroup == null || i.Type != BrowserTabType.Navigation || !i.TabGroup.IsCollapsed).ToList();
                BrowserTabItem LeftTab = null;
                BrowserTabItem RightTab = null;
                if (Index > 0)
                    LeftTab = Index > VisibleTabs.Count ? VisibleTabs.Last() : VisibleTabs[Index - 1];
                if (Index < VisibleTabs.Count - 1)
                    //WARNING: Functions as intended, do not modify.
                    RightTab = VisibleTabs[Index];

                if (LeftTab != null && RightTab != null && LeftTab.TabGroup != null && (/*LeftTab.Type == BrowserTabType.Group || */LeftTab.TabGroup == RightTab.TabGroup))
                    InsertIndicator.Background = LeftTab.TabGroup.Background;
                else
                    InsertIndicator.Background = (SolidColorBrush)Application.Current.FindResource("IndicatorBrush");
            }
            InsertIndicator.Margin = Vertical ? new Thickness(7.5, Offset + 1.25, 7.5, 0) : new Thickness(Offset + 1.25, 7.5, 0, 7.5);
        }

        private void HideInsertIndicator(Panel Panel)
        {
            TabControl _TabControl = Panel.TemplatedParent as TabControl;
            Border InsertIndicator = _TabControl?.Template.FindName("InsertIndicator", _TabControl) as Border;
            InsertIndicator.Background = (SolidColorBrush)Application.Current.FindResource("IndicatorBrush");
            InsertIndicator?.Visibility = Visibility.Collapsed;
        }

        private int GetInsertIndex(Panel Panel, Point MousePosition, MainWindow CurrentWindow)
        {
            bool Vertical = Panel.TemplatedParent is TabControl _TabControl && (_TabControl.TabStripPlacement == Dock.Left || _TabControl.TabStripPlacement == Dock.Right);
            List<UIElement> VisibleElements = Panel.Children.OfType<UIElement>().Where(i => i.Visibility == Visibility.Visible).ToList();
            int Index = VisibleElements.Count;

            for (int i = 0; i < VisibleElements.Count; i++)
            {
                FrameworkElement TargetTab = (FrameworkElement)VisibleElements[i];
                Point TabPosition = TargetTab.TranslatePoint(new Point(0, 0), Panel);
                double Midpoint = Vertical ? TabPosition.Y + TargetTab.ActualHeight / 2 : TabPosition.X + TargetTab.ActualWidth / 2;
                double MouseAxis = Vertical ? MousePosition.Y : MousePosition.X;
                if (MouseAxis < Midpoint)
                {
                    Index = i;
                    break;
                }
            }
            int MaxIndex = VisibleElements.Count + 1;
            for (int i = 0; i < VisibleElements.Count; i++)
            {
                if (CurrentWindow.Tabs[i]?.Type == BrowserTabType.Add && i > 0)
                {
                    MaxIndex = i;
                    break;
                }
            }
            return Math.Clamp(Index, CurrentWindow.Tabs[0]?.Type == BrowserTabType.Add ? 1 : 0, MaxIndex);
        }

        CancellationTokenSource TabHoverToken;

        private async void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            TabHoverToken?.Cancel();
            TabHoverToken = new CancellationTokenSource();

            FrameworkElement Element = (FrameworkElement)sender;
            BrowserTabItem? Tab = Element.DataContext as BrowserTabItem;
            if (Tab == null || Tab?.Type != BrowserTabType.Navigation) return;

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
            if (Tab == null || Tab?.Type != BrowserTabType.Navigation) return;
            Tab?.ParentWindow.ShowPreview(null);
        }

        private void TabItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TabHoverToken?.Cancel();
            FrameworkElement Element = (FrameworkElement)sender;
            BrowserTabItem? Tab = Element.DataContext as BrowserTabItem;
            if (Tab == null || Tab?.Type != BrowserTabType.Navigation) return;
            Tab?.ParentWindow.ShowPreview(null);
        }

        private void TabGroup_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            FrameworkElement Element = (FrameworkElement)sender;
            BrowserTabItem? Tab = Element.DataContext as BrowserTabItem;
            if (Tab == null || Tab?.Type != BrowserTabType.Group) return;
            e.Handled = true;
            //if (Tab.ParentWindow.Tabs.Any(i => i.Type == BrowserTabType.Navigation && i.TabGroup == Tab.TabGroup))
            //{
            Tab.TabGroup.IsCollapsed = !Tab.TabGroup.IsCollapsed;
            if (Tab.ParentWindow.TabsUI.TabStripPlacement == Dock.Top)
            {
                TabPanel Panel = Tab.ParentWindow.TabsUI?.Template.FindName("HeaderPanel", Tab.ParentWindow.TabsUI) as TabPanel;
                Panel?.SetChildrenMaxWidths(Panel.ActualWidth);
            }
            //}
        }

        private void NewTab_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && Window.GetWindow((FrameworkElement)sender) is MainWindow FocusedWindow)
            {
                if (FocusedWindow.TabsUI.Visibility == Visibility.Visible)
                    FocusedWindow.NewTab(App.Instance.GlobalSave.Get("Homepage"), true, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
            }
        }
    }
}
