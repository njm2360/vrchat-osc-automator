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

        string result = _sut.Export("Test", slots, false);

        Action act = () => JsonDocument.Parse(result);
        act.Should().NotThrow();
    }

    [Fact]
    public void Export_IncludesNameAndSlots()
    {
        var slots = new SequenceSlot[] { new FloatSlot("/input/Vertical", 1.0f, 300, false) };

        string json = _sut.Export("MyProfile", slots, false);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").GetString().Should().Be("MyProfile");
        doc.RootElement.GetProperty("slots")[0].GetProperty("type").GetString().Should().Be("float");
    }

    [Fact]
    public void Export_IncludesIsLoopMode()
    {
        var slots = new SequenceSlot[] { new FloatSlot("/test", 0.5f) };

        string json = _sut.Export("Test", slots, true);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("isLoopMode").GetBoolean().Should().BeTrue();
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

        string json = _sut.Export("RoundTrip", slots, false);
        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("RoundTrip");
        result.Slots.Should().BeEquivalentTo(slots, o => o.RespectingRuntimeTypes());
    }

    [Fact]
    public void Import_RoundTrip_PreservesIsLoopMode()
    {
        var slots = new SequenceSlot[] { new IntSlot("/input/Jump", 1) };

        string json = _sut.Export("Test", slots, true);
        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.IsLoopMode.Should().BeTrue();
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

        string json = _sut.Export("LoopTest", slots, false);
        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Slots.Should().HaveCount(3);
        result.Slots[0].Should().Be(new LoopBeginSlot(5));
        result.Slots[1].Should().Be(new IntSlot("/input/Jump", 1, 50, false));
        result.Slots[2].Should().BeOfType<LoopEndSlot>();
    }

    [Fact]
    public void Import_InvalidJson_ThrowsJsonException()
    {
        Action act = () => _sut.Import("{ not json }");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Import_EmptySlots_ReturnsNull()
    {
        string json = _sut.Export("Empty", [], false);
        ProfileExportData? result = _sut.Import(json);

        result.Should().BeNull();
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

        string json = _sut.Export("AllTypes", slots, false);
        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Slots.Should().BeEquivalentTo(slots, o => o.RespectingRuntimeTypes());
    }
}
