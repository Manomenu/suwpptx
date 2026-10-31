namespace Suwpptx.Server.Workspaces;

internal sealed record DeckVersion(int Number, string Path, long Size, DateTimeOffset ChangedAt);
