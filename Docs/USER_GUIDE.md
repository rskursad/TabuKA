# TabuKA - Kullanım Kılavuzu

TabuKA; arkadaşlarınız ve ailenizle eğlenceli vakit geçirmeniz için tasarlanmış modern bir Tabu (kelime anlatma) oyunudur.

---

## 🎮 Oyunun Temel Kuralları

1. **Amaç**: Ekranın en üstünde yer alan **ANA KELİME**yi, altındaki **5 YASAKLI KELİME**yi ve bunların köklerini/türevlerini kullanmadan takım arkadaşlarınıza anlatmaktır.
2. **Puanlama**:
   - **Doğru Bilinen Her Kelime**: `+1 Puan`
   - **Yasaklı Kelime Kullanımı (Tabu)**: `-1 Puan`
   - **Pas Geçme**: `0 Puan` (Tur başına belirlenen pas limiti kadar kullanılabilir).
3. **Tur Süresi**: Belirlenen süre (örn. 60 sn) boyunca aynı takım arka arkaya kart anlatmaya devam eder. Süre bitene kadar anlatım durmaz.

---

## 🚀 Oyuna Başlama Adımları

### 1. Yeni Oyun Kurma
1. Ana menüden **"🎮 Yeni Oyun Başlat"** butonuna tıklayın.
2. Açılan kurulum ekranında:
   - **Tur Süresi**: Takım başına verilecek süreyi kaydırıcı ile ayarlayın (30, 45, 60, 90, 120 veya 180 saniye).
   - **Pas Hakkı**: Bir tur içinde kullanılabilecek maksimum pas sayısını seçin (0 seçilirse sınırsızdır).
   - **Hedef Skor**: Oyunun biteceği skoru belirleyin (örn. 25 Puan). Serbest oynamak isterseniz 0 bırakın.
   - **Sesli Otomatik Tabu**: Açık bırakılırsa mikrofon arka planda konuşmaları dinler ve yasaklı kelime söylendiğinde otomatik olarak Tabu cezası verir.
   - **Takım Sayısı ve İsimleri**: 2 ile 6 arasında takım oluşturabilir ve takımlara özel isimler verebilirsiniz.
   - **Kategoriler**: İster belirli kategorileri seçin, ister tümünü seçerek karışık oynayın.
3. **"🚀 Oyunu Başlat"** butonuna basarak oyunu başlatın.

### 2. Tur Oynanışı
1. **"Sıradaki Takım Hazır mı?"** ekranı karşılar. Sıra hangi takımdaysa cihaz o takımdaki anlatıcıya teslim edilir.
2. **"Turu Başlat"** butonuna tıklandığında süre geri sayımı başlar ve ilk kart gelir.
3. Anlatıcı:
   - Takım doğru bildiğinde yeşil **"✅ DOĞRU"** butonuna tıklar.
   - Yasaklı kelime kullandığında kırmızı **"🚫 TABU"** butonuna tıklar (veya ses algılayıcı otomatik tabu çalar).
   - Bilemediğinde veya geçmek istediğinde turuncu **"⏭️ PAS GEÇ"** butonuna tıklar.
4. Sürenin son 5 saniyesinde uyarıcı tik-tak sesleri duyulur.
5. Süre bittiğinde düdük sesiyle tur sonlanır ve **"Tur Özeti"** ekranında o tur kazanılan net puan gösterilir.
6. **"Sıradaki Takıma Geç"** düğmesine basılarak sıra diğer takıma verilir.

### 3. Oyunu Duraklatma ve Bitirme
- İstediğiniz an sol üstteki **"⏸ Duraklat"** butonuna basarak süreyi durdurabilir ve mola verebilirsiniz.
- Sağ üstteki **"⏹ Bitir"** butonuna basarak dilediğiniz zaman oyunu sonlandırıp sonuç tablosuna gidebilirsiniz.
- Hedef puana ulaşıldığında şampiyonluk ekranı otomatik olarak belirir.

---

## ⚙️ Ayarlar ve Veritabanı Yönetimi

- **Görünüm (Tema)**: Sistem, Açık (Light) veya Koyu (Dark) mod seçimi.
- **Ses Ayarları**: Ana ses seviyesini ayarlama ve **"🔔 Sesi Test Et"** butonu ile ses efektlerini dinleme.
- **Konuşma Tanıma**: Test için Simülasyon veya çevrimdışı kullanım için Vosk motoru seçimi.
- **Veritabanı Yedekleme**:
  - **JSON Olarak Dışa Aktar**: Tüm kelime havuzunu masaüstüne JSON formatında yedekler.
  - **JSON'dan İçe Aktar**: Dışarıdan yeni kelime listeleri yüklemenizi sağlar.
  - **Kelime Yönetimi**: Ana menüden veya ayarlardan dilediğiniz an tek tek kelime ekleyebilir, arayabilir, düzenleyebilir veya silebilirsiniz.
