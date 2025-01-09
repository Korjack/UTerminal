using System;
using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

namespace UTerminal.Behaviors;

public class DocumentTextBindingBehavior : Behavior<TextEditor>
{
    private TextEditor? _textEditor;
    private bool _autoScrollToEnd;

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<DocumentTextBindingBehavior, string>(nameof(Text));
    
    public static readonly DirectProperty<DocumentTextBindingBehavior, bool> AutoScrollToEndProperty =
        AvaloniaProperty.RegisterDirect<DocumentTextBindingBehavior, bool>(
            nameof(AutoScrollToEnd),
            o => o.AutoScrollToEnd,
            (o, v) => o.AutoScrollToEnd = v);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public bool AutoScrollToEnd
    {
        get => _autoScrollToEnd;
        set => SetAndRaise(AutoScrollToEndProperty, ref _autoScrollToEnd, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is TextEditor textEditor)
        {
            _textEditor = textEditor;
            this.GetObservable(TextProperty).Subscribe(TextPropertyChanged);
        }
    }

    private void TextPropertyChanged(string? text)
    {
        if (_textEditor != null && _textEditor.Document != null && text != null)
        {
            _textEditor.Document.Text = text;

            if (AutoScrollToEnd)
            {
                _textEditor.ScrollToEnd();
            }
        }
    }
}