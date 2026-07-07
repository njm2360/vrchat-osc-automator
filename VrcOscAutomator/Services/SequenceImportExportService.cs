using System.Reflection;
using System.Text.Json;
using VrcOscAutomator.Exceptions;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class SequenceImportExportService : ISequenceImportExportService
{
    public const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private static readonly string AppVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public string Export(string name, IEnumerable<SequenceSlot> slots, bool isLoopMode)
        => JsonSerializer.Serialize(
            new ProfileExportData(name, slots.ToList(), isLoopMode, CurrentSchemaVersion, AppVersion),
            Options);

    public ProfileExportData? Import(string input)
    {
        using (var document = JsonDocument.Parse(input))
        {
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("schemaVersion", out var schemaVersionElement)
                && schemaVersionElement.ValueKind == JsonValueKind.Number
                && schemaVersionElement.TryGetInt32(out int schemaVersion)
                && schemaVersion > CurrentSchemaVersion)
            {
                throw new UnsupportedSchemaVersionException(schemaVersion);
            }
        }

        var data = JsonSerializer.Deserialize<ProfileExportData>(input, Options);
        if (data is not null)
            ValidateSlots(data.Slots);
        return data;
    }

    private static void ValidateSlots(IEnumerable<SequenceSlot> slots)
    {
        foreach (var slot in slots)
        {
            if (slot is null)
                throw new JsonException("slots must not contain null.");

            if (slot is OscSlot { Address: null })
                throw new JsonException("address must not be null.");

            if (slot is FloatSlot { TransitionMode: not TransitionMode.None } f && (f.TransitionFromValue is null || f.TransitionToValue is null)
             || slot is IntSlot { TransitionMode: not TransitionMode.None } n && (n.TransitionFromValue is null || n.TransitionToValue is null))
                throw new JsonException(
                    "transitionFromValue and transitionToValue are required when transitionMode is not None.");

            if (slot is RandomWaitSlot rw && (rw.MinMs < 0 || rw.MaxMs < rw.MinMs))
                throw new JsonException(
                    "random_wait requires minMs >= 0 and maxMs >= minMs.");
        }
    }
}
