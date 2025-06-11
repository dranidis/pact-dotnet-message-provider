public static class FileLog
{
    private static readonly string LogFilePath = "../../../../../pact.log";
    public static void DeleteLog()
    {
        if (File.Exists(LogFilePath))
        {
            File.Delete(LogFilePath);
        }
    }

    public static void Log(string message)
    {
        using var writer = new StreamWriter(LogFilePath, true);
        writer.WriteLine(message);
    }
}