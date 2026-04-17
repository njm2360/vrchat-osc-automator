using System.IO;
using System.Reflection;
using System.Text.Json;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Assembly.GetExecutingAssembly().GetName().Name!,
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    // V1 デシリアライズ用
    private static readonly JsonSerializerOptions LegacyJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new AppSettings();

        string raw = await File.ReadAllTextAsync(FilePath);

        if (GetVersion(raw) >= 2)
        {
            return JsonSerializer.Deserialize<AppSettings>(raw, JsonOptions) ?? new AppSettings();
        }

        // V1 → V2 マイグレーション
        var legacy = JsonSerializer.Deserialize<LegacyAppSettings>(raw, LegacyJsonOptions);
        if (legacy is null)
            return new AppSettings();

        var migrated = new AppSettings
        {
            Targets = legacy.Targets,
            Hotkeys = legacy.Hotkeys,
            Profiles = legacy.Profiles.Select(p => new Profile
            {
                Name = p.Name,
                IsLoopMode = legacy.IsLoopMode,
                Slots = p.Slots.Select(MigrateLegacySlot).ToList(),
            }).ToList(),
        };

        await SaveAsync(migrated);
        return migrated;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await using FileStream fs = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(fs, settings, JsonOptions);
    }

    private static int GetVersion(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("version", out var v))
                return v.GetInt32();
        }
        catch { }
        return 0;
    }

    private static SequenceSlot MigrateLegacySlot(LegacySlot s) => s.SlotType switch
    {
        1 => new LoopBeginSlot(s.RepeatCount),
        2 => new LoopEndSlot(),
        _ => s.Address is null
             ? new WaitSlot(s.DurationMs)
             : s.ValueType switch
             {
                 1 => new IntSlot(s.Address, (int)s.Value, s.DurationMs, s.ResetOnComplete),
                 2 => new BoolSlot(s.Address, s.Value != 0f, s.DurationMs, s.ResetOnComplete),
                 3 => new StringSlot(s.Address, s.StringValue, s.DurationMs, s.ResetOnComplete),
                 _ => new FloatSlot(s.Address, s.Value, s.DurationMs, s.ResetOnComplete),
             },
    };

    // ── V1 レガシー型 ─────────────────────────────────────────────────────

    private sealed class LegacyAppSettings
    {
        public List<OscTarget> Targets { get; set; } = [];
        public List<LegacyProfile> Profiles { get; set; } = [];
        public HotkeySettings Hotkeys { get; set; } = new();
        public bool IsLoopMode { get; set; }
    }

    private sealed class LegacyProfile
    {
        public string Name { get; set; } = string.Empty;
        public List<LegacySlot> Slots { get; set; } = [];
    }

    private sealed record LegacySlot
    {
        public string? Address { get; init; }
        public float Value { get; init; }
        public string StringValue { get; init; } = string.Empty;
        public int ValueType { get; init; }        // 0=Float, 1=Int, 2=Bool, 3=String
        public int DurationMs { get; init; } = 500;
        public bool ResetOnComplete { get; init; } = true;
        public int SlotType { get; init; }         // 0=Normal, 1=LoopBegin, 2=LoopEnd
        public int RepeatCount { get; init; } = 2;
    }
}
