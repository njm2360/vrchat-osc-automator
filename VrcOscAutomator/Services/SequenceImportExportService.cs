using System.Text;
using System.Text.Json;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class SequenceImportExportService : ISequenceImportExportService
{
    private static readonly JsonSerializerOptions _jsonOptions = new();

    public string Export(IEnumerable<SequenceSlot> slots)
    {
        string json = JsonSerializer.Serialize(slots.ToList(), _jsonOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public IReadOnlyList<SequenceSlot>? Import(string base64)
    {
        byte[] bytes = Convert.FromBase64String(base64);
        string json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<List<SequenceSlot>>(json, _jsonOptions);
    }
}
