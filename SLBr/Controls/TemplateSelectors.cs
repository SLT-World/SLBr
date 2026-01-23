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

        public override DataTemplate SelectTemplate(object Item, DependencyObject Container)
        {
            if (Item is InputField Field)
            {
                return Field.Type switch
                {
                    DialogInputType.Label => LabelTemplate,
                    DialogInputType.Boolean => BoolTemplate,
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

    public class BrowserTabItemStyleSelector : StyleSelector
    {
        public Style NavigationStyle { get; set; }
        public Style AddStyle { get; set; }
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
                }
            }
            return base.SelectStyle(Item, Container);
        }
    }
}
