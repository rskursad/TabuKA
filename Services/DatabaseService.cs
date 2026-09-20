using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TabuKA.Data;
using TabuKA.Entities;

namespace TabuKA.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<bool> IsDatabaseInitializedAsync();
    Task BackupAsync(string filePath);
    Task RestoreAsync(string filePath);
    Task ResetAsync();
    Task SeedWordsAsync(List<WordImportDto> words);
    Task<int> GetWordCountAsync();
    Task<int> GetCategoryCountAsync();
    Task<List<Category>> GetCategoriesAsync();
    Task<List<Word>> GetWordsByCategoryAsync(int categoryId);
    Task<List<Word>> GetRandomWordsAsync(int count, List<int>? categoryIds = null);
    Task<List<Word>> SearchWordsAsync(string? searchText, int? categoryId);
    Task ExportToJsonAsync(string filePath);
    Task ImportFromJsonAsync(string filePath);
    Task<int> SeedFromSeedDataAsync();
    Task UpdateGameSettingsAsync(GameSettings settings);
    Task AddWordAsync(Word word);
    Task UpdateWordAsync(Word word);
    Task DeleteWordAsync(int wordId);
    Task AddCategoryAsync(string name, string? description);
    Task DeleteCategoryAsync(int categoryId);
}

public class WordImportDto
{
    public string MainWord { get; set; } = string.Empty;
    public List<string> ForbiddenWords { get; set; } = new();
    public string CategoryName { get; set; } = string.Empty;
    public int Difficulty { get; set; } = 1;
}

