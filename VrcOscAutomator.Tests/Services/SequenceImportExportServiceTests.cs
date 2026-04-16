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
    public void Export_ReturnsValidJson()
    {
        var slots = new SequenceSlot[] { new FloatSlot("/test", 0.5f) };

        string result = _sut.Export(slots);

        // valid JSON かどうかは例外なしでパースできることで確認
        Action act = () => JsonDocument.Parse(result);
        act.Should().NotThrow();
    }

    [Fact]
    public void Export_EmptyList_ReturnsEmptyJsonArray()
    {
        string result = _sut.Export([]);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void Export_IncludesTypeDiscriminator()
    {
        var slots = new SequenceSlot[] { new FloatSlot("/input/Vertical", 1.0f, 300, false) };

        string json = _sut.Export(slots);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement[0].GetProperty("Type").GetString().Should().Be("float");
    }

    // ─── Import ────────────────────────────────────────────────────────────

    [Fact]
    public void Import_RoundTrip_ReturnsIdenticalSlots()
    {
        var slots = new SequenceSlot[]
        {
            new IntSlot("/input/Jump",  1,    50, false),
            new IntSlot("/input/Voice", 0,    50, false),
            new WaitSlot(1000),
        };

        string json = _sut.Export(slots);
        IReadOnlyList<SequenceSlot>? result = _sut.Import(json);

        result.Should().BeEquivalentTo(slots, o => o.RespectingRuntimeTypes());
    }

    [Fact]
    public void Import_LoopMarkers_RoundTrip()
    {
        var slots = new SequenceSlot[]
        {
            new LoopBeginSlot(5),
            new IntSlot("/input/Jump", 1, 50, false),
            new LoopEndSlot(),
        };

        string json = _sut.Export(slots);
        IReadOnlyList<SequenceSlot>? result = _sut.Import(json);

        result.Should().NotBeNull().And.HaveCount(3);
        result![0].Should().Be(new LoopBeginSlot(5));
        result[1].Should().Be(new IntSlot("/input/Jump", 1, 50, false));
        result[2].Should().BeOfType<LoopEndSlot>();
    }

    [Fact]
    public void Import_InvalidJson_ThrowsJsonException()
    {
        Action act = () => _sut.Import("{ not json }");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Import_EmptyJsonArray_ReturnsEmptyList()
    {
        IReadOnlyList<SequenceSlot>? result = _sut.Import("[]");

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Import_AllValueTypes_Preserved()
    {
        var slots = new SequenceSlot[]
        {
            new FloatSlot("/a", 0.5f,   50, false),
            new IntSlot("/b",   1,      50, false),
            new BoolSlot("/c",  true,   50, false),
            new StringSlot("/d","str",  50, false),
        };

        string json = _sut.Export(slots);
        IReadOnlyList<SequenceSlot>? result = _sut.Import(json);

        result.Should().BeEquivalentTo(slots, o => o.RespectingRuntimeTypes());
    }
}
