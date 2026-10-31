namespace Suwpptx.Server.Folder;

internal sealed record FolderEntry(string Path, long Size, DateTimeOffset ChangedAt);