public class DatabaseService : IDatabaseService
{
    private readonly IDbContextFactory<TabuKADbContext> _dbContextFactory;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IDbContextFactory<TabuKADbContext> dbContextFactory, ILogger<DatabaseService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        await context.Database.MigrateAsync();
        await SeedDefaultDataAsync(context);
        _logger.LogInformation("Veritabanı başlatıldı ve migrasyonlar uygulandı");
    }

    private async Task SeedDefaultDataAsync(TabuKADbContext context)
    {
        // Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Genel", Description = "Genel kelimeler", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 2, Name = "Hayvanlar", Description = "Hayvanlarla ilgili kelimeler", SortOrder = 2, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 3, Name = "Yiyecekler", Description = "Yiyecek ve içeceklerle ilgili kelimeler", SortOrder = 3, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 4, Name = "Eşyalar", Description = "Günlük eşyalar", SortOrder = 4, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 5, Name = "Meslekler", Description = "Meslek ve işlerle ilgili kelimeler", SortOrder = 5, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 6, Name = "Yerler", Description = "Yer ve mekanlarla ilgili kelimeler", SortOrder = 6, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 7, Name = "Eylemler", Description = "Fiiller ve eylemler", SortOrder = 7, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 8, Name = "Doğa", Description = "Doğa olayları ve öğeleri", SortOrder = 8, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 9, Name = "Teknoloji", Description = "Teknoloji ve bilgisayar terimleri", SortOrder = 9, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Category { Id = 10, Name = "Spor", Description = "Spor dalları ve terimleri", SortOrder = 10, IsActive = true, CreatedAt = new DateTime(2024, 1, 1) }
            };
            context.Categories.AddRange(categories);
        }

        // Seed GameSettings
        if (!await context.GameSettings.AnyAsync())
        {
            var settings = new List<GameSettings>
            {
                new GameSettings
                {
                    Id = 1,
                    Name = "Varsayılan",
                    RoundTimeSeconds = 60,
                    PassLimit = 3,
                    TeamCount = 2,
                    ScoreToWin = 0,
                    UseCustomCategories = false,
                    EnableSound = true,
                    EnableAutoTabooCheck = false,
                    Volume = 80,
                    IsDefault = true,
                    CreatedAt = new DateTime(2024, 1, 1)
                },
                new GameSettings
                {
                    Id = 2,
                    Name = "Hızlı Oyun",
                    RoundTimeSeconds = 30,
                    PassLimit = 1,
                    TeamCount = 2,
                    ScoreToWin = 10,
                    UseCustomCategories = false,
                    EnableSound = true,
                    EnableAutoTabooCheck = false,
                    Volume = 80,
                    IsDefault = false,
                    CreatedAt = new DateTime(2024, 1, 1)
                },
                new GameSettings
                {
                    Id = 3,
                    Name = "Uzun Oyun",
                    RoundTimeSeconds = 90,
                    PassLimit = 5,
                    TeamCount = 2,
                    ScoreToWin = 25,
                    UseCustomCategories = false,
                    EnableSound = true,
                    EnableAutoTabooCheck = false,
                    Volume = 80,
                    IsDefault = false,
                    CreatedAt = new DateTime(2024, 1, 1)
                }
            };
            context.GameSettings.AddRange(settings);
        }

        // Seed AppSettings
        if (!await context.AppSettings.AnyAsync())
        {
            var appSettings = new List<AppSettings>
            {
                new AppSettings { Id = 1, Key = "Theme", Value = "System", Description = "Uygulama teması (Light/Dark/System)", Type = SettingType.String, CreatedAt = new DateTime(2024, 1, 1) },
                new AppSettings { Id = 2, Key = "Language", Value = "tr-TR", Description = "Uygulama dili", Type = SettingType.String, CreatedAt = new DateTime(2024, 1, 1) },
                new AppSettings { Id = 3, Key = "MasterVolume", Value = "80", Description = "Genel ses seviyesi (0-100)", Type = SettingType.Integer, CreatedAt = new DateTime(2024, 1, 1) },
                new AppSettings { Id = 4, Key = "AutoSave", Value = "true", Description = "Otomatik kaydetme aktif", Type = SettingType.Boolean, CreatedAt = new DateTime(2024, 1, 1) },
                new AppSettings { Id = 5, Key = "ShowTutorial", Value = "true", Description = "İlk açılışta öğretici göster", Type = SettingType.Boolean, CreatedAt = new DateTime(2024, 1, 1) },
                new AppSettings { Id = 6, Key = "DatabaseVersion", Value = "1", Description = "Veritabanı şema sürümü", Type = SettingType.Integer, CreatedAt = new DateTime(2024, 1, 1) }
            };
            context.AppSettings.AddRange(appSettings);
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> IsDatabaseInitializedAsync()
    {
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task BackupAsync(string filePath)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        var dbPath = connection.DataSource;

        if (File.Exists(dbPath))
        {
            File.Copy(dbPath, filePath, true);
            _logger.LogInformation("Veritabanı yedeklendi: {Path}", filePath);
        }
        else
        {
            throw new FileNotFoundException("Veritabanı dosyası bulunamadı", dbPath);
        }
    }

    public async Task RestoreAsync(string filePath)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        var dbPath = connection.DataSource;

        await context.Database.CloseConnectionAsync();

        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        File.Copy(filePath, dbPath, true);
        _logger.LogInformation("Veritabanı geri yüklendi: {Path}", filePath);
    }

    public async Task ResetAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        await SeedDefaultDataAsync(context);
        await SeedFromSeedDataAsync();
        _logger.LogInformation("Veritabanı sıfırlandı ve yeniden oluşturuldu");
    }

    public async Task SeedWordsAsync(List<WordImportDto> words)
    {
        using var context = _dbContextFactory.CreateDbContext();
        
        var categories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
        
        foreach (var wordDto in words)
        {
            if (!categories.TryGetValue(wordDto.CategoryName, out var categoryId))
            {
                var newCategory = new Category
                {
                    Name = wordDto.CategoryName,
                    Description = $"Otomatik eklenen kategori: {wordDto.CategoryName}",
                    IsActive = true
                };
                context.Categories.Add(newCategory);
                await context.SaveChangesAsync();
                categoryId = newCategory.Id;
                categories[wordDto.CategoryName] = categoryId;
            }

            var word = new Word
            {
                MainWord = wordDto.MainWord,
                ForbiddenWords = wordDto.ForbiddenWords,
                CategoryId = categoryId,
                Difficulty = wordDto.Difficulty,
                IsActive = true
            };

            context.Words.Add(word);
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("{Count} kelime veritabanına eklendi", words.Count);
    }

    public async Task<int> GetWordCountAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Words.CountAsync(w => w.IsActive);
    }

    public async Task<int> GetCategoryCountAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Categories.CountAsync(c => c.IsActive);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }

    public async Task<List<Word>> GetWordsByCategoryAsync(int categoryId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Words
            .Where(w => w.CategoryId == categoryId && w.IsActive)
            .ToListAsync();
    }

    public async Task<List<Word>> GetRandomWordsAsync(int count, List<int>? categoryIds = null)
    {
        using var context = _dbContextFactory.CreateDbContext();
        
        var query = context.Words.Where(w => w.IsActive);
        
        if (categoryIds != null && categoryIds.Count > 0)
        {
            query = query.Where(w => categoryIds.Contains(w.CategoryId));
        }

        var words = await query.ToListAsync();
        
        var random = new Random();
        return words.OrderBy(_ => random.Next()).Take(count).ToList();
    }

    public async Task<List<Word>> SearchWordsAsync(string? searchText, int? categoryId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        var query = context.Words.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var normalized = searchText.Trim();
            query = query.Where(w => w.MainWord.Contains(normalized));
        }

        if (categoryId is > 0)
        {
            query = query.Where(w => w.CategoryId == categoryId);
        }

        return await query
            .OrderBy(w => w.MainWord)
            .Take(500)
            .ToListAsync();
    }

    public async Task ExportToJsonAsync(string filePath)
    {
        using var context = _dbContextFactory.CreateDbContext();
        
        var exportData = new
        {
            Categories = await context.Categories.Where(c => c.IsActive).ToListAsync(),
            Words = await context.Words.Where(w => w.IsActive).ToListAsync(),
            GameSettings = await context.GameSettings.ToListAsync(),
            ExportDate = DateTime.UtcNow,
            Version = 1
        };

        var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await File.WriteAllTextAsync(filePath, json);
        _logger.LogInformation("Veriler JSON olarak dışa aktarıldı: {Path}", filePath);
    }

    public async Task ImportFromJsonAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var importData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);

        using var context = _dbContextFactory.CreateDbContext();
        
        if (importData.TryGetProperty("Categories", out var categoriesElement))
        {
            var categories = System.Text.Json.JsonSerializer.Deserialize<List<Category>>(categoriesElement.GetRawText());
            if (categories != null)
            {
                foreach (var category in categories)
                {
                    var existing = await context.Categories.FirstOrDefaultAsync(c => c.Name == category.Name);
                    if (existing == null)
                    {
                        context.Categories.Add(category);
                    }
                }
            }
        }

        if (importData.TryGetProperty("Words", out var wordsElement))
        {
            var words = System.Text.Json.JsonSerializer.Deserialize<List<Word>>(wordsElement.GetRawText());
            if (words != null)
            {
                foreach (var word in words)
                {
                    var existing = await context.Words.FirstOrDefaultAsync(w => w.MainWord == word.MainWord && w.CategoryId == word.CategoryId);
                    if (existing == null)
                    {
                        context.Words.Add(word);
                    }
                }
            }
        }

        if (importData.TryGetProperty("GameSettings", out var settingsElement))
        {
            var settings = System.Text.Json.JsonSerializer.Deserialize<List<GameSettings>>(settingsElement.GetRawText());
            if (settings != null)
            {
                foreach (var setting in settings)
                {
                    var existing = await context.GameSettings.FirstOrDefaultAsync(s => s.Name == setting.Name);
                    if (existing == null)
                    {
                        context.GameSettings.Add(setting);
                    }
                }
            }
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Veriler JSON'dan içe aktarıldı: {Path}", filePath);
    }

    /// <summary>
    /// Reads all bundled seed files (initial_words.json, packs/*.json, external_words.json)
    /// and imports words that do not already exist.
    /// </summary>
    public async Task<int> SeedFromSeedDataAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();

        var categories = (await context.Categories.ToListAsync()).ToDictionary(c => c.Name, c => c.Id);

        var seedRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData");
        if (!Directory.Exists(seedRoot))
        {
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "Data", "SeedData");
            if (Directory.Exists(fallback))
                seedRoot = fallback;
        }
        if (!Directory.Exists(seedRoot))
            return 0;

        var packDir = Path.Combine(seedRoot, "packs");
        var files = new List<string>
        {
            Path.Combine(seedRoot, "initial_words.json"),
            Path.Combine(seedRoot, "external_words.json")
        };

        if (Directory.Exists(packDir))
        {
            files.AddRange(Directory.GetFiles(packDir, "*.json"));
        }

        var imported = 0;
        var wordsToAdd = new List<Word>();

        foreach (var filePath in files.Where(File.Exists))
        {
            var wordsInFile = ParseSeedFile(filePath, categories);
            wordsToAdd.AddRange(wordsInFile);
        }

        var existingKeys = new HashSet<(string, int)>();
        foreach (var w in await context.Words.Select(w => new { w.MainWord, w.CategoryId }).ToListAsync())
        {
            existingKeys.Add((w.MainWord, w.CategoryId));
        }

        foreach (var word in wordsToAdd.Where(w => w != null))
        {
            if (existingKeys.Contains((word.MainWord, word.CategoryId)))
                continue;

            context.Words.Add(word);
            existingKeys.Add((word.MainWord, word.CategoryId));
            imported++;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Seed verilerinden {Count} yeni kelime eklendi", imported);
        return imported;
    }

    private List<Word> ParseSeedFile(string filePath, Dictionary<string, int> categories)
    {
        var result = new List<Word>();
        var json = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("word", out var wordEl))
                {
                    var word = CreateWord(wordEl.GetString() ?? "", ParseForbidden(item), "Genel", TryGetInt(item, "difficulty", 1), categories);
                    if (word != null) result.Add(word);
                }
            }
            return result;
        }

        if (document.RootElement.TryGetProperty("categoryName", out var categoryEl))
        {
            var categoryName = categoryEl.GetString() ?? "Genel";
            if (document.RootElement.TryGetProperty("words", out var wordsEl))
            {
                foreach (var item in wordsEl.EnumerateArray())
                {
                    if (item.TryGetProperty("mainWord", out var mainEl))
                    {
                        var word = CreateWord(mainEl.GetString() ?? "", ParseForbidden(item), categoryName, TryGetInt(item, "difficulty", 1), categories);
                        if (word != null) result.Add(word);
                    }
                }
            }
            return result;
        }

        if (document.RootElement.TryGetProperty("words", out var mixedWordsEl))
        {
            foreach (var item in mixedWordsEl.EnumerateArray())
            {
                if (item.TryGetProperty("mainWord", out var mainEl))
                {
                    var categoryName = item.TryGetProperty("categoryName", out var cn) ? cn.GetString() ?? "Genel" : "Genel";
                    var word = CreateWord(mainEl.GetString() ?? "", ParseForbidden(item), categoryName, TryGetInt(item, "difficulty", 1), categories);
                    if (word != null) result.Add(word);
                }
            }
        }

        return result;
    }

    private Word? CreateWord(string mainWord, List<string> forbidden, string categoryName, int difficulty, Dictionary<string, int> categories)
    {
        if (string.IsNullOrWhiteSpace(mainWord))
            return null;

        if (!categories.TryGetValue(categoryName, out var categoryId))
        {
            var existing = categories.Keys.FirstOrDefault(k => string.Equals(k, categoryName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                categoryId = categories[existing];
            }
            else
            {
                var category = new Category
                {
                    Name = categoryName,
                    Description = $"Otomatik eklenen kategori: {categoryName}",
                    IsActive = true,
                    SortOrder = categories.Count + 1
                };
                using var tempContext = _dbContextFactory.CreateDbContext();
                tempContext.Categories.Add(category);
                tempContext.SaveChanges();
                categoryId = category.Id;
                categories[categoryName] = categoryId;
            }
        }

        return new Word
        {
            MainWord = mainWord,
            ForbiddenWords = forbidden,
            CategoryId = categoryId,
            Difficulty = difficulty,
            IsActive = true
        };
    }

public async Task UpdateGameSettingsAsync(GameSettings settings)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var existing = await context.GameSettings.FirstOrDefaultAsync(s => s.Id == settings.Id);
        if (existing == null)
            return;

        existing.SelectedCategoryIdsJson = settings.SelectedCategoryIdsJson;
        existing.TeamCount = settings.TeamCount;
        existing.UseCustomCategories = settings.UseCustomCategories;
        existing.RoundTimeSeconds = settings.RoundTimeSeconds;
        existing.PassLimit = settings.PassLimit;
        existing.ScoreToWin = settings.ScoreToWin;
        existing.EnableAutoTabooCheck = settings.EnableAutoTabooCheck;
        existing.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        _logger.LogInformation("Oyun ayarları güncellendi: {Name}", settings.Name);
    }

    private static List<string> ParseForbidden(System.Text.Json.JsonElement item)
    {
        if (item.TryGetProperty("forbiddenWords", out var fw) || item.TryGetProperty("forbidden_words", out fw))
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(fw.GetRawText()) ?? new List<string>();
            return list.Select(w => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(w.ToLowerInvariant())).ToList();
        }
        return new List<string>();
    }

    private static int TryGetInt(System.Text.Json.JsonElement item, string prop, int defaultValue)
    {
        return item.TryGetProperty(prop, out var el) && el.TryGetInt32(out var val) ? val : defaultValue;
    }

    public async Task AddWordAsync(Word word)
    {
        using var context = _dbContextFactory.CreateDbContext();
        word.CreatedAt = DateTime.UtcNow;
        word.IsActive = true;
        context.Words.Add(word);
        await context.SaveChangesAsync();
        _logger.LogInformation("Kelime eklendi: {Word}", word.MainWord);
    }

    public async Task UpdateWordAsync(Word word)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var existing = await context.Words.FirstOrDefaultAsync(w => w.Id == word.Id);
        if (existing == null)
            return;

        existing.MainWord = word.MainWord;
        existing.ForbiddenWordsJson = word.ForbiddenWordsJson;
        existing.CategoryId = word.CategoryId;
        existing.Difficulty = word.Difficulty;
        existing.IsActive = word.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        _logger.LogInformation("Kelime güncellendi: {Word}", word.MainWord);
    }

    public async Task DeleteWordAsync(int wordId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var existing = await context.Words.FirstOrDefaultAsync(w => w.Id == wordId);
        if (existing == null)
            return;

        context.Words.Remove(existing);
        await context.SaveChangesAsync();
        _logger.LogInformation("Kelime silindi: {Word}", existing.MainWord);
    }

    public async Task AddCategoryAsync(string name, string? description)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var existing = await context.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (existing != null)
            return;

        var maxSort = await context.Categories.AnyAsync() ? await context.Categories.MaxAsync(c => c.SortOrder) : 0;
        context.Categories.Add(new Category
        {
            Name = name,
            Description = description ?? string.Empty,
            IsActive = true,
            SortOrder = maxSort + 1,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        _logger.LogInformation("Kategori eklendi: {Name}", name);
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var existing = await context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (existing == null)
            return;

        context.Categories.Remove(existing);
        await context.SaveChangesAsync();
        _logger.LogInformation("Kategori silindi: {Name}", existing.Name);
    }
}