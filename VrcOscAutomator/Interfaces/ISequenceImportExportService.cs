using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface ISequenceImportExportService
{
    string Export(string name, IEnumerable<SequenceSlot> slots, bool isLoopMode);

    ProfileExportData? Import(string input);
}
