/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SLBr.Controls
{
    public class InputFieldTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate LabelTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate ColorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object Item, DependencyObject Container)
        {
            if (Item is InputField Field)
            {
                return Field.Type switch
                {
                    DialogInputType.Label => LabelTemplate,
                    DialogInputType.Boolean => BoolTemplate,
                    DialogInputType.Color => ColorTemplate,
                    _ => TextTemplate
                };
            }
            return base.SelectTemplate(Item, Container);
        }
    }

    public class ProfileTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UserTemplate { get; set; }
        public DataTemplate GuestTemplate { get; set; }
        public DataTemplate AddTemplate { get; set; }

        public override DataTemplate SelectTemplate(object Item, DependencyObject Container)
        {
            if (Item is Profile _Profile)
            {
                switch (_Profile.Type)
                {
                    case ProfileType.User:
                        return UserTemplate;
                    case ProfileType.System:
                        if (_Profile.Name == "Guest")
                            return GuestTemplate;
                        else
                            return AddTemplate;
                }
            }
            return base.SelectTemplate(Item, Container);
        }
    }

    public class FavouriteTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UrlTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }

        public override DataTemplate SelectTemplate(object Item, DependencyObject Container)
        {
            if (Item is Favourite _Favourite)
            {
                switch (_Favourite.Type)
                {
                    case "url":
                        return UrlTemplate;
                    case "folder":
                        return FolderTemplate;
                }
            }
            return base.SelectTemplate(Item, Container);
        }
    }

    public class FavouriteStyleSelector : StyleSelector
    {
        public Style UrlStyle { get; set; }
        public Style FolderStyle { get; set; }

        public override Style SelectStyle(object Item, DependencyObject Container)
        {
            if (Item is Favourite _Favourite)
            {
                switch (_Favourite.Type)
                {
                    case "url":
                        return UrlStyle;
                    case "folder":
                        return FolderStyle;
                }
            }
            return base.SelectStyle(Item, Container);
        }
    }

    public class BrowserTabItemStyleSelector : StyleSelector
    {
        public Style NavigationStyle { get; set; }
        public Style AddStyle { get; set; }
        public Style GroupStyle { get; set; }
        public override Style SelectStyle(object Item, DependencyObject Container)
        {
            if (Item is BrowserTabItem _TabItem)
            {
                switch (_TabItem.Type)
                {
                    case BrowserTabType.Navigation:
                        return NavigationStyle;
                    case BrowserTabType.Add:
                        return AddStyle;
                    case BrowserTabType.Group:
                        return GroupStyle;
                }
            }
            return base.SelectStyle(Item, Container);
        }
    }
}
