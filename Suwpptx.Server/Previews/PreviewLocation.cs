using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Previews;

internal static partial class PreviewLocation
{
    [GeneratedRegex(@"^workspaces/[^/]+/versions/v[0-9]{1,6}\.pptx$")]
    private static partial Regex Version();

    [GeneratedRegex(@"^(templates/[^/]+|workspaces/[^/]+/resources/.+)\.(pptx|potx)$", RegexOptions.IgnoreCase)]
    private static partial Regex Other();

    // Why: a version keeps its pictures next to it for good; any other deck's pictures are a cache that is safe to delete
    public static string? Of(RootFolder root, FileInfo deck)
    {
        var relative = root.Relative(deck.FullName);
        if (FolderFiles.Hidden(relative))
        {
            return null;
        }
        if (Version().IsMatch(relative))
        {
            return Path.Combine(root.Path, relative[..^".pptx".Length] + "-previews");
        }
        if (!Other().IsMatch(relative))
        {
            return null;
        }
        var key = string.Create(CultureInfo.InvariantCulture, $"{relative}|{deck.Length}|{deck.LastWriteTimeUtc.Ticks}");
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16];
        return Path.Combine(root.Cache, "previews", hash);
    }
}
