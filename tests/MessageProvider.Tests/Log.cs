public static class FileLog
{
    public static void Log(string message)
    {
        var logFilePath = "../../../../../pact.log";
        using var writer = new StreamWriter(logFilePath, true);
        writer.WriteLine($"{DateTime.Now}: {message}");
    }
}