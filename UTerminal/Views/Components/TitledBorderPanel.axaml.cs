using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;

namespace UTerminal.Views.Components;

public class TitledBorderPanel : TemplatedControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<TitledBorderPanel, string>(nameof(Title), "Title");

    public static readonly StyledProperty<object> ContentProperty =
        ContentControl.ContentProperty.AddOwner<TitledBorderPanel>();

    public static readonly StyledProperty<double> TitleFontSizeProperty =
        AvaloniaProperty.Register<TitledBorderPanel, double>(nameof(TitleFontSize), 10);

    public static readonly StyledProperty<IBrush> TitleBackgroundProperty =
        AvaloniaProperty.Register<TitledBorderPanel, IBrush>(nameof(TitleBackground), Brushes.White);

    public static readonly StyledProperty<IBrush> TitleForegroundProperty =
        AvaloniaProperty.Register<TitledBorderPanel, IBrush>(nameof(TitleForeground), Brushes.Black);

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<TitledBorderPanel>();

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    [Content]
    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public double TitleFontSize
    {
        get => GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public IBrush TitleBackground
    {
        get => GetValue(TitleBackgroundProperty);
        set => SetValue(TitleBackgroundProperty, value);
    }

    public IBrush TitleForeground
    {
        get => GetValue(TitleForegroundProperty);
        set => SetValue(TitleForegroundProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    static TitledBorderPanel()
    {
        // Border의 기본 속성들을 상속
        BorderBrushProperty.OverrideDefaultValue<TitledBorderPanel>(Brushes.Black);
        BorderThicknessProperty.OverrideDefaultValue<TitledBorderPanel>(new Thickness(2));
        PaddingProperty.OverrideDefaultValue<TitledBorderPanel>(new Thickness(10));
        CornerRadiusProperty.OverrideDefaultValue<TitledBorderPanel>(new CornerRadius(5));
    }
}