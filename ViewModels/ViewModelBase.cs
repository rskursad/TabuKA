using CommunityToolkit.Mvvm.ComponentModel;
using TabuKA.Services;

namespace TabuKA.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Ekranın anlık ölçülerinden türetilen boyut sınıfı.
    /// Kök görünüm (MainView) değiştiğinde tüm ekranlar bağlı kalan değerleri otomatik günceller.
    /// </summary>
    public ResponsiveState Responsive => ResponsiveState.Instance;
}
