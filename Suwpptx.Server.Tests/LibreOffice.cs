namespace Suwpptx.Server.Tests;

internal static class LibreOffice
{
    public static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(90);

    private const string Missing = "Install LibreOffice Impress to run the preview tests (Fedora: sudo dnf install libreoffice-impress)";

    // Why: a developer machine without LibreOffice skips the preview tests, but CI must never pass without having run them
    public static void RequireInstalled()
    {
        if (OnPath("soffice"))
        {
            return;
        }
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
        {
            Assert.Fail(Missing);
        }
        Assert.Skip(Missing);
    }

    private static bool OnPath(string program) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(directory => File.Exists(Path.Combine(directory, program)));
}
