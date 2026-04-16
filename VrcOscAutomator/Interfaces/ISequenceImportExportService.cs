using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface ISequenceImportExportService
{
    string Export(IEnumerable<SequenceSlot> slots);

    IReadOnlyList<SequenceSlot>? Import(string input);
}
