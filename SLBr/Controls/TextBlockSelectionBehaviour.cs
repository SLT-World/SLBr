/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SLBr.Controls
{
    public static class TextBlockSelectionBehaviour
    {
        private static bool CommandHandlersRegistered;

        private static readonly DependencyProperty AttachedEditorProperty = DependencyProperty.RegisterAttached("AttachedEditor", typeof(object), typeof(TextBlockSelectionBehaviour), new PropertyMetadata(null));
        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(TextBlockSelectionBehaviour), new PropertyMetadata(false, OnEnableChanged));
        public static readonly DependencyProperty HasContextMenuProperty = DependencyProperty.RegisterAttached("HasContextMenu", typeof(bool), typeof(TextBlockSelectionBehaviour), new PropertyMetadata(false, OnHasContextMenuChanged));

        public static bool GetEnable(DependencyObject _Object) =>
            (bool)_Object.GetValue(EnableProperty);

        public static void SetEnable(DependencyObject _Object, bool Value) =>
            _Object.SetValue(EnableProperty, Value);

        public static bool GetHasContextMenu(DependencyObject _Object) =>
            (bool)_Object.GetValue(HasContextMenuProperty);

        public static void SetHasContextMenu(DependencyObject _Object, bool Value) =>
            _Object.SetValue(HasContextMenuProperty, Value);

        private static void OnEnableChanged(DependencyObject _Object, DependencyPropertyChangedEventArgs e)
        {
            if (_Object is not TextBlock _TextBlock)
                return;

            if ((bool)e.NewValue)
            {
                /*ValueSource ValueSource = DependencyPropertyHelper.GetValueSource(_TextBlock, TextBlock.FontFamilyProperty);
                if (ValueSource.BaseValueSource == BaseValueSource.Local || ValueSource.BaseValueSource == BaseValueSource.Style)
                {
                    _TextBlock.SetValue(EnableProperty, false);
                    return;
                }*/
                if (App.Instance.LiteMode)
                {
                    _TextBlock.SetValue(EnableProperty, false);
                    return;
                }
                if (!CommandHandlersRegistered)
                {
                    TextEditorWrapper.RegisterCommandHandlers(typeof(TextBlock), true, true, true);
                    CommandHandlersRegistered = true;
                }
                _TextBlock.Focusable = true;
                _TextBlock.FocusVisualStyle = null;
                _TextBlock.Loaded += TextBlock_Loaded;
                _TextBlock.Unloaded += TextBlock_Unloaded;
            }
            else
            {
                _TextBlock.Loaded -= TextBlock_Loaded;
                _TextBlock.Unloaded -= TextBlock_Unloaded;
            }
        }

        private static void TextBlock_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBlock _TextBlock)
                return;
            _TextBlock.Loaded -= TextBlock_Loaded;
            _TextBlock.ContextMenuOpening -= TextBlock_ContextMenuOpening;
            _TextBlock.ContextMenu = null;
        }

        public static string GetSelectedText(TextBlock _TextBlock)
        {
            if (_TextBlock.GetValue(AttachedEditorProperty) is TextEditorWrapper Wrapper)
                return Wrapper.SelectedText;
            return string.Empty;
        }

        private static void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock _TextBlock)
            {
                _TextBlock.Loaded -= TextBlock_Loaded;

                //WARNING: Do not remove AttachedEditorProperty, used for TextEditor creation.
                _TextBlock.SetValue(AttachedEditorProperty, TextEditorWrapper.CreateFor(_TextBlock));
                if (GetHasContextMenu(_TextBlock))
                    ApplyContextMenu(_TextBlock);
            }
        }

        private static void OnHasContextMenuChanged(DependencyObject _Object, DependencyPropertyChangedEventArgs e)
        {
            if (_Object is not TextBlock _TextBlock)
                return;
            if (!_TextBlock.IsLoaded)
                return;

            if ((bool)e.NewValue)
            {
                if (_TextBlock.ContextMenu == null)
                    ApplyContextMenu(_TextBlock);
            }
            else
            {

                if (_TextBlock.ContextMenu != null)
                {
                    _TextBlock.ContextMenuOpening -= TextBlock_ContextMenuOpening;
                    _TextBlock.ContextMenu = null;
                }
            }
        }

        private static void ApplyContextMenu(TextBlock _TextBlock)
        {
            ContextMenu Menu = new()
            {
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                FontStyle = FontStyles.Normal,
                FontFamily = new FontFamily("Segoe UI")
            };
            Menu.Items.Add(new MenuItem { Header = "Copy", Command = ApplicationCommands.Copy });
            Menu.Items.Add(new Separator());
            Menu.Items.Add(new MenuItem { Header = "Select all", Command = ApplicationCommands.SelectAll });

            _TextBlock.ContextMenu = Menu;
            _TextBlock.ContextMenuOpening += TextBlock_ContextMenuOpening;
        }

        private static void TextBlock_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is TextBlock _TextBlock)
            {
                if (string.IsNullOrEmpty(GetSelectedText(_TextBlock)))
                    e.Handled = true;
            }
        }
    }

    class TextEditorWrapper
    {
        private static readonly Type TextEditorType = Type.GetType("System.Windows.Documents.TextEditor, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        private static readonly PropertyInfo IsReadOnlyProperty = TextEditorType.GetProperty("IsReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo TextViewProperty = TextEditorType.GetProperty("TextView", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo RegisterMethod = TextEditorType.GetMethod("RegisterCommandHandlers", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(bool), typeof(bool), typeof(bool) }, null);

        private static readonly Type TextContainerType = Type.GetType("System.Windows.Documents.ITextContainer, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        private static readonly PropertyInfo TextContainerTextViewProperty = TextContainerType.GetProperty("TextView");
        private static readonly PropertyInfo TextContainerProperty = typeof(TextBlock).GetProperty("TextContainer", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo SelectionProperty = TextEditorType.GetProperty("Selection", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly PropertyInfo TextProperty = Type.GetType("System.Windows.Documents.ITextRange, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")?.GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);

        public static void RegisterCommandHandlers(Type ControlType, bool AcceptsRichContent, bool ReadOnly, bool RegisterEventListeners)
        {
            RegisterMethod.Invoke(null, [ControlType, AcceptsRichContent, ReadOnly, RegisterEventListeners]);
        }

        public static TextEditorWrapper CreateFor(TextBlock _TextBlock)
        {
            object? TextContainer = TextContainerProperty.GetValue(_TextBlock);

            TextEditorWrapper _Editor = new(TextContainer, _TextBlock, false);
            IsReadOnlyProperty.SetValue(_Editor.Editor, true);
            TextViewProperty.SetValue(_Editor.Editor, TextContainerTextViewProperty.GetValue(TextContainer));

            return _Editor;
        }

        public string SelectedText
        {
            get
            {
                if (Editor == null || SelectionProperty == null || TextProperty == null) return string.Empty;
                object? SelectionInstance = SelectionProperty.GetValue(Editor);
                return SelectionInstance != null ? (TextProperty.GetValue(SelectionInstance) as string ?? string.Empty) : string.Empty;
            }
        }

        private readonly object Editor;

        public TextEditorWrapper(object TextContainer, FrameworkElement UIScope, bool UndoEnabled)
        {
            Editor = Activator.CreateInstance(TextEditorType, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, [TextContainer, UIScope, UndoEnabled], null);
        }
    }
}
