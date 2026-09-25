using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using TabuKA.Converters;
using TabuKA.Services;
using TabuKA.Views.Controls;
using Xunit;

namespace TabuKA.Tests;

public class ResponsiveLayoutTests
{
    private static void SetSizeClass(double width)
    {
        ResponsiveState.Instance.Update(width, 800);
    }

    private static double MeasurePanel(ReflowPanel panel, double width)
    {
        panel.Measure(new Size(width, double.PositiveInfinity));
        return panel.DesiredSize.Width;
    }

    // ---------------- ResponsiveState ----------------

    [Theory]
    [InlineData(360, ResponsiveSizeClass.Compact)]
    [InlineData(411, ResponsiveSizeClass.Compact)]
    [InlineData(700, ResponsiveSizeClass.Compact)]
    [InlineData(701, ResponsiveSizeClass.Medium)]
    [InlineData(999, ResponsiveSizeClass.Medium)]
    [InlineData(1000, ResponsiveSizeClass.Expanded)]
    [InlineData(1920, ResponsiveSizeClass.Expanded)]
    public void SizeClass_RespectsBreakpoints(double width, ResponsiveSizeClass expected)
    {
        SetSizeClass(width);
        Assert.Equal(expected, ResponsiveState.Instance.SizeClass);
    }

    [Fact]
    public void SizeClass_RaisesChangedEventOnlyOnTransition()
    {
        SetSizeClass(1200);
        var raised = 0;
        ResponsiveState.Instance.SizeClassChanged += (_, _) => raised++;

        ResponsiveState.Instance.Update(1100, 800);
        Assert.Equal(0, raised);

        ResponsiveState.Instance.Update(500, 800);
        Assert.Equal(1, raised);

        ResponsiveState.Instance.Update(400, 800);
        Assert.Equal(1, raised);
    }

    // ---------------- ResponsiveValueConverter ----------------

    [Theory]
    [InlineData(360, 12.0)]
    [InlineData(800, 20.0)]
    [InlineData(1400, 24.0)]
    public void ResponsiveValueConverter_PicksValueForSizeClass(double width, double expected)
    {
        SetSizeClass(width);
        var converter = new ResponsiveValueConverter();

        var result = converter.Convert(null, typeof(double), "12|20|24", CultureInfo.InvariantCulture);

        Assert.Equal(expected, Assert.IsType<double>(result), 3);
    }

    [Fact]
    public void ResponsiveValueConverter_FallsBackToLeftmostTokenWhenMissing()
    {
        SetSizeClass(1400);
        var converter = new ResponsiveValueConverter();

        var result = converter.Convert(null, typeof(double), "12|20", CultureInfo.InvariantCulture);

        Assert.Equal(20.0, Assert.IsType<double>(result), 3);
    }

    [Theory]
    [InlineData("10,5", 10, 5, 10, 5)]
    [InlineData("8", 8, 8, 8, 8)]
    [InlineData("1,2,3,4", 1, 2, 3, 4)]
    public void ResponsiveValueConverter_ParsesThicknessTokens(string token, double l, double t, double r, double b)
    {
        SetSizeClass(360);
        var converter = new ResponsiveValueConverter();

        var result = converter.Convert(null, typeof(Thickness), token, CultureInfo.InvariantCulture);

        var thickness = Assert.IsType<Thickness>(result);
        Assert.Equal(l, thickness.Left, 3);
        Assert.Equal(t, thickness.Top, 3);
        Assert.Equal(r, thickness.Right, 3);
        Assert.Equal(b, thickness.Bottom, 3);
    }

    [Fact]
    public void ResponsiveValueConverter_ParsesEnums()
    {
        SetSizeClass(360);
        var converter = new ResponsiveValueConverter();

        var result = converter.Convert(null, typeof(Orientation), "Vertical|Horizontal", CultureInfo.InvariantCulture);

        Assert.Equal(Orientation.Vertical, Assert.IsType<Orientation>(result));
    }

    // ---------------- ResponsiveVisibilityConverter ----------------

    [Theory]
    [InlineData(360, "compact", true)]
    [InlineData(800, "compact", false)]
    [InlineData(360, "!compact", false)]
    [InlineData(800, "!compact", true)]
    [InlineData(360, "compact|medium", true)]
    [InlineData(1400, "compact|medium", false)]
    public void ResponsiveVisibilityConverter_MatchesSpec(double width, string parameter, bool expected)
    {
        SetSizeClass(width);
        var converter = new ResponsiveVisibilityConverter();

        var result = converter.Convert(null, typeof(bool), parameter, CultureInfo.InvariantCulture);

        Assert.Equal(expected, Assert.IsType<bool>(result));
    }

    // ---------------- ReflowPanel ----------------

