using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TabuKA.Data;
using TabuKA.Entities;

namespace TabuKA.Services;

public interface ISettingsService
{
    Task<T> GetSettingAsync<T>(string key, T defaultValue = default!);
    Task SetSettingAsync<T>(string key, T value);
    Task<AppSettings?> GetAppSettingAsync(string key);
    Task<List<AppSettings>> GetAllSettingsAsync();
    Task ResetToDefaultsAsync();
    Task<string> GetThemeAsync();
    Task SetThemeAsync(string theme);
    Task<int> GetMasterVolumeAsync();
    Task SetMasterVolumeAsync(int volume);
}

public class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<TabuKADbContext> _dbContextFactory;
    private readonly ILogger<SettingsService> _logger;
    private readonly Dictionary<string, object> _cache = new();

    public SettingsService(IDbContextFactory<TabuKADbContext> dbContextFactory, ILogger<SettingsService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default!)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            return (T)cached;
        }

        using var context = _dbContextFactory.CreateDbContext();
        var setting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            return defaultValue;
        }

        var value = ConvertSettingValue<T>(setting.Value, setting.Type);
        _cache[key] = value!;
        return value!;
    }

    public async Task SetSettingAsync<T>(string key, T value)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var setting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);

        var (stringValue, type) = ConvertToSettingValue(value);

        if (setting == null)
        {
            setting = new AppSettings
            {
                Key = key,
                Value = stringValue,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };
            context.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = stringValue;
            setting.Type = type;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        _cache[key] = value!;
        _logger.LogInformation("Ayar güncellendi: {Key} = {Value}", key, value);
    }

    public async Task<AppSettings?> GetAppSettingAsync(string key)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<List<AppSettings>> GetAllSettingsAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.AppSettings.ToListAsync();
    }

    public async Task ResetToDefaultsAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        await context.AppSettings.ExecuteDeleteAsync();
        _cache.Clear();
        _logger.LogInformation("Tüm ayarlar varsayılana sıfırlandı");
    }

    public async Task<string> GetThemeAsync()
    {
        return await GetSettingAsync("Theme", "System");
    }

    public async Task SetThemeAsync(string theme)
    {
        await SetSettingAsync("Theme", theme);
    }

    public async Task<int> GetMasterVolumeAsync()
    {
        return await GetSettingAsync("MasterVolume", 80);
    }

    public async Task SetMasterVolumeAsync(int volume)
    {
        await SetSettingAsync("MasterVolume", Math.Clamp(volume, 0, 100));
    }

    private T ConvertSettingValue<T>(string value, SettingType type)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        if (typeof(T) == typeof(int) && int.TryParse(value, out var intVal))
        {
            return (T)(object)intVal;
        }

        if (typeof(T) == typeof(bool) && bool.TryParse(value, out var boolVal))
        {
            return (T)(object)boolVal;
        }

        if (typeof(T) == typeof(double) && double.TryParse(value, out var doubleVal))
        {
            return (T)(object)doubleVal;
        }

        if (typeof(T) == typeof(List<int>) && type == SettingType.Json)
        {
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<int>>(value);
            return (T)(object)(deserialized ?? new List<int>());
        }

        return default!;
    }

    private (string StringValue, SettingType Type) ConvertToSettingValue<T>(T value)
    {
        if (value == null)
        {
            return ("", SettingType.String);
        }

        var type = typeof(T);
        
        if (type == typeof(string))
        {
            return (value.ToString()!, SettingType.String);
        }

        if (type == typeof(int))
        {
            return (value.ToString()!, SettingType.Integer);
        }

        if (type == typeof(bool))
        {
            return (value.ToString()!.ToLowerInvariant(), SettingType.Boolean);
        }

        if (type == typeof(double))
        {
            return (value.ToString()!, SettingType.Double);
        }

        if (type == typeof(List<int>))
        {
            return (System.Text.Json.JsonSerializer.Serialize(value), SettingType.Json);
        }

        return (System.Text.Json.JsonSerializer.Serialize(value), SettingType.Json);
    }
}