using System.Text;
using System.Text.Json;
using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.Services;
using Xunit;

namespace VrcOscAutomator.Tests.Services;

public class SequenceImportExportServiceTests
{
    private readonly SequenceImportExportService _sut = new();

    // ─── Export ────────────────────────────────────────────────────────────

    [Fact]
    public void Export_ReturnsValidBase64()
    {
        var slots = new[] { new SequenceSlot { Address = "/test", Value = 0.5f } };

        string result = _sut.Export(slots);

        Convert.TryFromBase64String(result, new byte[result.Length], out _).Should().BeTrue();
    }

    [Fact]
    public void Export_EmptyList_ReturnsBase64EncodedEmptyArray()
    {
        string result = _sut.Export([]);

        string json = Encoding.UTF8.GetString(Convert.FromBase64String(result));
        json.Should().Be("[]");
    }

    [Fact]
    public void Export_PreservesAllFields()
    {
        var slot = new SequenceSlot
        {
            Address = "/input/Vertical",
            Value = 1.0f,
            StringValue = "hello",
            ValueType = OscValueType.Float,
            DurationMs = 300,
            ResetOnComplete = false,
            SlotType = SlotType.Normal,
            RepeatCount = 3,
        };

        string base64 = _sut.Export([slot]);
        IReadOnlyList<SequenceSlot>? imported = _sut.Import(base64);

        imported.Should().ContainSingle().Which.Should().BeEquivalentTo(slot);
    }

    // ─── Import ────────────────────────────────────────────────────────────

    [Fact]
    public void Import_RoundTrip_ReturnsIdenticalSlots()
    {
        var slots = new[]
        {
            new SequenceSlot { Address = "/input/Jump",    Value = 1f,  ValueType = OscValueType.Int },
            new SequenceSlot { Address = "/input/Voice",   Value = 0f,  ValueType = OscValueType.Int },
            new SequenceSlot { Address = null,             DurationMs = 1000 }, // 待機
        };

        string base64 = _sut.Export(slots);
        IReadOnlyList<SequenceSlot>? result = _sut.Import(base64);

        result.Should().BeEquivalentTo(slots);
    }

    [Fact]
    public void Import_LoopMarkers_RoundTrip()
    {
        var slots = new[]
        {
            new SequenceSlot { SlotType = SlotType.LoopBegin, RepeatCount = 5 },
            new SequenceSlot { Address = "/input/Jump", Value = 1f, ValueType = OscValueType.Int },
            new SequenceSlot { SlotType = SlotType.LoopEnd },
        };

        string base64 = _sut.Export(slots);
        IReadOnlyList<SequenceSlot>? result = _sut.Import(base64);

        result.Should().BeEquivalentTo(slots);
    }

    [Fact]
    public void Import_InvalidBase64_ThrowsFormatException()
    {
        Action act = () => _sut.Import("not-valid-base64!!!");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Import_ValidBase64ButInvalidJson_ThrowsJsonException()
    {
        string badJson = Convert.ToBase64String(Encoding.UTF8.GetBytes("{ not json }"));

        Action act = () => _sut.Import(badJson);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Import_EmptyJsonArray_ReturnsEmptyList()
    {
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("[]"));

        IReadOnlyList<SequenceSlot>? result = _sut.Import(base64);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Import_AllValueTypes_Preserved()
    {
        var slots = new[]
        {
            new SequenceSlot { Address = "/a", Value = 0.5f,  ValueType = OscValueType.Float },
            new SequenceSlot { Address = "/b", Value = 1f,    ValueType = OscValueType.Int },
            new SequenceSlot { Address = "/c", Value = 1f,    ValueType = OscValueType.Bool },
            new SequenceSlot { Address = "/d", StringValue = "str", ValueType = OscValueType.String },
        };

        string base64 = _sut.Export(slots);
        IReadOnlyList<SequenceSlot>? result = _sut.Import(base64);

        result.Should().BeEquivalentTo(slots);
    }
}
