namespace Suwpptx.Server.Previews;

internal static class LibreOfficeLocation
{
    public const string WindowsInstall = "winget install TheDocumentFoundation.LibreOffice";

    public const string FedoraInstall = "sudo dnf install libreoffice-impress";

    // Why: the Windows installer does not put LibreOffice on PATH, so the standard install folders are searched as well
    public static string? Find(string? path, string? pathExtensions, bool windows, IEnumerable<string> installFolders)
    {
        var names = windows ? Executables(pathExtensions) : ["soffice"];
        var onPath = (path ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(directory => names.Select(name => Path.Combine(directory, name)))
            .FirstOrDefault(File.Exists);
        return onPath
            ?? installFolders.Select(folder => Path.Combine(folder, windows ? "soffice.exe" : "soffice")).FirstOrDefault(File.Exists);
    }

    public static IEnumerable<string> InstallFolders(bool windows, string? programFiles, string? programFilesX86, string optRoot)
    {
        if (windows)
        {
            return new[] { programFiles, programFilesX86 }
                .OfType<string>()
                .Where(folder => folder.Length > 0)
                .Select(folder => Path.Combine(folder, "LibreOffice", "program"));
        }
        var opt = Directory.Exists(optRoot)
            ? Directory
                .EnumerateDirectories(optRoot, "libreoffice*")
                .Order(StringComparer.Ordinal)
                .Select(folder => Path.Combine(folder, "program"))
            : [];
        return ["/usr/bin", "/usr/lib/libreoffice/program", "/usr/lib64/libreoffice/program", .. opt];
    }

    public static string Problem(bool windows) =>
        $"Install LibreOffice Impress to see slide previews ({(windows ? "Windows: " + WindowsInstall : "Fedora: " + FedoraInstall)}), then restart the app.";

    private static string[] Executables(string? pathExtensions)
    {
        var extensions = (string.IsNullOrWhiteSpace(pathExtensions) ? ".EXE;.COM" : pathExtensions).Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return [.. extensions.Select(extension => "soffice" + extension.ToLowerInvariant())];
    }
}
