using System.IO;
using System.Text;

namespace VrcOscAutomator.Services;

public static class CrashLogger
{
    private const long MaxFileSize = 1024 * 1024;

    private static readonly Lock Gate = new();

    public static string LogPath { get; } = Path.Combine(AppContext.BaseDirectory, "error.log");

    public static void Log(string source, Exception? ex)
    {
        var text =
            $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {source}{Environment.NewLine}" +
            $"{ex?.ToString() ?? "(例外オブジェクトなし)"}{Environment.NewLine}{Environment.NewLine}";

        lock (Gate)
        {
            try
            {
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxFileSize)
                    File.Delete(LogPath);

                File.AppendAllText(LogPath, text, Encoding.UTF8);
            }
            catch
            { }
        }
    }
}
