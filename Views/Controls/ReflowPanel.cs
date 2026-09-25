using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace TabuKA.Views.Controls;

/// <summary>
/// Sabit sütun sayısına sahip <c>Grid</c> yerine geçen panel.
/// <para>
/// <see cref="Columns"/> kadar sütun kullanır; her sütun eşit ağırlıkla paylaşılır.
/// <see cref="ReflowPanel.Columns"/> bağlandığında dar ekranlarda sütun sayısı
/// 1'e düşürülerek kartlar alt alta yığılır.
/// </para>
/// </summary>
public class ReflowPanel : Panel
{
    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<ReflowPanel, int>(nameof(Columns), 1);

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<ReflowPanel, double>(nameof(RowSpacing), 0);

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<ReflowPanel, double>(nameof(ColumnSpacing), 0);

    /// <summary>Çocuğun sütun içindeki ağırlığı (varsayılan 1).</summary>
    public static readonly AttachedProperty<double> ChildWeightProperty =
        AvaloniaProperty.RegisterAttached<ReflowPanel, Control, double>("ChildWeight", 1d);

    /// <summary>Çocuk için sabit genişlik. NaN ise ağırlığa göre paylaştırılır.</summary>
    public static readonly AttachedProperty<double> ChildFixedWidthProperty =
        AvaloniaProperty.RegisterAttached<ReflowPanel, Control, double>("ChildFixedWidth", double.NaN);

    public int Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public static void SetChildWeight(Control element, double value) => element.SetValue(ChildWeightProperty, value);
    public static double GetChildWeight(Control element) => element.GetValue(ChildWeightProperty);

    public static void SetChildFixedWidth(Control element, double value) => element.SetValue(ChildFixedWidthProperty, value);
    public static double GetChildFixedWidth(Control element) => element.GetValue(ChildFixedWidthProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var columns = Math.Max(1, Columns);
        var visible = GetVisibleChildren();
        if (visible.Count == 0)
        {
            return default;
        }

        var cells = ComputeCells(visible, availableSize.Width, columns);
        var totalWidth = 0d;
        var totalHeight = 0d;

        for (var rowStart = 0; rowStart < visible.Count; rowStart += columns)
        {
            var rowCount = Math.Min(columns, visible.Count - rowStart);
            var rowHeight = 0d;
            var rowWidth = 0d;

            for (var i = 0; i < rowCount; i++)
            {
                var index = rowStart + i;
                var child = visible[index];
                var (cellWidth, spacing) = cells[index];
                var contentWidth = Math.Max(0, cellWidth - spacing);

                child.Measure(new Size(contentWidth, double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
                rowWidth += cellWidth;
            }

            totalHeight += rowHeight;
            if (rowStart > 0)
            {
                totalHeight += RowSpacing;
            }

            totalWidth = Math.Max(totalWidth, rowWidth);
        }

        return new Size(totalWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Math.Max(1, Columns);
        var visible = GetVisibleChildren();
        if (visible.Count == 0)
        {
            return finalSize;
        }

        var cells = ComputeCells(visible, finalSize.Width, columns);
        var y = 0d;

        for (var rowStart = 0; rowStart < visible.Count; rowStart += columns)
        {
            var rowCount = Math.Min(columns, visible.Count - rowStart);
            var rowHeight = 0d;

            for (var i = 0; i < rowCount; i++)
            {
                rowHeight = Math.Max(rowHeight, visible[rowStart + i].DesiredSize.Height);
            }

            var x = 0d;
            for (var i = 0; i < rowCount; i++)
            {
                var index = rowStart + i;
                var child = visible[index];
                var (cellWidth, spacing) = cells[index];

                child.Arrange(new Rect(x, y, Math.Max(0, cellWidth - spacing), rowHeight));
                x += cellWidth;
            }

            y += rowHeight + RowSpacing;
        }

        return finalSize;
    }

    private List<Control> GetVisibleChildren()
    {
        var result = new List<Control>(Children.Count);
        foreach (var child in Children)
        {
            if (child.IsVisible)
            {
                result.Add(child);
            }
        }

        return result;
    }

    /// <summary>
    /// Her çocuk için (hücre genişliği, hücre sonundaki boşluk) çifti hesaplar.
    /// <para>
    /// Hesaplama satır bazlıdır: sabit genişlikli çocuklar kendi genişliklerini alır,
    /// kalan alan ağırlıklara göre aynı satırdaki esnek çocuklar arasında paylaştırılır.
    /// Böylece sütun sayısı daralınca (örn. <c>*,380</c> → <c>380 / *</c>) yatay taşma oluşmaz.
    /// </para>
    /// </summary>
    private (double CellWidth, double Spacing)[] ComputeCells(IReadOnlyList<Control> children, double availableWidth, int columns)
    {
        var cells = new (double, double)[children.Count];

        for (var rowStart = 0; rowStart < children.Count; rowStart += columns)
        {
            var rowCount = Math.Min(columns, children.Count - rowStart);
            var spacingTotal = (rowCount - 1) * ColumnSpacing;

            var fixedTotal = 0d;
            var weightTotal = 0d;
            for (var i = 0; i < rowCount; i++)
            {
                var fixedWidth = GetChildFixedWidth(children[rowStart + i]);
                if (!double.IsNaN(fixedWidth))
                {
                    fixedTotal += fixedWidth;
                }
                else
                {
                    weightTotal += GetChildWeight(children[rowStart + i]);
                }
            }

            // Sabit genişlikler mevcut alanı aşıyorsa orantılı olarak daralt.
            var fixedScale = 1d;
            var availableForContent = Math.Max(0, availableWidth - spacingTotal);
            if (fixedTotal > availableForContent && fixedTotal > 0)
            {
                fixedScale = availableForContent / fixedTotal;
                fixedTotal = availableForContent;
            }

            var autoSpace = Math.Max(0, availableWidth - fixedTotal - spacingTotal);

            for (var i = 0; i < rowCount; i++)
            {
                var index = rowStart + i;
                var isLastInRow = i == rowCount - 1;
                var spacing = isLastInRow ? 0d : ColumnSpacing;

                var fixedWidth = GetChildFixedWidth(children[index]);
                double cellWidth;

                if (!double.IsNaN(fixedWidth))
                {
                    cellWidth = fixedWidth * fixedScale + spacing;
                }
                else if (weightTotal > 0)
                {
                    cellWidth = autoSpace * (GetChildWeight(children[index]) / weightTotal) + spacing;
                }
                else
                {
                    cellWidth = 0d;
                }

                cells[index] = (cellWidth, spacing);
            }
        }

        return cells;
    }
}
