using System.Buffers;

namespace Suwpptx.Server.Folder;

internal static class FolderNames
{
    public const int MaxLength = 120;

    private static readonly SearchValues<char> Forbidden = SearchValues.Create("/\\:*?\"<>|\0");

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
            return "Use a name without spaces at the start or the end.";
        }
        return null;
    }
}
