using System.Text.Json;
using FluentAssertions;
using VrcOscAutomator.Exceptions;
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
    public void Import_EmptySlots_ReturnsData()
    {
        string json = _sut.Export("Empty", [], false);
        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Slots.Should().BeEmpty();
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

    // ─── RandomWaitSlot バリデーション ────────────────────────────────────

    [Fact]
    public void Import_RandomWait_ValidRange_RoundTrips()
    {
        var slots = new SequenceSlot[] { new RandomWaitSlot(300, 1000) };

        string json = _sut.Export("Rand", slots, false);
        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Slots.Should().ContainSingle();
        result.Slots[0].Should().Be(new RandomWaitSlot(300, 1000));
    }

    [Fact]
    public void Import_RandomWait_MinEqualsMax_Imports()
    {
        const string json = """{"name":"","isLoopMode":false,"slots":[{"type":"random_wait","minMs":500,"maxMs":500}]}""";

        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Slots.Should().ContainSingle();
    }

    [Fact]
    public void Import_RandomWait_MinGreaterThanMax_ThrowsJsonException()
    {
        const string json = """{"name":"","isLoopMode":false,"slots":[{"type":"random_wait","minMs":1000,"maxMs":300}]}""";

        Action act = () => _sut.Import(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Import_RandomWait_NegativeMin_ThrowsJsonException()
    {
        const string json = """{"name":"","isLoopMode":false,"slots":[{"type":"random_wait","minMs":-1,"maxMs":300}]}""";

        Action act = () => _sut.Import(json);

        act.Should().Throw<JsonException>();
    }

    // ─── スキーマバージョン ────────────────────────────────────────────────

    [Fact]
    public void Export_IncludesSchemaVersionAndAppVersion()
    {
        var slots = new SequenceSlot[] { new WaitSlot(100) };

        string json = _sut.Export("Test", slots, false);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(SequenceImportExportService.CurrentSchemaVersion);
        doc.RootElement.GetProperty("appVersion").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Import_LegacyFormat_NoSchemaVersion_IntEnum_Imports()
    {
        // v1.2.0形式: schemaVersion フィールド無し + enum を整数で表現
        const string json = """{"name":"Legacy","isLoopMode":false,"slots":[{"type":"int","address":"/x","value":1,"durationMs":50,"resetOnComplete":false,"transitionMode":0}]}""";

        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Legacy");
        result.Slots.Should().ContainSingle();
    }

    [Fact]
    public void Import_LegacyFormat_NoSchemaVersion_StringEnum_Imports()
    {
        // v1.3.0形式: schemaVersion フィールド無し + enum を文字列で表現
        const string json = """{"name":"Legacy","isLoopMode":false,"slots":[{"type":"int","address":"/x","value":1,"durationMs":50,"resetOnComplete":false,"transitionMode":"None"}]}""";

        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Legacy");
        result.Slots.Should().ContainSingle();
    }

    [Fact]
    public void Import_WithCurrentSchemaVersion_Imports()
    {
        const string json = """{"schemaVersion":2,"appVersion":"1.3.1","name":"Current","isLoopMode":false,"slots":[{"type":"int","address":"/x","value":1,"durationMs":50,"resetOnComplete":false,"transitionMode":"None"}]}""";

        ProfileExportData? result = _sut.Import(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Current");
        result.SchemaVersion.Should().Be(2);
        result.AppVersion.Should().Be("1.3.1");
    }

    [Fact]
    public void Import_FutureSchemaVersion_ThrowsUnsupportedSchemaVersionException()
    {
        const string json = """{"schemaVersion":3,"name":"Future","isLoopMode":false,"slots":[]}""";

        Action act = () => _sut.Import(json);

        act.Should().Throw<UnsupportedSchemaVersionException>()
            .Which.SchemaVersion.Should().Be(3);
    }

    [Theory]
    [InlineData("""{"slots":[null],"name":"","isLoopMode":false}""")]  // null スロット要素
    [InlineData("""{"slots":[{"type":"float","address":null,"value":0.5,"durationMs":500,"resetOnComplete":true,"transitionMode":"None"}],"name":"","isLoopMode":false}""")]  // address が null
    [InlineData("""{"slots":[{"type":"int","address":null,"value":1,"durationMs":500,"resetOnComplete":true,"transitionMode":"None"}],"name":"","isLoopMode":false}""")]  // IntSlot: address が null
    public void Import_NullSlotOrNullAddress_ThrowsJsonException(string json)
    {
        Action act = () => _sut.Import(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Import_NonNumericSchemaVersion_ThrowsJsonException()
    {
        const string json = """{"schemaVersion":"3","name":"Bad","isLoopMode":false,"slots":[]}""";

        Action act = () => _sut.Import(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Import_FutureSchemaVersion_WithUndeserializableSlot_StillThrowsUnsupportedSchemaVersionException()
    {
        // 未知のスロット型を含み、通常のデシリアライズなら JsonException になるはずのファイルでも、
        // schemaVersion チェックが優先されて UnsupportedSchemaVersionException が投げられることを確認する。
        const string json = """{"schemaVersion":3,"name":"Future","isLoopMode":false,"slots":[{"type":"unknown_future_slot_type","someField":123}]}""";

        Action act = () => _sut.Import(json);

        act.Should().Throw<UnsupportedSchemaVersionException>()
            .Which.SchemaVersion.Should().Be(3);
    }
}
