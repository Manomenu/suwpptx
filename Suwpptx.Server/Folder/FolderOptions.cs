namespace Suwpptx.Server.Folder;

internal sealed class FolderOptions
{
    public string Root { get; set; } = "";

    public long UploadLimitMegabytes { get; set; } = 2048;
}
