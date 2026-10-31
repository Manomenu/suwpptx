using Suwpptx.Server.Previews;

namespace Suwpptx.Server.Tests.Previews;

public sealed class LibreOfficeLocationTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("suwpptx-lo-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Fake(params string[] parts)
    {
        var file = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(new FileInfo(file).DirectoryName ?? root);
        File.WriteAllText(file, "");
        return file;
    }

    private string Folder(params string[] parts) => Directory.CreateDirectory(Path.Combine([root, .. parts])).FullName;

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-3")]
    public void On_Windows_a_PATH_folder_is_searched_with_each_PATHEXT_extension()
    {
        var expected = Fake("tools", "soffice.com");
        var path = string.Join(Path.PathSeparator, Folder("empty"), Folder("tools"));

        var found = LibreOfficeLocation.Find(path, ".EXE;.COM;.BAT", windows: true, []);

        Assert.Equal(expected, found);
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-3")]
    public void On_Windows_the_standard_install_folder_is_used_when_PATH_has_nothing()
    {
        var expected = Fake("Program Files", "LibreOffice", "program", "soffice.exe");
        var folders = LibreOfficeLocation.InstallFolders(
            windows: true,
            Path.Combine(root, "Program Files"),
            Path.Combine(root, "Program Files (x86)"),
            optRoot: Path.Combine(root, "opt")
        );

        var found = LibreOfficeLocation.Find(Folder("empty"), ".EXE;.COM", windows: true, folders);

        Assert.Equal(expected, found);
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-3")]
    public void On_Linux_plain_soffice_is_found_on_PATH_and_in_the_opt_folders()
    {
        var onPath = Fake("bin", "soffice");
        var inOpt = Fake("opt", "libreoffice25.8", "program", "soffice");
        Fake("lib64", "libreoffice", "program", "soffice");

        Assert.Equal(onPath, LibreOfficeLocation.Find(Folder("bin"), null, windows: false, []));
        var folders = LibreOfficeLocation
            .InstallFolders(windows: false, null, null, optRoot: Path.Combine(root, "opt"))
            .Where(folder => folder.StartsWith(root, StringComparison.Ordinal));
        Assert.Equal(inOpt, LibreOfficeLocation.Find(Folder("empty"), null, windows: false, folders));
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-3")]
    public void Nothing_found_gives_null_and_a_problem_that_names_the_install_command_of_the_system()
    {
        var folders = LibreOfficeLocation.InstallFolders(windows: true, Folder("pf"), null, optRoot: Path.Combine(root, "opt"));

        Assert.Null(LibreOfficeLocation.Find(Folder("empty"), ".EXE", windows: true, folders));
        Assert.Contains(
            "winget install TheDocumentFoundation.LibreOffice",
            LibreOfficeLocation.Problem(windows: true),
            StringComparison.Ordinal
        );
        Assert.Contains("sudo dnf install libreoffice-impress", LibreOfficeLocation.Problem(windows: false), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-3")]
    public void The_standard_Linux_folders_are_listed()
    {
        var folders = LibreOfficeLocation.InstallFolders(windows: false, null, null, optRoot: Path.Combine(root, "none")).ToList();

        Assert.Contains("/usr/bin", folders);
        Assert.Contains("/usr/lib/libreoffice/program", folders);
        Assert.Contains("/usr/lib64/libreoffice/program", folders);
    }
}
