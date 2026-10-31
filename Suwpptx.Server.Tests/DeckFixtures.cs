namespace Suwpptx.Server.Tests;

internal static class DeckFixtures
{
    public static byte[] Bytes(string fileName) =>
        File.ReadAllBytes(Path.Combine(RepositoryFiles.Root, "Suwpptx.Server.Tests", "Previews", "Decks", fileName));

    public static void Copy(string fixture, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? throw new InvalidOperationException($"{target} has no folder"));
        File.WriteAllBytes(target, Bytes(fixture));
    }
}
