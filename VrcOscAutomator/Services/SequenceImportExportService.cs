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
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public string Export(string name, IEnumerable<SequenceSlot> slots, bool isLoopMode)
        => JsonSerializer.Serialize(new ProfileExportData(name, slots.ToList(), isLoopMode), Options);

    public ProfileExportData? Import(string input)
    {
        var data = JsonSerializer.Deserialize<ProfileExportData>(input, Options);
        if (data is not null)
            ValidateSlots(data.Slots);
        return data;
    }

    private static void ValidateSlots(IEnumerable<SequenceSlot> slots)
    {
        foreach (var slot in slots)
        {
            if (slot is FloatSlot { TransitionMode: not TransitionMode.None } f && (f.TransitionFromValue is null || f.TransitionToValue is null)
             || slot is IntSlot { TransitionMode: not TransitionMode.None } n && (n.TransitionFromValue is null || n.TransitionToValue is null))
                throw new JsonException(
                    "transitionFromValue and transitionToValue are required when transitionMode is not None.");
        }
    }
}
