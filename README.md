# TabuKA

Masaüstü Tabu kelime anlatma oyunu. Avalonia UI + .NET 10 ile geliştirilmiştir; Windows/Linux/macOS üzerinde çalışır.

## Özellikler

- **Kategori & kelime veritabanı**: 2346 kelime, 13 kategori (SQLite). İlk çalıştırmada otomatik seed edilir.
- **Oyun motoru**: Takım oluşturma, tur yönetimi, süre sayacı, puan hesaplama, pas hakkı, kazanma koşulu (ScoreToWin).
- **Otomatik Tabu kontrolü**: Mikrofon ile konuşma tanıma (Vosk) veya simülasyon motoru; yasaklı kelime yakalanınca anlık uyarı gösterir.
- **Kelime yönetimi**: Kelime ekleme, düzenleme, silme ve arama arayüzü.
- **Ayarlar**: Light/Dark/System tema, ses motoru seçimi, veritabanı yedekleme/geri yükleme/sıfırlama.

## Teknolojiler

- .NET 10 (`net10.0-windows`)
- Avalonia 12.1.2 + Avalonia.Controls.DataGrid
- Entity Framework Core 10 + SQLite
- CommunityToolkit.Mvvm 8.4.2
- NAudio 3.1.0 (mikrofon kaydı), FontAwesome.Sharp (ikonlar)
- xUnit (testler)

## Kurulum ve Çalıştırma

```bash
dotnet restore
dotnet run --project TabuKA
```

Testler:

```bash
dotnet test TabuKA.Tests
```

Veritabanı `tabuka.db` uygulama klasöründe otomatik oluşturulur; `Data\SeedData` altındaki kelime paketleri boş veritabanına otomatik yüklenir.

## Ses Tanıma (Vosk)

- **Simülasyon modu** (varsayılan): Mikrofon kaydını yapar ve tanıma sonucu tetikleyicisi oyun tarafından yönlendirilir.
- **Vosk modu**: `Models\vosk-model-tr` klasörüne Türkçe Vosk modeli indirilip yerleştirildikten sonra ayarlar ekranından etkinleştirilir. Model yoksa ilk açılışta otomatik indirme arka planda denenir.