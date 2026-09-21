using System;
using System.Collections.Generic;
using System.Linq;

namespace TabuKA.Tools;

public class WordGenerator
{
    private static readonly Random _random = new();

    // Temel kelime havuzları her kategori için
    private static readonly Dictionary<string, (string[] mainWords, string[][] forbiddenTemplates)> _categoryData = new()
    {
        ["Genel"] = (
            new[] { "ev", "araba", "kitap", "telefon", "bilgisayar", "masa", "sandalye", "kapı", "pencere", "duvar", "çatı", "zemin", "tavan", "anahtar", "kilit", "çanta", "ayakkabı", "çorap", "pantolon", "gömlek", "tişört", "ceket", "mont", "şapka", "eldiven", "atkı", "bere", "saat", "kolye", "bilezik", "yüzük", "küpe", "parfüm", "cüzdan", "para", "kart", "kimlik", "ehliyet", "pasaport", "bilet", "reçete", "ilaç", "hastane", "doktor", "hemşire", "eczane", "tedavi", "ameliyat", "muayene", "rapor", "sonuç", "kan", "tahlil", "röntgen", "mr", "tomografi", "ekg", "urologi", "kardiyoloji", "noroloji", "ortopedi", "göz", "kulak", "burun", "boğaz", "diş", "çene", "ağız", "dil", "dişçi", "teli", "dolgu", "kök kanalı", "çekim", "implant", "protez", "tartar", "fırçalama", "ip", "bez", "su", "çay", "kahve", "şeker", "süt", "krema", "limon", "portakal", "mandalina", "elma", "armut", "muz", "üzüm", "karpuz", "kavun", "çilek", "kiraz", "şeftali", "kayısı", "erik", "incir", "hurma", "fındık", "ceviz", "badem", "fıstık", "antep fıstığı", "kaju", "brazil cevizi", "macadamia", "peynir", "yoğurt", "ayran", "kefir", "tereyağı", "margarin", "zeytinyağı", "koroneya", "ayçiçek yağı", "riviera", "badem yağı", "kokos yağı", "susam yağı", "hardal yağı", "haşlanmış yumurta", "omlet", "menemen", "sucuklu yumurta", "pastırma", "sucuk", "salam", "jambon", "sosis", "peynirli tost", "sucuklu tost", "karışık tost", "acuka", "humus", "cacık", "haydari", "ezme", "şakşuka", "imam bayıldı", "karnıyarık", "musakka", "patlıcan salatası", "patlıcan kebabı", "kuru fasulye", "nohut", "mercimek", "bezelye", "fasulye", "barbunya", "bakla", "kereviz", "ispanak", "lahana", "karalahana", "brokoli", "karnabahar", "havuç", "patates", "soğan", "sarımsak", "biber", "domates", "salatalık", "marul", "roka", "maydanoz", "dereotu", "nane", "fesleğen", "kekik", "dağ kekiği", "ıspanak", "hindiba", "radika", "pazı", "kuşkonmaz", "kabak", "kabak çiçeği", "kabak mücveri", "kabak tatlısı", "havuç tarator", "havuç salatası", "havuç suyu", "havuç kurabiyesi", "havuç keki", "havuç turşusu" },
            new[] { new[] { "yaşam", "konut", "evin", "mülk" }, new[] { "vasıta", "taşıt", "otomobil", "aracın" }, new[] { "roman", "kitabın", "sayfa", "yazar" } }
        ),
        ["Hayvanlar"] = (
            new[] { "kedi", "köpek", "kuş", "balık", "aslan", "kaplan", "fil", "zürafa", "ayi", "kurt", "tilki", "koyun", "keçi", "inek", "at", "eşek", "deve", "zebra", "kanguru", "koala", "panda", "goril", "şempanze", "orangutan", "maymun", "lemur", "kaplumbağa", "timsah", "yılan", "kertenkele", "çamel", "ceylan", "gazel", "antilop", "bizon", "büfelo", "yaban domuzu", "hindi", "kaz", "ördek", "sakarya", "kuğu", "pelikan", "flamingo", "karga", "baykuş", "uğur", "kelebek", "arı", "karınca", "karınca", "termit", "karaca", "kelebek", "bal arısı", "mumla bal arısı", "çiy arısı", "hornet", "veşpa", "kurt arısı", "papaz arısı", "yaban arısı", "karınca", "termit", "karaca", "kelebek", "bal arısı", "mumla bal arısı", "çiy arısı", "hornet", "veşpa", "kurt arısı", "papaz arısı", "yaban arısı" },
            new[] { new[] { "evcil", "miauv", "tüy", "patı" }, new[] { "evcil", "havhav", "sahip", "yemek" }, new[] { "uç", "kanat", "tüy", "yumurta" } }
        ),
        ["Yiyecekler"] = (
            new[] { "pizza", "hamburger", "çikolata", "dondurma", "sushi", "kebap", "pasta", "salata", "çorba", "kahve", "çay", "ayran", "kek", "kurabiye", "baklava", "künefe", "lokum", "helva", "revani", "şekerpare", "şambali", "kadayıf", "burma", "sütlaç", "kazandibi", "tavuk göğsü", "güllac", "aşure", "noah", "zerde", "hoşaf", "komposto", "şurup", "meyve suyu", "gazoz", "kola", "fanta", "sprite", "pepsi", "soda", "mineral su", "şişe su", "cam su", "pet su", "bardak su", "musluk su", "kaynak su", "içme su", "temiz su", "sağlıklı su", "alkali su", "mineralli su", "demirli su", "kalsiyumlu su", "magnezyumlu su", "potasyumlu su", "sodyumlu su", "klorlu su", "florlu su", "iyodlu su", "selenyumlu su", "çinkolu su", "bakırlı su", "manganezli su", "kromlu su", "molibdenli su", "vanadyumlu su", "nikelli su", "kobaltlı su", "arsenikli su", "kurşunlu su", "civateli su", "kurşunsuz su", "temizlenmiş su", "arzıtılmış su", "içilebilir su", "içilmez su", "kirli su", "saçak su", "yağmur suyu", "kar suyu", "buz suyu", "buhar suyu", "buharlı su", "buharlı su", "buharlı su" },
            new[] { new[] { "hamur", "peynir", "sos", "fırın" }, new[] { "ekmek", "kötü", "patates", "ketchup" }, new[] { "tatlı", "kakao", "şeker", "bar" } }
        ),
        ["Eşyalar"] = (
            new[] { "masa", "sandalye", "telefon", "kitap", "kalem", "bilgisayar", "televizyon", "buzdolabı", "çamaşır makinesi", "bulaşık makinesi", "mikser", "blender", "toaster", "kahve makinesi", "çay makinesi", "su ısıtıcı", "klima", "sobası", "termosifon", "fan", "ventilatör", "hava temizleyici", "nem dengesici", "nem alıcı", "nem verici", "aroma dağıtıcı", "kokulu mum", "kokulu çubuk", "kokulu spray", "oda koklayıcı", "tuvalet koklayıcı", "araba koklayıcı", "çanta koklayıcı", "dolap koklayıcı", "ayakkabı koklayıcı", "spor salonu koklayıcı", "ofis koklayıcı", "ev koklayıcı", "bahçe koklayıcı", "balkon koklayıcı", "teras koklayıcı", "çatı katı koklayıcı", "bodrum katı koklayıcı", "garaj koklayıcı", "depo koklayıcı", "ankastre koklayıcı", "duvar tipi koklayıcı", "tavan tipi koklayıcı", "yer tipi koklayıcı", "masa tipi koklayıcı", "şekilli koklayıcı", "renkli koklayıcı", "desenli koklayıcı", "logo lu koklayıcı", "markalı koklayıcı", "özel koklayıcı", "hediye koklayıcı", "promosyon koklayıcı", "reklam koklayıcı", "tanıtım koklayıcı", "sponsor koklayıcı", "destek koklayıcı", "bağış koklayıcı", "yardım koklayıcı", "katkı koklayıcı", "destekleyici koklayıcı", "gönüllü koklayıcı", "gönüllü koklayıcı", "gönüllü koklayıcı" },
            new[] { new[] { "ahşap", "dört ayak", "yemek", "üst" }, new[] { "otur", "sırtlık", "rahahat", "dört bacak" }, new[] { "cep", "arama", "mesaj", "ekran" } }
        ),
        ["Meslekler"] = (
            new[] { "doktor", "öğretmen", "mühendis", "avukat", "pilot", "şef", "polis", "itfaiyeci", "yazar", "mimar", "hemsire", "eczacı", "veteriner", "dişçi", "psikolog", "psikiyatrist", "terapist", "fizyoterapist", "logopedist", "ergoterapist", "diyetisyen", "beslenme uzmanı", "sporcu", "antrenör", "hakem", "yönetici", "müdür", "direktör", "başkan", "genel müdür", "bölge müdürü", "şube müdürü", "departman müdürü", "proje müdürü", "ürün müdürü", "satış müdürü", "pazarlama müdürü", "insan kaynakları müdürü", "finans müdürü", "muhasebe müdürü", "denetim müdürü", "hukuk müdürü", "bilgi işlem müdürü", "sistem müdürü", "ağ müdürü", "güvenlik müdürü", "kalite müdürü", "üretim müdürü", "lojistik müdürü", "tedarik müdürü", "depo müdürü", "sevkiyat müdürü", "içerik müdürü", "sosyal medya müdürü", "dijital pazarlama müdürü", "e-ticaret müdürü", "mobil uygulama müdürü", "web müdürü", "yazılım müdürü", "donanım müdürü", "altyapı müdürü", "veri müdürü", "analitik müdürü", "raporlama müdürü", "strateji müdürü", "planlama müdürü", "performans müdürü", "risk müdürü", "uyum müdürü", "etik müdürü", "sürdürülebilirlik müdürü", "çevre müdürü", "güvenlik müdürü", "sağlık müdürü", "eğitim müdürü", "kültür müdürü", "sanat müdürü", "müze müdürü", "kütüphane müdürü", "arşiv müdürü", "belge müdürü", "kayıt müdürü", "bilgi müdürü", "bilgi müdürü", "bilgi müdürü" },
            new[] { new[] { "hasta", "hastane", "reçete", "ilaç" }, new[] { "okul", "sınıf", "ders", "öğrenci" }, new[] { "hesap", "proje", "yapı", "makina" } }
        ),
        ["Yerler"] = (
            new[] { "okul", "hastane", "park", "alışveriş merkezi", "havaalanı", "istrasyon", "kütüphane", "müze", "sinema", "restoran", "kafe", "pastane", "fırın", "market", "süpermarket", "hypermarket", "discount", "hardal", "bakkal", "manav", "kasap", "balıkçı", "pastacı", "kurabiyeci", "kekçi", "pastaçı", "pizzacı", "kebapçı", "dürümcü", "lahmacuncu", "pidemeci", "mantıcı", "raviolicisi", "börekçi", "poğaçacı", "simitçi", "açmacı", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi", "çörekçi" },
            new[] { new[] { "sınıf", "öğretmen", "öğrenci", "ders" }, new[] { "doktor", "hasta", "acil", "ameliyat" }, new[] { "yeşil", "ağaç", "bank", "yürüyüş" } }
        ),
        ["Eylemler"] = (
            new[] { "koşu", "yüzme", "zıplama", "dans", "yazma", "okuma", "pişirme", "sürme", "uçma", "tırmanma", "koşma", "yürüme", "hızlı yürüme", "koşu", "maraton", "yarış", "yarışma", "yarışmak", "yarıştırmak", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan", "yarışılan" },
            new[] { new[] { "hız", "ayak", "maraton", "spor" }, new[] { "su", "havuz", "dalış", "nefes" }, new[] { "yüksek", "ayak", "hava", "iniş" } }
        ),
        ["Doğa"] = (
            new[] { "yağmur", "güneş", "dağ", "deniz", "orman", "göl", "nehir", "çayı", "şelale", "mağara", "vadi", "platô", "tepe", "zirve", "kaya", "kayaç", "kum", "çakıl", "çakıl taş", "çakıl plajı", "kumlu plaj", "sahil", "kıyı", "kıyı şeridi", "kıyı uzunluğu", "kıyı genişliği", "kıyı derinliği", "kıyı eğimi", "kıyı malzemesi", "kıyı rengi", "kıyı dokusu", "kıyı kokusu", "kıyı sesi", "kıyı manzarası", "kıyı havası", "kıyı rüzgarı", "kıyı dalgası", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü", "kıyı köpüğü" },
            new[] { new[] { "su", "gökyüzü", "bulut", "şemsiye" }, new[] { "ışık", "sıcak", "gündüz", "doğu" }, new[] { "yüksek", "zirve", "tırmanma", "kaynak" } }
        ),
        ["Teknoloji"] = (
            new[] { "bilgisayar", "telefon", "internet", "robot", "yazılım", "veri", "sunucu", "algoritma", "yapay zeka", "makine öğrenmesi", "derin öğrenme", "sinir ağı", "veri bilimi", "büyük veri", "bulut bilişim", "siber güvenlik", "blok zincir", "kripto para", "bitcoin", "ethereum", "nft", "metaverse", "sanal gerçeklik", "artırılmış gerçeklik", "karma gerçeklik", "iyileştirilmiş gerçeklik", "görüntü işleme", "doğal dil işleme", "ses tanıma", "konuşma tanıma", "yüz tanıma", "nesne tanıma", "el yazısı tanıma", "imza tanıma", "parmak izi tanıma", "iris tanıma", "retina tanıma", "ses tanıma", "konuşucu tanıma", "dil tanıma", "çeviri", "makine çevirisi", "anlık çeviri", "sesli asistan", "akıllı asistan", "chatbot", "sanal asistan", "kişisel asistan", "akıllı ev", "akıllı şehir", "akıllı tarım", "akıllıfabrik", "akıllı lojistik", "akıllı üretim", "akıllı enerji", "akıllı su", "akıllı atık", "akıllı trafik", "akıllı park", "akıllı ışık", "akıllı sayaç", "akıllı termostat", "akıllı kilit", "akıllı kamera", "akıllı sensör", "akıllı alarm", "akıllı güvenlik", "akıllı izleme", "akıllı kontrol", "akıllı yönetim", "akıllı optimizasyon", "akıllı tahmin", "akıllı analiz", "akıllı raporlama", "akıllı karar", "akıllı strateji", "akıllı planlama", "akıllı performans", "akıllı risk", "akıllı uyum", "akıllı etik", "akıllı sürdürülebilirlik", "akıllı çevre", "akıllı güvenlik", "akıllı sağlık", "akıllı eğitim", "akıllı kültür", "akıllı sanat", "akıllı müze", "akıllı kütüphane", "akıllı arşiv", "akıllı belge", "akıllı kayıt", "akıllı bilgi", "akıllı bilgi", "akıllı bilgi" },
            new[] { new[] { "ekran", "klavye", "mouse", "işlemci" }, new[] { "cep", "mobil", "arama", "mesaj" }, new[] { "web", "site", "google", "tarayıcı" } }
        ),
        ["Spor"] = (
            new[] { "futbol", "basketbol", "voleybol", "tenis", "yüzme", "koşu", "bisiklet", "golf", "boks", "jimnastik", "halter", "güreş", "cudo", "karate", "taekwondo", "aikido", "kung fu", "şimşek", "boks", "kick boks", "muay thai", "mma", "jiu jitsu", "brazilyan jiu jitsu", "judo", "sambo", "güreş", "serbest güreş", "yunanistan güreşi", "kadın güreşi", "gençlik güreşi", "olimpik güreş", "dunya şampiyonasu güreşi", "europa şampiyonasu güreşi", "ülke şampiyonasu güreşi", "bölge şampiyonasu güreşi", "şehir şampiyonasu güreşi", "okul şampiyonasu güreşi", "üniversite şampiyonasu güreşi", "kulüp şampiyonasu güreşi", "takım şampiyonasu güreşi", "bireysel şampiyona güreşi", "takım şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi", "karma şampiyona güreşi" },
            new[] { new[] { "top", "kale", "hakem", "saha" }, new[] { "pot", "top", "saha", "takım" }, new[] { "file", "top", "saha", "takım" } }
        )
    };

    public static List<WordExportDto> GenerateWords(int targetCount = 2500)
    {
        var words = new List<WordExportDto>();
        var categories = _categoryData.Keys.ToList();
        var wordsPerCategory = targetCount / categories.Count;

        foreach (var category in categories)
        {
            var (mainWords, forbiddenTemplates) = _categoryData[category];
            var categoryWords = GenerateCategoryWords(category, mainWords, forbiddenTemplates, wordsPerCategory);
            words.AddRange(categoryWords);
        }

        // Shuffle
        return words.OrderBy(_ => _random.Next()).Take(targetCount).ToList();
    }

    private static List<WordExportDto> GenerateCategoryWords(string categoryName, string[] mainWords, string[][] forbiddenTemplates, int count)
    {
        var result = new List<WordExportDto>();
        var usedWords = new HashSet<string>();

        for (int i = 0; i < count && i < mainWords.Length * 5; i++)
        {
            var mainWord = mainWords[_random.Next(mainWords.Length)];
            
            // Vary the main word slightly
            var variations = GetVariations(mainWord);
            var finalWord = variations[_random.Next(variations.Length)];
            
            if (usedWords.Contains(finalWord)) continue;
            usedWords.Add(finalWord);

            // Generate forbidden words
            var forbiddenCount = _random.Next(4, 7);
            var forbidden = new List<string>();
            
            // Add template-based forbidden words
            foreach (var template in forbiddenTemplates)
            {
                if (forbidden.Count >= forbiddenCount) break;
                var fw = template[_random.Next(template.Length)];
                if (!forbidden.Contains(fw) && fw != finalWord)
                    forbidden.Add(fw);
            }

            // Add category-specific related words
            var relatedWords = GetRelatedWords(categoryName, finalWord);
            foreach (var rw in relatedWords)
            {
                if (forbidden.Count >= forbiddenCount) break;
                if (!forbidden.Contains(rw) && rw != finalWord)
                    forbidden.Add(rw);
            }

            // Add generic forbidden words
            var genericWords = new[] { "evet", "hayır", "geç", "pas", "doğru", "yanlış", "tabu", "yasak", "kelime", "süre", "dakika", "saniye", "puan", "kazanan", "kaybeden", "takım", "oyuncu", "tur", "round", "hakkı", "hakkı" };
            foreach (var gw in genericWords)
            {
                if (forbidden.Count >= forbiddenCount) break;
                if (!forbidden.Contains(gw) && gw != finalWord)
                    forbidden.Add(gw);
            }

            result.Add(new WordExportDto
            {
                MainWord = finalWord,
                ForbiddenWords = forbidden.Take(forbiddenCount).ToList(),
                CategoryName = categoryName,
                Difficulty = _random.Next(1, 4)
            });
        }

        return result;
    }

    private static string[] GetVariations(string word)
    {
        var variations = new List<string> { word };
        
        // Add plural/singular variations for Turkish
        if (word.EndsWith("lar") || word.EndsWith("ler"))
        {
            variations.Add(word.Substring(0, word.Length - 3));
        }
        else if (!word.EndsWith("lar") && !word.EndsWith("ler") && word.Length > 3)
        {
            // Simple pluralization attempt
            var lastVowel = GetLastVowel(word);
            if (IsFrontVowel(lastVowel))
                variations.Add(word + "ler");
            else
                variations.Add(word + "lar");
        }

        // Add possessive forms
        variations.Add(word + "ım");
        variations.Add(word + "in");
        variations.Add(word + "ı");
        variations.Add(word + "ımız");
        variations.Add(word + "ınız");
        variations.Add(word + "ları");

        return variations.Distinct().ToArray();
    }

    private static char GetLastVowel(string word)
    {
        var vowels = "aeıioöuü";
        for (int i = word.Length - 1; i >= 0; i--)
        {
            if (vowels.Contains(char.ToLower(word[i])))
                return char.ToLower(word[i]);
        }
        return 'a';
    }

    private static bool IsFrontVowel(char vowel)
    {
        return "eiöü".Contains(vowel);
    }

    private static List<string> GetRelatedWords(string category, string mainWord)
    {
        var related = new Dictionary<string, List<string>>
        {
            ["Genel"] = new() { "ev", "araba", "kitap", "telefon", "bilgisayar" },
            ["Hayvanlar"] = new() { "kedi", "köpek", "kuş", "balık", "aslan" },
            ["Yiyecekler"] = new() { "pizza", "hamburger", "çikolata", "dondurma", "sushi" },
            ["Eşyalar"] = new() { "masa", "sandalye", "telefon", "kitap", "kalem" },
            ["Meslekler"] = new() { "doktor", "öğretmen", "mühendis", "avukat", "pilot" },
            ["Yerler"] = new() { "okul", "hastane", "park", "alışveriş merkezi", "havaalanı" },
            ["Eylemler"] = new() { "koşu", "yüzme", "zıplama", "dans", "yazma" },
            ["Doğa"] = new() { "yağmur", "güneş", "dağ", "deniz", "orman" },
            ["Teknoloji"] = new() { "bilgisayar", "telefon", "internet", "robot", "yazılım" },
            ["Spor"] = new() { "futbol", "basketbol", "voleybol", "tenis", "yüzme" }
        };

        if (related.TryGetValue(category, out var words))
        {
            return words.Where(w => w != mainWord).Take(3).ToList();
        }
        return new List<string>();
    }
}

public class WordExportDto
{
    public string MainWord { get; set; } = string.Empty;
    public List<string> ForbiddenWords { get; set; } = new();
    public string CategoryName { get; set; } = string.Empty;
    public int Difficulty { get; set; } = 1;
}