namespace Suwpptx.Server.Tests;

internal sealed record ScriptResult(int ExitCode, string Output, string Errors, TimeSpan Elapsed)
{
    public string All => Output + Errors;
}
