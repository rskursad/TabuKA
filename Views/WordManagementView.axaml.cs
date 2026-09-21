using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TabuKA.Views;

public partial class WordManagementView : UserControl
{
    public WordManagementView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}