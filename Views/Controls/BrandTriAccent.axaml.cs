using Avalonia;
using Avalonia.Controls;

namespace TabuKA.Views.Controls;

public partial class BrandTriAccent : UserControl
{
    public static readonly StyledProperty<string> VariantProperty =
        AvaloniaProperty.Register<BrandTriAccent, string>(nameof(Variant), "Corner");

    public static readonly StyledProperty<double> AccentOpacityProperty =
        AvaloniaProperty.Register<BrandTriAccent, double>(nameof(AccentOpacity), 0.22);

    public string Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public double AccentOpacity
    {
        get => GetValue(AccentOpacityProperty);
        set => SetValue(AccentOpacityProperty, value);
    }

    public BrandTriAccent()
    {
        InitializeComponent();
        ApplyVariant();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty || change.Property == AccentOpacityProperty)
        {
            ApplyVariant();
        }
    }

    private void ApplyVariant()
    {
        if (CornerPath is null || TopEdgePath is null || EdgePath is null)
        {
            return;
        }

        var variant = (Variant ?? "Corner").Trim();
        var opacity = AccentOpacity <= 0 ? 0.22 : AccentOpacity;

        CornerPath.IsVisible = variant.Equals("Corner", System.StringComparison.OrdinalIgnoreCase);
        TopEdgePath.IsVisible = variant.Equals("TopEdge", System.StringComparison.OrdinalIgnoreCase);
        EdgePath.IsVisible = variant.Equals("Edge", System.StringComparison.OrdinalIgnoreCase);

        CornerPath.Opacity = opacity;
        TopEdgePath.Opacity = opacity;
        EdgePath.Opacity = opacity;
    }
}
