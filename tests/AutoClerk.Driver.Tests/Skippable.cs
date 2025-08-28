using Xunit.Sdk;

internal static class Skippable
{
    public static void WindowsOnly()
    {
        if (!OperatingSystem.IsWindows())
            SkipException.ForSkip("Windows-only UI automation test.");
    }
}