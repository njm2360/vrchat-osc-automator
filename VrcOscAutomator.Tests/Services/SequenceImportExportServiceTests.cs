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
        var slots = new SequenceSlot[] { new FloatSlot("/test", 0.5f, 500, true, TransitionMode.None) };

        string result = _sut.Export("Test", slots, false);

        Action act = () => JsonDocument.Parse(result);
        act.Should().NotThrow();
    }

    [Fact]
    public void Export_IncludesNameAndSlots()
    {
        var slots = new SequenceSlot[] { new FloatSlot("/input/Vertical", 1.0f, 300, false, TransitionMode.None) };

        string json = _sut.Export("MyProfile", slots, false);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").GetString().Should().Be("MyProfile");
        doc.RootElement.GetProperty("slots")[0].GetProperty("type").GetString().Should().Be("float");
    }

    [Fact]
    public void Export_IncludesIsLoopMode()
    {
        var slots = new SequenceSlot[] { new FloatSlot("/test", 0.5f, 500, true, TransitionMode.None) };

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
            new IntSlot("/input/Jump",  1,    50, false, TransitionMode.None),
            new IntSlot("/input/Voice", 0,    50, false, TransitionMode.None),
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
        var slots = new SequenceSlot[] { new IntSlot("/input/Jump", 1, 500, true, TransitionMode.None) };

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
            new IntSlot("/input/Jump", 1, 50, false, TransitionMode.None),
            new LoopEndSlot(),
        };

        string json = _sut.Export("LoopTest", slots, false);
        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Slots.Should().HaveCount(3);
        result.Slots[0].Should().Be(new LoopBeginSlot(5));
        result.Slots[1].Should().Be(new IntSlot("/input/Jump", 1, 50, false, TransitionMode.None));
        result.Slots[2].Should().BeOfType<LoopEndSlot>();
    }

    [Fact]
    public void Import_InvalidJson_ThrowsJsonException()
    {
        Action act = () => _sut.Import("{ not json }");

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("""{"slots":[{"type":"float","value":0.5,"durationMs":500,"resetOnComplete":true,"transitionMode":0}],"name":"","isLoopMode":false}""")]  // address 欠落
    [InlineData("""{"slots":[{"type":"float","address":"/x","durationMs":500,"resetOnComplete":true,"transitionMode":0}],"name":"","isLoopMode":false}""")]  // value 欠落
    [InlineData("""{"slots":[{"type":"float","address":"/x","value":0.5,"resetOnComplete":true,"transitionMode":0}],"name":"","isLoopMode":false}""")]  // durationMs 欠落
    [InlineData("""{"slots":[{"type":"float","address":"/x","value":0.5,"durationMs":500,"transitionMode":0}],"name":"","isLoopMode":false}""")]  // resetOnComplete 欠落
    [InlineData("""{"slots":[{"type":"wait"}],"name":"","isLoopMode":false}""")]  // WaitSlot: durationMs 欠落
    [InlineData("""{"slots":[{"type":"loop_begin"}],"name":"","isLoopMode":false}""")]  // LoopBeginSlot: repeatCount 欠落
    public void Import_MissingRequiredField_ThrowsJsonException(string json)
    {
        Action act = () => _sut.Import(json);

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("""{"slots":[{"type":"float","address":"/x","value":0.5,"durationMs":500,"resetOnComplete":true,"transitionMode":1,"transitionToValue":1.0}],"name":"","isLoopMode":false}""")]  // transitionFromValue 欠落
    [InlineData("""{"slots":[{"type":"float","address":"/x","value":0.5,"durationMs":500,"resetOnComplete":true,"transitionMode":1,"transitionFromValue":0.0}],"name":"","isLoopMode":false}""")]  // transitionToValue 欠落
    [InlineData("""{"slots":[{"type":"int","address":"/x","value":1,"durationMs":500,"resetOnComplete":true,"transitionMode":1,"transitionToValue":1}],"name":"","isLoopMode":false}""")]  // IntSlot: transitionFromValue 欠落
    [InlineData("""{"slots":[{"type":"int","address":"/x","value":1,"durationMs":500,"resetOnComplete":true,"transitionMode":1,"transitionFromValue":0}],"name":"","isLoopMode":false}""")]  // IntSlot: transitionToValue 欠落
    public void Import_TransitionModeSetButValuesMissing_ThrowsJsonException(string json)
    {
        Action act = () => _sut.Import(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Export_FloatSlot_TransitionModeNone_ExcludesTransitionFields()
    {
        var slots = new SequenceSlot[] { new FloatSlot("/x", 0f, 500, true, TransitionMode.None) };

        string json = _sut.Export("Test", slots, false);

        using var doc = JsonDocument.Parse(json);
        var slot = doc.RootElement.GetProperty("slots")[0];
        slot.TryGetProperty("transitionFromValue", out _).Should().BeFalse();
        slot.TryGetProperty("transitionToValue", out _).Should().BeFalse();
    }

    [Fact]
    public void Export_FloatSlot_TransitionModeNonNone_IncludesTransitionFields()
    {
        var slots = new SequenceSlot[] { new FloatSlot("/x", 0f, 500, true, TransitionMode.Linear, 0.2f, 0.8f) };

        string json = _sut.Export("Test", slots, false);

        using var doc = JsonDocument.Parse(json);
        var slot = doc.RootElement.GetProperty("slots")[0];
        slot.TryGetProperty("transitionFromValue", out _).Should().BeTrue();
        slot.TryGetProperty("transitionToValue", out _).Should().BeTrue();
    }

    [Fact]
    public void Import_RoundTrip_TransitionValues_Preserved()
    {
        var slots = new SequenceSlot[]
        {
            new FloatSlot("/x", 0f, 500, true, TransitionMode.EaseInOut, 0.2f, 0.9f),
            new IntSlot("/y",   0,  500, true, TransitionMode.Linear,    2,    8),
        };

        string json = _sut.Export("Test", slots, false);
        ProfileExportData? result = _sut.Import(json);

        result!.Slots.Should().BeEquivalentTo(slots, o => o.RespectingRuntimeTypes());
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
            new FloatSlot("/a", 0.5f,   50, false, TransitionMode.None),
            new IntSlot("/b",   1,      50, false, TransitionMode.None),
            new BoolSlot("/c",  true,   50, false),
            new StringSlot("/d","str",  50, false),
        };

        string json = _sut.Export("AllTypes", slots, false);
        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Slots.Should().BeEquivalentTo(slots, o => o.RespectingRuntimeTypes());
    }
}
