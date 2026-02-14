/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Collections.Specialized;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;

namespace WinUI
{
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof(Panel))]
    public class WinUITabControl : TabControl
    {
        private Panel ItemsHolderPanel = null;

        public WinUITabControl() : base()
        {
            ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                UpdateSelectedItem();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ItemsHolderPanel = GetTemplateChild("PART_ItemsHolder") as Panel;
            UpdateSelectedItem();
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (ItemsHolderPanel == null)
                return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    ItemsHolderPanel.Children.Clear();
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var Item in e.OldItems)
                        {
                            ContentPresenter _ContentPresenter = FindChildContentPresenter(Item);
                            if (_ContentPresenter != null)
                                ItemsHolderPanel.Children.Remove(_ContentPresenter);
                        }
                    }
                    UpdateSelectedItem();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    //Replace not implemented yet
                    break;
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateSelectedItem();
        }

        private void UpdateSelectedItem()
        {
            if (ItemsHolderPanel == null)
                return;
            TabItem Item = GetSelectedTabItem();
            if (Item != null)
                CreateChildContentPresenter(Item);
            foreach (ContentPresenter Child in ItemsHolderPanel.Children)
                Child.Visibility = ((Child.Tag as TabItem).IsSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        private ContentPresenter CreateChildContentPresenter(object Item)
        {
            if (Item == null)
                return null;
            ContentPresenter _ContentPresenter = FindChildContentPresenter(Item);
            if (_ContentPresenter != null)
                return _ContentPresenter;

            _ContentPresenter = new ContentPresenter();
            _ContentPresenter.Content = (Item is TabItem) ? (Item as TabItem).Content : Item;
            _ContentPresenter.ContentTemplate = SelectedContentTemplate;
            _ContentPresenter.ContentTemplateSelector = SelectedContentTemplateSelector;
            _ContentPresenter.ContentStringFormat = SelectedContentStringFormat;
            _ContentPresenter.Visibility = Visibility.Collapsed;
            _ContentPresenter.Tag = (Item is TabItem) ? Item : (ItemContainerGenerator.ContainerFromItem(Item));
            ItemsHolderPanel.Children.Add(_ContentPresenter);
            return _ContentPresenter;
        }

        private ContentPresenter FindChildContentPresenter(object Data)
        {
            if (ItemsHolderPanel == null)
                return null;
            if (Data is TabItem)
            {
                Data = (Data as TabItem).Content;
                foreach (ContentPresenter _ContentPresenter in ItemsHolderPanel.Children)
                {
                    if (_ContentPresenter.Content == Data)
                        return _ContentPresenter;
                }
            }
            return null;
        }

        protected TabItem GetSelectedTabItem()
        {
            TabItem _SelectedItem = SelectedItem as TabItem;
            /*TabItem Item = _SelectedItem as TabItem;
            if (Item == null)
                Item = ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as TabItem;*/
            return _SelectedItem;
        }
    }
}
