using System.Text.Json.Serialization;

namespace VrcOscAutomator.Models;

public record ProfileExportData(
    [property: JsonRequired] string Name,
    [property: JsonRequired] List<SequenceSlot> Slots,
    [property: JsonRequired] bool IsLoopMode,
    int? SchemaVersion = null,
    string? AppVersion = null);
