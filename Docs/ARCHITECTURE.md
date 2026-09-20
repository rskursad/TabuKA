# TabuKA - Sistem Mimarisi ve Tasarım Dokümantasyonu

TabuKA, .NET 10 ve Avalonia UI 12 kullanılarak geliştirilmiş, modern, platformlar arası (Cross-Platform) bir Tabu kelime anlatma oyunudur.

---

## 1. Mimari Genel Bakış

Proje, **MVVM (Model-View-ViewModel)** desenine ve bağımlılık enjeksiyonu (Dependency Injection - `Microsoft.Extensions.DependencyInjection`) prensiplerine sıkı sıkıya bağlıdır.

```
TabuKA/
├── Entities/            # Veritabanı ve iş modeli varlıkları (EF Core)
├── Data/                # SQLite DbContext, DesignTimeDbContextFactory ve Seed verileri
├── Services/            # İş mantığı, ses sentezleme ve konuşma tanıma servisleri
├── ViewModels/          # CommunityToolkit.Mvvm tabanlı ViewModel sınıfları
├── Views/               # Avalonia XAML arayüz bileşenleri ve şablonları
├── Converters/          # XAML Binding değer dönüştürücüleri
├── Assets/              # İkonlar ve statik kaynaklar
└── TabuKA.Tests/        # xUnit ve SQLite InMemory entegrasyon ve akış testleri
```

---

## 2. Temel Servisler

### 2.1. `GameService` (`IGameService`)
Oyun mantığının merkezidir:
- **`CreateGameAsync(settings, teams)`**: Oyun oturumunu başlatır, takımları ve başlangıç skorlarını yapılandırır.
- **`StartRoundAsync(gameSessionId, teamId)`**: Rastgele kullanılmamış kelime seçerek bir tur kartı üretir.
- **`SubmitAnswerAsync(roundId, result, advanceTeam = true)`**: Doğru, Tabu veya Pas sonucunu işler. Bir takımın süresi boyunca ardı ardına gelen kelimeleri aynı takımda tutmak için `advanceTeam: false` parametresiyle çağrılır.
- **`NextTurnAsync(gameSessionId)`**: Bir takımın süresi dolduğunda sıradaki takıma geçer ve pas haklarını sıfırlar.
- **`CanPassAsync(gameSessionId, teamId)`**: Takımın mevcut tur için pas hakkının kalıp kalmadığını denetler.
- **`CheckTabooWordAsync(spokenText, forbiddenWords)`**: Metin içinde yasaklı kelimeleri duyarsız (case-insensitive) tarar.

### 2.2. `SoundEffectService` (`ISoundEffectService`)
Oyun içi ses efektleri için harici medya dosyalarına ihtiyaç duymadan, NAudio ile bellek üzerinde saf PCM sinyalleri sentezler:
- `PlayCorrect()`: E5 -> A5 çift tonlu zil sesi (Doğru tahmin).
- `PlayTaboo()`: 140Hz kare dalga klasik alçak frekanslı buzzer sesi (Yasaklı kelime / Tabu).
- `PlayPass()`: 440Hz -> 330Hz hızlı geçiş sesi (Pas hakkı).
- `PlayTick()`: 900Hz metronom tık sesi (Son 5 saniye geri sayım).
- `PlayTimeUp()`: 220Hz + 440Hz çift düdük/korna sesi (Süre bittiğinde).
Kullanıcının `MasterVolume` ses seviyesine ve genel ses ayarlarına dinamik olarak uyar.

### 2.3. `SpeechRecognitionManager` & `VoskSpeechRecognitionService`
Otomatik tabu kontrolü için iki katmanlı ses tanıma motoru:
- **`Simulation`**: Mikrofon veya model bulunmayan ortamlarda test amaçlı simülatör.
- **`Vosk (Offline)`**: Tamamen yerel ve çevrimdışı çalışan Türkçe konuşma tanıma motoru. Karttaki yasaklı kelimeleri dinamik JSON gramerine ekleyerek yüksek doğruluk sağlar.
- UI iş parçacığı güvenliği `Dispatcher.UIThread` ile sağlanır.

### 2.4. `DatabaseService` (`IDatabaseService`)
Entity Framework Core ve SQLite üzerinden kelime, kategori ve ayar yönetimini üstlenir:
- 2300'ün üzerinde hazır kelime paketi (`Data/SeedData/packs/*.json`).
- JSON veritabanı yedeği alma (`ExportToJsonAsync`) ve geri yükleme (`ImportFromJsonAsync`).
- El ile kelime/kategori ekleme, düzenleme ve arama.

---

## 3. Kullanıcı Arayüzü (UI/UX) Akışı

1. **Ana Menü (`MainMenuView`)**:
   - Veritabanı durumu (kelime ve kategori sayıları).
   - "Yeni Oyun Başlat", "Kelime Havuzu & Yönetimi", "İstatistikler & Geçmiş", "Ayarlar".
2. **Oyun Kurulumu (`GameSetupView`)**:
   - Tur süresi seçimi (30 - 180 sn).
   - Pas hakkı seçimi (0 - 10, 0 = Sınırsız).
   - Kazanma hedef skoru (15, 25, 50 veya serbest).
   - Sesli otomatik tabu kontrolü açma/kapama.
   - Takım sayısı ve takım isimleri belirleme.
   - Kategori filtreleme ("Tümünü Seç" ve "Temizle" butonları ile).
3. **Oyun Oynanış Ekranı (`GamePlayView`)**:
   - **Hazır mısınız? Ara Ekranı**: Cihaz sıradaki takıma geçtiğinde süreyi başlatmadan önce hazır olma fırsatı tanır.
   - **Aktif Kart & Butonlar**: Büyük ana kelime, 5 yasaklı kelime etiketi, hemen kartın altında devasa dokunmatik `DOĞRU (+1)`, `TABU (-1)` ve `PAS GEÇ` butonları.
   - **Süre Doldu & Tur Özeti**: O turdaki doğru, tabu ve pas istatistikleri sunulur, ardından sıradaki takıma devredilir.
   - **Duraklatma (Pause) Modalı**: Süreyi dondurur, devam etme veya oyunu bitirme seçeneği verir.
   - **Şampiyonluk Podyumu**: Hedef puana ulaşıldığında veya oyun bitirildiğinde kazanan takımı kutlar.
