/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinUI;

namespace SLBr
{
    public partial class WinUIStyleDictionary
    {
        public WinUIStyleDictionary()
        {
            InitializeComponent();
            if (TabHoverTimer == null)
            {
                TabHoverTimer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(400)
                };
                TabHoverTimer.Tick += TabHoverTimer_Tick;
            }
        }

        private void TabIcon_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Image? Image = sender as Image;
            if (Image?.DataContext is BrowserTabItem CurrentTab)
                CurrentTab.Icon = App.Instance.TabIcon;
        }

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject Object)
                App.Instance.GetWindow(Object).ButtonAction(sender, e);
        }

        private object? TabDragSender = null;
        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            /*if (e.OriginalSource is DependencyObject OriginalSource && Utils.FindAncestorOfType<Button>(OriginalSource) != null)
                return;*/
            //e.LeftButton == MouseButtonState.Pressed || 
            if (e.MiddleButton == MouseButtonState.Pressed)
                TabDragSender = sender;
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (TabDragSender != sender)
                return;
            TabItem SourceTabItem = (TabItem)sender;
            BrowserTabItem Tab = (BrowserTabItem)SourceTabItem.DataContext;
            if (Tab.Type == BrowserTabType.Group)
            {
                if (Tab.TabGroup.IsCollapsed)
                {
                    if (App.Instance.GetWindow(SourceTabItem).GetTab().TabGroup == Tab.TabGroup)
                        return;
                }
                else
                    return;
            }
            //Interferes with close button
            //if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed || Mouse.PrimaryDevice.MiddleButton == MouseButtonState.Pressed)
            if (Mouse.PrimaryDevice.MiddleButton == MouseButtonState.Pressed)
            {
                TabItem _TabItem = (TabItem)e.Source;
                DragDrop.DoDragDrop(_TabItem, _TabItem, DragDropEffects.All);
            }
        }
        private void TabItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //e.ChangedButton == MouseButton.Left || 
            if (e.ChangedButton == MouseButton.Middle)
                TabDragSender = null;
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            MainWindow FocusedWindow = App.Instance.GetWindow((DependencyObject)sender);
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
                        FocusedWindow.NewTab(Files[i], false, NewIndex, CurrentTab.Content != null && CurrentTab.Content.Private, CurrentTab.TabGroup);
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
                    FocusedWindow.NewTab(Url, false, FocusedWindow.Tabs.IndexOf(CurrentTab) + 1, CurrentTab.Content != null && CurrentTab.Content.Private, CurrentTab.TabGroup);
                e.Handled = true;
            }
            else if (CurrentTab.Type == BrowserTabType.Group && e.Data.GetData(typeof(TabItem)) != null)
            {
                TabItem TabItemSource = (TabItem)e.Data.GetData(typeof(TabItem));
                BrowserTabItem Tab = (BrowserTabItem)TabItemSource.DataContext;
                if (Tab.Type == BrowserTabType.Navigation)
                {
                    Tab.TabGroup = CurrentTab.TabGroup;
                    int OldIndex = FocusedWindow.Tabs.IndexOf(Tab);
                    int NewIndex = FocusedWindow.Tabs.IndexOf(CurrentTab) + 1;
                    if (OldIndex == NewIndex || OldIndex == NewIndex - 1)
                        return;
                    if (NewIndex > OldIndex)
                        NewIndex--;
                    FocusedWindow.Tabs.Move(OldIndex, NewIndex);
                    FocusedWindow.TabsUI.SelectedIndex = NewIndex;
                }
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
                (int NewIndex, int RealIndex) = GetInsertIndex(Panel, e.GetPosition(Panel), App.Instance.GetWindow(Panel));
                ShowInsertIndicator(Panel, NewIndex, RealIndex);
                e.Effects = DragDropEffects.Move;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                (int NewIndex, int RealIndex) = GetInsertIndex(Panel, e.GetPosition(Panel), App.Instance.GetWindow(Panel));
                ShowInsertIndicator(Panel, NewIndex, RealIndex);
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
            MainWindow FocusedWindow = App.Instance.GetWindow(Panel);
            HideInsertIndicator(Panel);
            if (e.Data.GetDataPresent(typeof(TabItem)))
            {
                TabItem SourceTabItem = (TabItem)e.Data.GetData(typeof(TabItem))!;
                BrowserTabItem Tab = (BrowserTabItem)SourceTabItem.DataContext;
                int OldIndex = FocusedWindow.Tabs.IndexOf(Tab);

                (_, int RealIndex) = GetInsertIndex(Panel, e.GetPosition(Panel), FocusedWindow);
                if (OldIndex == RealIndex || OldIndex == RealIndex - 1)
                    return;

                if (Tab.Type == BrowserTabType.Group)
                {
                    if (Tab.TabGroup.IsCollapsed && FocusedWindow.TabGroups.Count > 1)
                    {
                        BrowserTabItem LeftTab = null;
                        BrowserTabItem RightTab = null;
                        if (RealIndex > 0)
                        {
                            LeftTab = FocusedWindow.Tabs[RealIndex - 1];
                            if (RealIndex < FocusedWindow.Tabs.Count - 1)
                                RightTab = FocusedWindow.Tabs[RealIndex];
                        }
                        if (LeftTab == null || RightTab == null || RightTab.TabGroup == null || LeftTab.TabGroup == null || RightTab.TabGroup == Tab.TabGroup)
                            MoveCollapsedGroup(Tab, RealIndex, FocusedWindow);
                    }
                }
                else
                {
                    if (RealIndex > OldIndex)
                        RealIndex--;
                    FocusedWindow.Tabs.Move(OldIndex, RealIndex);
                    FocusedWindow.TabsUI.SelectedIndex = RealIndex;
                    if (FocusedWindow.TabGroups.Count != 0)
                    {
                        BrowserTabItem LeftTab = null;
                        BrowserTabItem RightTab = null;
                        if (RealIndex > 0)
                            LeftTab = FocusedWindow.Tabs[RealIndex - 1];
                        if (RealIndex < FocusedWindow.Tabs.Count - 1)
                            RightTab = FocusedWindow.Tabs[RealIndex + 1];

                        if (LeftTab != null && RightTab != null && LeftTab.TabGroup != null && (/*LeftTab.Type == BrowserTabType.Group || */LeftTab.TabGroup == RightTab.TabGroup))
                            Tab.TabGroup = LeftTab.TabGroup;
                        else
                            Tab.TabGroup = null;
                    }
                }
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] Files = (string[])e.Data.GetData(DataFormats.FileDrop);

                (_, int RealIndex) = GetInsertIndex(Panel, e.GetPosition(Panel), FocusedWindow);

                for (int i = 0; i < Files.Length; i++)
                    FocusedWindow.NewTab(Files[i], i == Files.Length - 1, RealIndex);
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string Data = (string)e.Data.GetData(DataFormats.StringFormat);

                (_, int RealIndex) = GetInsertIndex(Panel, e.GetPosition(Panel), FocusedWindow);

                FocusedWindow.NewTab(Utils.FilterUrlForBrowser(Data, App.Instance.DefaultSearchProvider.SearchUrl), true, RealIndex);
                e.Handled = true;
            }
        }

        private void ShowInsertIndicator(Panel Panel, int VisibleIndex, int RealIndex)
        {
            List<UIElement> VisibleElements = Panel.Children.OfType<UIElement>().Where(i => i.Visibility == Visibility.Visible).ToList();
            TabControl _TabControl = Panel.TemplatedParent as TabControl;
            Border InsertIndicator = _TabControl?.Template.FindName("InsertIndicator", _TabControl) as Border;
            InsertIndicator?.Visibility = Visibility.Visible;

            bool Vertical = _TabControl.TabStripPlacement == Dock.Left || _TabControl.TabStripPlacement == Dock.Right;

            double Offset = 0;
            if (VisibleIndex < VisibleElements.Count)
            {
                FrameworkElement TargetTab = (FrameworkElement)VisibleElements[VisibleIndex];
                Point Position = TargetTab.TranslatePoint(new Point(0, 0), Panel);
                Offset = Vertical ? Position.Y : Position.X;
            }
            else if (VisibleElements.Count > 0)
            {
                FrameworkElement LastTab = (FrameworkElement)VisibleElements[^1];
                Point Position = LastTab.TranslatePoint(new Point(LastTab.ActualWidth, LastTab.ActualHeight), Panel);
                Offset = Vertical ? Position.Y : Position.X;
            }
            MainWindow FocusedWindow = App.Instance.GetWindow(Panel);
            if (FocusedWindow.TabGroups.Count != 0)
            {
                /*List<BrowserTabItem> VisibleTabs = FocusedWindow.Tabs.Where(i => i == FocusedWindow.TabsUI.SelectedItem || i.TabGroup == null || i.Type != BrowserTabType.Navigation || !i.TabGroup.IsCollapsed).ToList();
                BrowserTabItem LeftTab = null;
                BrowserTabItem RightTab = null;
                if (VisibleIndex > 0)
                    LeftTab = VisibleIndex > VisibleTabs.Count ? VisibleTabs.Last() : VisibleTabs[VisibleIndex - 1];
                if (VisibleIndex < VisibleTabs.Count - 1)
                    //WARNING: Functions as intended, do not modify.
                    RightTab = VisibleTabs[VisibleIndex];*/

                BrowserTabItem LeftTab = null;
                BrowserTabItem RightTab = null;
                if (RealIndex > 0)
                    LeftTab = VisibleIndex > FocusedWindow.Tabs.Count ? FocusedWindow.Tabs.Last() : FocusedWindow.Tabs[RealIndex - 1];
                if (RealIndex < FocusedWindow.Tabs.Count - 1)
                    RightTab = FocusedWindow.Tabs[RealIndex];

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

        private (int, int) GetInsertIndex(Panel Panel, Point MousePosition, MainWindow CurrentWindow)
        {
            bool Vertical = Panel.TemplatedParent is TabControl _TabControl && (_TabControl.TabStripPlacement == Dock.Left || _TabControl.TabStripPlacement == Dock.Right);
            List<UIElement> VisibleElements = Panel.Children.OfType<UIElement>().Where(i => i.Visibility == Visibility.Visible).ToList();
            int Index = VisibleElements.Count;

            for (int i = 0; i < VisibleElements.Count; i++)
            {
                FrameworkElement VisibleTargetTab = (FrameworkElement)VisibleElements[i];
                Point TabPosition = VisibleTargetTab.TranslatePoint(new Point(0, 0), Panel);
                double Midpoint = Vertical ? TabPosition.Y + VisibleTargetTab.ActualHeight / 2 : TabPosition.X + VisibleTargetTab.ActualWidth / 2;
                double MouseAxis = Vertical ? MousePosition.Y : MousePosition.X;
                if (MouseAxis < Midpoint)
                {
                    Index = i;
                    break;
                }
            }
            int MaxIndex = VisibleElements.Count + 1;
            bool LastAdd = false;
            List<BrowserTabItem> VisibleTabs = CurrentWindow.Tabs.Where(i => i == CurrentWindow.TabsUI.SelectedItem || i.TabGroup == null || i.Type != BrowserTabType.Navigation || !i.TabGroup.IsCollapsed).ToList();
            /*for (int i = VisibleElements.Count - 1; i >= 0; i--)
            {
                if (VisibleTabs[i]?.Type == BrowserTabType.Add)
                {
                    LastAdd = true;
                    MaxIndex = i;
                    break;
                }
            }*/
            if (VisibleTabs[^1]?.Type == BrowserTabType.Add)
            {
                LastAdd = true;
                MaxIndex = VisibleTabs.Count - 1;
            }

            //int VisibleIndex = Math.Clamp(Index, CurrentWindow.Tabs[0]?.Type == BrowserTabType.Add ? 1 : 0, MaxIndex);
            int VisibleIndex = Math.Clamp(Index, 0, MaxIndex);
            int RealIndex = VisibleIndex;
            bool Surpass = RealIndex > VisibleTabs.Count - 1;
            BrowserTabItem TargetTab = Surpass ? VisibleTabs.Last() : VisibleTabs[RealIndex];
            if (RealIndex > CurrentWindow.Tabs.Count - 1)
                RealIndex = CurrentWindow.Tabs.IndexOf(TargetTab) + 1;
            else if (CurrentWindow.Tabs[RealIndex] != TargetTab)
                RealIndex = CurrentWindow.Tabs.IndexOf(TargetTab);
            if (Surpass && !LastAdd)
                RealIndex++;
            return (VisibleIndex, RealIndex);
        }

        private static (int Start, int Count) GetGroupRange(MainWindow CurrentWindow, TabGroup Group)
        {
            int Start = -1;
            int Count = 0;
            for (int i = 0; i < CurrentWindow.Tabs.Count; i++)
            {
                BrowserTabItem Tab = CurrentWindow.Tabs[i];
                if (Tab.TabGroup == Group)
                {
                    if (Start == -1)
                        Start = i;
                    Count++;
                }
                else if (Start != -1)
                    break;
            }
            return (Start, Count);
        }

        private static void MoveCollapsedGroup(BrowserTabItem GroupHeader, int TargetIndex, MainWindow CurrentWindow)
        {
            var (Start, Count) = GetGroupRange(CurrentWindow, GroupHeader.TabGroup);
            if (Start < 0 || Count == 0)
                return;
            if (TargetIndex >= Start && TargetIndex <= Start + Count)
                return;
            List<BrowserTabItem> Block = CurrentWindow.Tabs.Skip(Start).Take(Count).ToList();

            for (int i = 0; i < Count; i++)
                CurrentWindow.Tabs.RemoveAt(Start);
            if (TargetIndex > Start)
                TargetIndex -= Count;

            TargetIndex = Math.Clamp(TargetIndex, 0, CurrentWindow.Tabs.Count);
            for (int i = 0; i < Block.Count; i++)
                CurrentWindow.Tabs.Insert(TargetIndex + i, Block[i]);
            //window.TabsUI.SelectedItem = GroupHeader;
        }

        private static DispatcherTimer TabHoverTimer;
        private static FrameworkElement? CurrentHoverElement;
        private static BrowserTabItem? CurrentHoverTab;

        private void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            TabHoverTimer.Stop();
            if (sender is not FrameworkElement Element || Element.DataContext is not BrowserTabItem Tab || Tab.Type != BrowserTabType.Navigation)
                return;
            CurrentHoverElement = Element;
            CurrentHoverTab = Tab;
            TabHoverTimer.Start();
        }

        private void TabItem_MouseLeave(object sender, MouseEventArgs e)
        {
            ResetHoverState();
        }

        private void TabItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ResetHoverState();
        }

        private static void TabHoverTimer_Tick(object? sender, EventArgs e)
        {
            TabHoverTimer.Stop();
            if (CurrentHoverTab != null && CurrentHoverElement != null)
                CurrentHoverTab.ParentWindow.ShowPreview(CurrentHoverTab, CurrentHoverElement);
        }

        private static void ResetHoverState()
        {
            TabHoverTimer?.Stop();
            CurrentHoverTab?.ParentWindow.ShowPreview(null);
            CurrentHoverElement = null;
            CurrentHoverTab = null;
        }

        private void TabGroup_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (sender is not FrameworkElement Element || Element.DataContext is not BrowserTabItem Tab || Tab?.Type != BrowserTabType.Group)
                return;
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
            if (e.ChangedButton == MouseButton.Left && sender is DependencyObject Object)
            {
                MainWindow FocusedWindow = App.Instance.GetWindow(Object);
                if (FocusedWindow.TabsUI.Visibility == Visibility.Visible)
                    FocusedWindow.NewTab(App.Instance.GlobalSave.Get("Homepage"), true, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
            }
        }

        private void Hyperlink_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is DependencyObject Object)
                App.Instance.GetWindow(Object).GetTab().Content?.WebView_StatusMessage(null, ((Hyperlink)sender).NavigateUri.AbsoluteUri);
        }

        private void Hyperlink_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is DependencyObject Object)
                App.Instance.GetWindow(Object).GetTab().Content?.WebView_StatusMessage(null, "");
        }
    }
}
