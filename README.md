# TabuKA

Masaüstü Tabu kelime anlatma oyunu. Avalonia UI + .NET 10 ile geliştirilmiştir; Windows/Linux/macOS üzerinde çalışır.

## Özellikler

- **Kategori & kelime veritabanı**: 2346 kelime, 13 kategori (SQLite). İlk çalıştırmada otomatik seed edilir.
- **Oyun motoru**: Takım oluşturma, tur yönetimi, süre sayacı, puan hesaplama, pas hakkı, kazanma koşulu (ScoreToWin).
- **Otomatik Tabu kontrolü**: Mikrofon ile konuşma tanıma (Vosk) veya simülasyon motoru; yasaklı kelime yakalanınca anlık uyarı gösterir.
- **Kelime yönetimi**: Kelime ekleme, düzenleme, silme ve arama arayüzü.
- **Ayarlar**: Light/Dark/System tema, ses motoru seçimi, veritabanı yedekleme/geri yükleme/sıfırlama.

## Teknolojiler

- .NET 10 (`net10.0` / `net10.0-windows` / `net10.0-android`)
- Avalonia 12.1.2 + Avalonia.Controls.DataGrid
- Entity Framework Core 10 + SQLite
- CommunityToolkit.Mvvm 8.4.2
- NAudio 3.1.0 (mikrofon kaydı), FontAwesome.Sharp (ikonlar)
- xUnit (testler)

## Kurulum ve Çalıştırma

### Make (Linux / macOS / Windows-Git-Bash)

```bash
make build          # Derle  (varsayılan: Debug)
make run            # Masaüstü uygulamayı çalıştır
make test           # Birim testleri
make ci             # Derle + test
make publish        # Bulunulan platform için yayınla
make publish-linux  # Kendi kendine yeten Linux paketi (linux-x64)
make publish-win    # Windows paketi (win-x64) - yalnızca Windows'ta
make publish-mac    # macOS paketi (osx-x64) - yalnızca macOS'ta
make android-build  # Android APK derle
make android-run    # Bağlı cihaz/emülatörde çalıştır
make workload-android  # Android iş yükünü kur
make clean          # bin/obj/publish temizliği
```

Konfigürasyon değiştirmek için: `make run CONFIG=Release`

### Doğrudan dotnet

```bash
dotnet restore
dotnet run --project TabuKA                     # Linux/macOS
dotnet run --project TabuKA -f net10.0-windows  # Windows
dotnet build TabuKA.Android                     # Android APK (önce: dotnet workload install android)
```

Testler:

```bash
dotnet test TabuKA.Tests
```

Veritabanı `tabuka.db` uygulama klasöründe otomatik oluşturulur; `Data\SeedData` altındaki kelime paketleri boş veritabanına otomatik yüklenir.

## Platform Destek Matrisi

| Platform       | Hedef TFM        | Çıktı Türü | Nasıl        |
|----------------|------------------|------------|--------------|
| Linux          | `net10.0`        | Exe        | `make run`   |
| macOS          | `net10.0`        | Exe        | `make run`   |
| Windows        | `net10.0-windows`| WinExe     | `TabuKA.ps1 run` |
| Android        | `net10.0-android`| APK        | `make android-build` |

Windows'ta alt kütüphane hedefi (`net10.0`) Android referansı için `Library` olarak derlenir; Linux/macOS'ta aynı hedef masaüstü için `Exe` olur.

## Ses Tanıma (Vosk)

- **Simülasyon modu** (varsayılan): Mikrofon kaydını yapar ve tanıma sonucu tetikleyicisi oyun tarafından yönlendirilir.
- **Vosk modu**: `Models\vosk-model-tr` klasörüne Türkçe Vosk modeli indirilip yerleştirildikten sonra ayarlar ekranından etkinleştirilir. Model yoksa ilk açılışta otomatik indirme arka planda denenir.