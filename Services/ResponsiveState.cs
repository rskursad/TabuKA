using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace TabuKA.Services;

public enum ResponsiveSizeClass
{
    /// <summary>Telefonlar ve dar pencereler (tek kolon, kompakt ölçüler).</summary>
    Compact,

    /// <summary>Tabletler / orta genişlikte pencereler (iki kolon uygulanabilir).</summary>
    Medium,

    /// <summary>Masaüstü / geniş pencereler (tam çok kolonlu düzen).</summary>
    Expanded
}

/// <summary>
/// Uygulama genelinde paylaşılan, gözlenebilir ekran durumu.
/// Kök görünüm (MainView) boyut ve güvenli alan değiştiğinde bunu günceller;
/// tüm ekranlar <c>ViewModelBase.Responsive</c> üzerinden okur.
/// </summary>
public sealed class ResponsiveState : INotifyPropertyChanged
{
    public const double CompactMaxWidth = 700;
    public const double ExpandedMinWidth = 1000;

    public static ResponsiveState Instance { get; } = new();

    private double _width = 1000;
    private double _height = 800;
    private Thickness _safeAreaPadding;

    public double Width
    {
        get => _width;
        private set => SetField(ref _width, value);
    }

    public double Height
    {
        get => _height;
        private set => SetField(ref _height, value);
    }

    /// <summary>Çentik / sistem navigasyon çubuğu gibi güvenli alan boşlukları.</summary>
    public Thickness SafeAreaPadding
    {
        get => _safeAreaPadding;
        private set
        {
            if (SetField(ref _safeAreaPadding, value))
            {
                OnPropertyChanged(nameof(SafeAreaPaddingBottom));
                OnPropertyChanged(nameof(SafeAreaPaddingTop));
            }
        }
    }

    public double SafeAreaPaddingBottom => _safeAreaPadding.Bottom;
    public double SafeAreaPaddingTop => _safeAreaPadding.Top;

    public ResponsiveSizeClass SizeClass
    {
        get
        {
            if (_width <= 0)
            {
                return ResponsiveSizeClass.Expanded;
            }

            if (_width <= CompactMaxWidth)
            {
                return ResponsiveSizeClass.Compact;
            }

            return _width >= ExpandedMinWidth ? ResponsiveSizeClass.Expanded : ResponsiveSizeClass.Medium;
        }
    }

    public bool IsCompact => SizeClass == ResponsiveSizeClass.Compact;
    public bool IsMedium => SizeClass == ResponsiveSizeClass.Medium;
    public bool IsExpanded => SizeClass == ResponsiveSizeClass.Expanded;

    /// <summary>Boyut sınıfı değiştiğinde tetiklenen olay.</summary>
    public event EventHandler<ResponsiveSizeClass>? SizeClassChanged;

    public void Update(double width, double height)
    {
        Update(width, height, _safeAreaPadding);
    }

    public void Update(double width, double height, Thickness safeAreaPadding)
    {
        var previousClass = SizeClass;

        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
        SafeAreaPadding = safeAreaPadding;

        OnPropertyChanged(nameof(SizeClass));
        OnPropertyChanged(nameof(IsCompact));
        OnPropertyChanged(nameof(IsMedium));
        OnPropertyChanged(nameof(IsExpanded));

        if (previousClass != SizeClass)
        {
            SizeClassChanged?.Invoke(this, SizeClass);
        }
    }

    /// <summary>Kök görünümün güvenli alan boşluklarını okur (Android çentik / sistem çubukları).</summary>
    public static Thickness ReadSafeAreaPadding(Control root)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(root);
            var insetsManager = topLevel?.InsetsManager;
            return insetsManager?.SafeAreaPadding ?? default;
        }
        catch
        {
            return default;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
