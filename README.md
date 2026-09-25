# TabuKA

Masaüstü ve mobil Tabu kelime anlatma oyunu. Avalonia UI + .NET 10 ile geliştirilmiştir; Windows, Linux, macOS ve Android üzerinde çalışır.

## Özellikler

- **Kategori & kelime veritabanı**: 2346 kelime, 13 kategori (SQLite). İlk çalıştırmada otomatik seed edilir.
- **Oyun motoru**: Takım oluşturma, tur yönetimi, süre sayacı, puan hesaplama, pas hakkı, kazanma koşulu (ScoreToWin).
- **Otomatik Tabu kontrolü**: Mikrofon ile konuşma tanıma (Vosk) veya simülasyon motoru; yasaklı kelime yakalanınca anlık uyarı gösterir.
- **Kelime yönetimi**: Kelime ekleme, düzenleme, silme ve arama arayüzü.
- **Ayarlar**: Light/Dark/System tema desteği, ses motoru seçimi, veritabanı yedekleme/geri yükleme/sıfırlama.
- **Profesyonel Arayüz**: Inter modern tipografi, şık kart tasarımları ve açık/koyu tema adaptasyonu.

## Teknolojiler

- .NET 10 (`net10.0` / `net10.0-windows` / `net10.0-android`)
- Avalonia 12.1.2 + Avalonia.Fonts.Inter + Avalonia.Controls.DataGrid
- Entity Framework Core 10 + SQLite
- CommunityToolkit.Mvvm 8.4.2
- NAudio 3.1.0 (mikrofon kaydı)
- xUnit (testler)

## Kurulum ve Çalıştırma

### Make (Linux / macOS / Windows Git-Bash / WSL)

```bash
make build                  # Masaüstü derle (varsayılan: Debug)
make run                    # Masaüstü uygulamayı çalıştır
make test                   # Birim testleri
make ci                     # Derle + test
make publish                # Bulunulan masaüstü platformu için yayınla
make publish-linux          # Kendi kendine yeten Linux paketi (linux-x64)
make publish-win            # Windows paketi (win-x64) - Windows'ta
make publish-mac            # macOS paketi (osx-x64) - macOS'ta

# Android Komutları
make workload-android        # Android iş yükünü kur (dotnet workload install android)
make android-build           # Android APK derle (Debug)
make android-run             # Bağlı cihaz/emülatörde çalıştır
make android-keystore-custom # Kişisel bilgilerinizle interaktif imza anahtarı (keystore) üret
make android-keystore        # Test imza anahtarı oluştur (şifre size sorulur)
make android-publish         # İmzalı Android paketi yayınla (APK; şifre sorulur)
make android-publish-apk     # İmzalı APK paketi yayınla
make android-publish-bundle  # İmzalı AAB (Google Play Store paketi) yayınla
make clean                   # bin/obj/publish temizliği
```

> Ortak proje olduğundan keystore şifreleri hiçbir yerde hazır girilmez:
> `make android-publish` şifreyi sizden etkileşimli olarak ister. Dilerseniz
> CLI parametresi (`KEYSTORE_PASS=...`) veya `keystore.properties` içinden de
> verebilirsiniz.

#### Kişisel İmza ve `keystore.properties` Yapılandırması:
Kendi imzanızı kullanmak için iki pratik yöntem mevcuttur:

1. **İnteraktif Olarak Kendi İmzanızı Üretin:**
   ```bash
   make android-keystore-custom
   ```
   *Terminalde Ad, Soyad, Kurum, Şehir ve Şifre bilgilerinizi girerek kendi adınıza güvenli bir Keystore üretir ve `keystore.properties` dosyasına otomatik kaydedebilir.*

2. **Var Olan Keystore Dosyanızı Bağlayın:**
   `keystore.properties.example` şablonunu `keystore.properties` olarak kopyalayın:
   ```bash
   cp keystore.properties.example keystore.properties
   ```
   İçeriğini kendi imza dosyanıza göre düzenleyin:
   ```properties
   storeFile=my-release-key.keystore
   keyAlias=myalias
   # storePassword=mykeystoreşifreniz   (opsiyonel; boş bırakırsanız sorulur)
   # keyPassword=mykeystoreşifreniz     (opsiyonel; boşsa keystore şifresi kullanılır)
   ```
   *(Bu dosya `.gitignore` içine eklenmiştir, şifreleriniz Git'e gitmez.
   Şifre satırları boş bırakılırsa `make android-publish` çalıştığında
   terminalde etkileşimli olarak istenir — kodsal bir varsayılan yoktur.)*

Ardından doğrudan yayınlayın (şifre sorulacaktır):
```bash
make android-publish
```

Veya şifreyi komut satırından parametre vererek (CI için):
```bash
make android-publish KEYSTORE=my-key.keystore KEY_ALIAS=myalias KEYSTORE_PASS=mypass
```

### PowerShell (Windows: TabuKA.ps1)

Windows ortamında PowerShell üzerinden tüm işlemleri yönetebilirsiniz:

```powershell
.\TabuKA.ps1 build                  # Derle ve testleri çalıştır
.\TabuKA.ps1 run                    # Masaüstü oyunu başlat
.\TabuKA.ps1 build-android          # Android paketini derle
.\TabuKA.ps1 run-android            # Cihazda/emülatörde başlat
.\TabuKA.ps1 create-keystore-custom # Kişisel bilgilerinizle interaktif imza anahtarı üret
.\TabuKA.ps1 create-keystore        # Otomatik test imza anahtarı üret
.\TabuKA.ps1 publish-android        # İmzalı Android paketini yayınla (keystore.properties destekli)
```

Özel imza bilgileriyle yayınlama:
```powershell
.\TabuKA.ps1 publish-android -KeystorePath "my-key.keystore" -KeyAlias "myalias" -KeyPassword "mypassword" -PackageFormat apk
```

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

## Platform Destek Matrisi

| Platform       | Hedef TFM        | Çıktı Türü | Nasıl |
|----------------|------------------|------------|-------|
| Linux          | `net10.0`        | Exe        | `make run` |
| macOS          | `net10.0`        | Exe        | `make run` |
| Windows        | `net10.0-windows`| WinExe     | `.\TabuKA.ps1 run` / `make run` |
| Android (APK)  | `net10.0-android`| İmzalı APK | `make android-publish` / `.\TabuKA.ps1 publish-android` |
| Android (AAB)  | `net10.0-android`| İmzalı AAB | `make android-publish-bundle` / `.\TabuKA.ps1 publish-android -PackageFormat aab` |

## Ses Tanıma (Vosk)

- **Simülasyon modu** (varsayılan): Mikrofon kaydını yapar ve tanıma sonucu tetikleyicisi oyun tarafından yönlendirilir.
- **Vosk modu**: `Models\vosk-model-tr` klasörüne Türkçe Vosk modeli indirilip yerleştirildikten sonra ayarlar ekranından etkinleştirilir. Model yoksa ilk açılışta otomatik indirme arka planda denenir.
