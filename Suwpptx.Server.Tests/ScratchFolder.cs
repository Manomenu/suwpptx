namespace Suwpptx.Server.Tests;

internal sealed class ScratchFolder : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("suwpptx-test-");

    public string Path => _folder.FullName;

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose()
    {
        try
        {
            _folder.Delete(recursive: true);
        }
        catch (IOException)
        {
            // Why: a started fake app may still hold a file for a moment; a temp folder left behind is harmless.
        }
    }
}
