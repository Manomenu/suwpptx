namespace Suwpptx.Server.Workspaces;

internal sealed record WorkspaceSummary(string Name, int Files, int Versions, int? LatestVersion, DateTimeOffset ChangedAt);
