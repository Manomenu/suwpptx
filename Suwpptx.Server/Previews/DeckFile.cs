using System.IO.Compression;

namespace Suwpptx.Server.Previews;

internal static class DeckFile
{
    private static readonly byte[] OleSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    // Why: LibreOffice answers a broken deck with a vague failure, so the reason is found here, where it can say what to do
    public static string? ProblemWith(string path)
    {
        var name = Path.GetFileName(path);
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Span<byte> head = stackalloc byte[8];
            var read = stream.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
            if (read == head.Length && head.SequenceEqual(OleSignature))
            {
                return $"{name} is password-protected or in the old .ppt format: save it as an unprotected .pptx";
            }
            if (read < ZipSignature.Length || !head[..ZipSignature.Length].SequenceEqual(ZipSignature))
            {
                return $"{name} is not a PowerPoint file (.pptx)";
            }
            stream.Position = 0;
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            return zip.GetEntry("ppt/presentation.xml") is null ? $"{name} is a zip file but not a PowerPoint deck" : null;
        }
        catch (InvalidDataException)
        {
            return $"{name} is damaged: it can't be opened as a .pptx";
        }
    }
}
