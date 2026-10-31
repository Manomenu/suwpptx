using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Workspaces;

internal sealed record Workspace(string Name, string Folder, IReadOnlyList<FolderEntry> Files, IReadOnlyList<DeckVersion> Versions);
