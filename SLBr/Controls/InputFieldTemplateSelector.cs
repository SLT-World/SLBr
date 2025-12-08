using System.Windows;
using System.Windows.Controls;

namespace SLBr.Controls
{
    public class InputFieldTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate LabelTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is InputField Field)
            {
                return Field.Type switch
                {
                    DialogInputType.Label => LabelTemplate,
                    DialogInputType.Boolean => BoolTemplate,
                    _ => TextTemplate
                };
            }
            return base.SelectTemplate(item, container);
        }
    }
}
