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
    };

    public string Export(IEnumerable<SequenceSlot> slots)
        => JsonSerializer.Serialize(slots.ToList(), Options);

    public IReadOnlyList<SequenceSlot>? Import(string input)
        => JsonSerializer.Deserialize<List<SequenceSlot>>(input, Options);
}