    [Fact]
    public void ReflowPanel_SingleColumnStackesChildren()
    {
        var panel = new ReflowPanel { Columns = 1, RowSpacing = 10 };
        for (var i = 0; i < 3; i++)
        {
            panel.Children.Add(new Border { Height = 40 });
        }

        panel.Measure(new Size(300, double.PositiveInfinity));

        Assert.Equal(300, panel.DesiredSize.Width, 3);
        Assert.Equal(140, panel.DesiredSize.Height, 3);
    }

    [Fact]
    public void ReflowPanel_TwoColumnsShareWidthEqually()
    {
        var panel = new ReflowPanel { Columns = 2, ColumnSpacing = 10 };
        panel.Children.Add(new Border { Height = 50 });
        panel.Children.Add(new Border { Height = 50 });

        panel.Measure(new Size(400, double.PositiveInfinity));

        // 400 - 10 boşluk = 390 / 2 = 195
        Assert.Equal(400, panel.DesiredSize.Width, 3);
        Assert.Equal(50, panel.DesiredSize.Height, 3);
    }

    [Fact]
    public void ReflowPanel_RespectsWeights()
    {
        var panel = new ReflowPanel { Columns = 2, ColumnSpacing = 0 };
        var a = new Border { Height = 30 };
        var b = new Border { Height = 30 };
        ReflowPanel.SetChildWeight(a, 1);
        ReflowPanel.SetChildWeight(b, 3);
        panel.Children.Add(a);
        panel.Children.Add(b);

        panel.Measure(new Size(400, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 400, 100));

        Assert.Equal(100, a.Bounds.Width, 3);
        Assert.Equal(300, b.Bounds.Width, 3);
    }

    [Fact]
    public void ReflowPanel_FixedWidthChildKeepsItsWidthOnWideLayout()
    {
        var panel = new ReflowPanel { Columns = 2, ColumnSpacing = 12 };
        var table = new Border { Height = 100 };
        var side = new Border { Height = 100 };
        ReflowPanel.SetChildFixedWidth(side, 380);
        panel.Children.Add(table);
        panel.Children.Add(side);

        panel.Measure(new Size(900, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 900, 200));

        Assert.Equal(380, side.Bounds.Width, 3);
        Assert.Equal(508, table.Bounds.Width, 3);
    }

    [Fact]
    public void ReflowPanel_FixedWidthChildDoesNotOverflowWhenColumnsCollapse()
    {
        // columns=1 olduğunda sabit 380'lik yan panel kendi satırında kalmalı,
        // mevcut genişliği aşacak şekilde taşmamalı.
        var panel = new ReflowPanel { Columns = 1, RowSpacing = 8 };
        var table = new Border { Height = 200 };
        var side = new Border { Height = 300 };
        ReflowPanel.SetChildFixedWidth(side, 380);
        panel.Children.Add(table);
        panel.Children.Add(side);

        panel.Measure(new Size(336, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 336, 600));

        Assert.Equal(336, table.Bounds.Width, 3);
        Assert.Equal(336, side.Bounds.Width, 3);
        Assert.Equal(208, side.Bounds.Y, 3);
        Assert.Equal(508, panel.DesiredSize.Height, 3);
    }

    [Fact]
    public void ReflowPanel_ClampsFixedWidthsThatExceedAvailableWidth()
    {
        // Tek satırda iki sabit çocuk toplamı mevcut alandan büyükse orantılı daraltılır.
        var panel = new ReflowPanel { Columns = 2, ColumnSpacing = 20 };
        var a = new Border { Height = 30 };
        var b = new Border { Height = 30 };
        ReflowPanel.SetChildFixedWidth(a, 300);
        ReflowPanel.SetChildFixedWidth(b, 300);
        panel.Children.Add(a);
        panel.Children.Add(b);

        panel.Measure(new Size(320, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 320, 100));

        Assert.Equal(150, a.Bounds.Width, 3);
        Assert.Equal(150, b.Bounds.Width, 3);
    }

    [Fact]
    public void ReflowPanel_SkipsInvisibleChildren()
    {
        // 3 çocuktan 1'i gizli -> görünür 2 çocuk tek satırda kalır
        var panel = new ReflowPanel { Columns = 2, RowSpacing = 10 };
        panel.Children.Add(new Border { Height = 40 });
        var hidden = new Border { Height = 999, IsVisible = false };
        panel.Children.Add(hidden);
        panel.Children.Add(new Border { Height = 40 });

        panel.Measure(new Size(300, double.PositiveInfinity));

        Assert.Equal(300, panel.DesiredSize.Width, 3);
        Assert.Equal(40, panel.DesiredSize.Height, 3);
    }

    [Fact]
    public void ReflowPanel_ThreeChildrenInTwoColumnsWrapToSecondRow()
    {
        var panel = new ReflowPanel { Columns = 2, RowSpacing = 10 };
        for (var i = 0; i < 3; i++)
        {
            panel.Children.Add(new Border { Height = 40 });
        }

        panel.Measure(new Size(300, double.PositiveInfinity));

        Assert.Equal(90, panel.DesiredSize.Height, 3);
    }
}
