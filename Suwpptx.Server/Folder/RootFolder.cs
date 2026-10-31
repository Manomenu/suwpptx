using Microsoft.Extensions.Options;

namespace Suwpptx.Server.Folder;

internal sealed class RootFolder(IOptions<FolderOptions> options)
{
    public string Path { get; } = System.IO.Path.GetFullPath(options.Value.Root);

    public long UploadLimitBytes { get; } = options.Value.UploadLimitMegabytes * 1024 * 1024;

    public string Templates => System.IO.Path.Combine(Path, "templates");

    public string Workspaces => System.IO.Path.Combine(Path, "workspaces");

    public string Cache => System.IO.Path.Combine(Path, ".cache");

    public void Create()
    {
        Directory.CreateDirectory(Templates);
        Directory.CreateDirectory(Workspaces);
    }

    public string? Workspace(string name) => FolderNames.ProblemWith(name) is null ? System.IO.Path.Combine(Workspaces, name) : null;

    // Why: every path a request names goes through here, so nothing outside the root is ever read or written
    public static string? Inside(string directory, string relativePath)
    {
        var segments = relativePath.Split('/');
        if (segments.Any(segment => FolderNames.ProblemWith(segment) is not null))
        {
            return null;
        }
        var full = System.IO.Path.GetFullPath(System.IO.Path.Combine([directory, .. segments]));
        var root =
            System.IO.Path.GetFullPath(directory).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
        return full.StartsWith(root, StringComparison.Ordinal) ? full : null;
    }

    public string Relative(string fullPath) => System.IO.Path.GetRelativePath(Path, fullPath).Replace('\\', '/');
}
