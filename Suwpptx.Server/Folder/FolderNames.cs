using System.Buffers;

namespace Suwpptx.Server.Folder;

internal static class FolderNames
{
    public const int MaxLength = 120;

    private static readonly SearchValues<char> Forbidden = SearchValues.Create("/\\:*?\"<>|\0");

    // Why: Windows treats these as devices whatever the extension, so a name that works on Linux would fail when the folder is used on Windows
    private static readonly HashSet<string> Reserved = new(
        ["CON", "PRN", "AUX", "NUL", .. Enumerable.Range(1, 9).SelectMany(number => new[] { $"COM{number}", $"LPT{number}" })],
        StringComparer.OrdinalIgnoreCase
    );

    public static string? ProblemWith(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Give it a name.";
        }
        if (name.Length > MaxLength)
        {
            return $"Use a name of at most {MaxLength} characters.";
        }
        if (name.StartsWith('.'))
        {
            return "Use a name that doesn't start with a dot: those are the app's own files.";
        }
        if (name.AsSpan().ContainsAny(Forbidden) || name.Any(char.IsControl))
        {
            return "Use a name without / \\ : * ? \" < > | : it has to work as a folder or file name.";
        }
        if (name != name.Trim())
        {
            return "Pick another name: it can't start or end with a space (Windows drops them).";
        }
        if (name.EndsWith('.'))
        {
            return "Pick another name: Windows doesn't allow a name that ends in a dot.";
        }
        var stem = name.Split('.')[0].TrimEnd(' ');
        if (Reserved.Contains(stem))
        {
            return $"Pick another name: Windows reserves {stem.ToUpperInvariant()}, also with an extension.";
        }
        return null;
    }
}
