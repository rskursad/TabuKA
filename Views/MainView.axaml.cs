using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.VisualTree;
using TabuKA.Services;

namespace TabuKA.Views;

public partial class MainView : UserControl
{
    private IInsetsManager? _insetsManager;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        SizeChanged += OnRootSizeChanged;
        UpdateResponsiveState();

        _insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (_insetsManager is not null)
        {
            _insetsManager.SafeAreaChanged += OnSafeAreaChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        SizeChanged -= OnRootSizeChanged;

        if (_insetsManager is not null)
        {
            _insetsManager.SafeAreaChanged -= OnSafeAreaChanged;
            _insetsManager = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnRootSizeChanged(object? sender, SizeChangedEventArgs e) => UpdateResponsiveState();

    private void OnSafeAreaChanged(object? sender, EventArgs e) => UpdateResponsiveState();

    private void UpdateResponsiveState()
    {
        var safeArea = ResponsiveState.ReadSafeAreaPadding(this);
        SafeAreaHost.Padding = safeArea;
        ResponsiveState.Instance.Update(Bounds.Width, Bounds.Height, safeArea);
    }
}
