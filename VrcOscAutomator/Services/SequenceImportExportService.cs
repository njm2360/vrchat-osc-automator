using System.Text.Json;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class SequenceImportExportService : ISequenceImportExportService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Export(string name, IEnumerable<SequenceSlot> slots, bool isLoopMode)
        => JsonSerializer.Serialize(new ProfileExportData(name, slots.ToList(), isLoopMode), Options);

    public ProfileExportData? Import(string input)
    {
        var data = JsonSerializer.Deserialize<ProfileExportData>(input, Options);
        return data?.Slots is { Count: > 0 } ? data : null;
    }
}
