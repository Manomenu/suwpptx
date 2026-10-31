using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Suwpptx.Server.Tests;

public sealed partial class CriteriaScriptTests : IDisposable
{
    private const string Spec = "docs/visual/spec/notes.html";

    private readonly string sample = Directory.CreateTempSubdirectory("suwpptx-criteria-").FullName;

    private sealed record Result(int ExitCode, string Output, string Errors);

    public void Dispose() => Directory.Delete(sample, recursive: true);

    private static Result Run(string root, params string[] arguments)
    {
        var gitBash = @"C:\Program Files\Git\bin\bash.exe";
        var start = new ProcessStartInfo(File.Exists(gitBash) ? gitBash : "bash")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("scripts/.internal/criteria.sh");
        start.ArgumentList.Add("--root");
        start.ArgumentList.Add(root);
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        start.WorkingDirectory = RepositoryFiles.Root;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("bash did not start");
        var output = process.StandardOutput.ReadToEnd();
        var errors = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new Result(process.ExitCode, output, errors);
    }

    private Result RunSample(params string[] arguments)
    {
        var git = new ProcessStartInfo("git", "init -q") { WorkingDirectory = sample };
        using (var process = Process.Start(git) ?? throw new InvalidOperationException("git did not start"))
        {
            process.WaitForExit();
        }
        return Run(sample, arguments);
    }

    private void Write(string path, string content)
    {
        var full = Path.Combine(sample, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full) ?? sample);
        File.WriteAllText(full, content);
    }

    private void WriteSpec(string path, string criteria, string prose = "") =>
        Write(
            path,
            $"<section data-role=\"goals\"><p>{prose}</p></section>\n<section data-role=\"acceptance\">\n<h2>Kryteria</h2>\n<ul class=\"checks\">\n{criteria}\n</ul>\n</section>"
        );

    private void WriteSpec(string criteria) => WriteSpec(Spec, criteria);

    [Theory]
    [InlineData("Suwpptx.Server.Tests/Notes/NoteStoreTests.cs", "[Trait(\"criterion\", \"AC-notes-1\")]", "server test (xunit)")]
    [InlineData("Suwpptx.Server.Tests/ContractsTests.cs", "[Trait(\"criterion\", \"AC-notes-1\")]", "architecture test (xunit)")]
    [InlineData("Suwpptx.Web/src/notes/NotesScreen.e2e.ts", "test(\"AC-notes-1: a note is saved\")", "e2e test (playwright)")]
    [InlineData("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1: a note is saved\")", "web test (vitest)")]
    [InlineData("Suwpptx.Web/eslint.config.js", "// AC-notes-1", "lint (eslint)")]
    [InlineData("Suwpptx.Web/tsconfig.app.json", "// AC-notes-1", "type check (tsc)")]
    [InlineData(".editorconfig", "# AC-notes-1\ndotnet_diagnostic.CA1062.severity = error", "lint (.NET analyzer)")]
    [InlineData("BannedSymbols.txt", "P:System.DateTime.Now;AC-notes-1 use TimeProvider", "banned API")]
    [InlineData("Directory.Build.props", "<!-- AC-notes-1 -->", "build rule")]
    public void A_guardrail_is_traced_to_its_criterion_with_its_kind(string path, string content, string kind)
    {
        WriteSpec("<li id=\"AC-notes-1\">a note is saved</li>");
        Write(path, content);

        var result = RunSample();

        Assert.True(result.ExitCode == 0, result.Errors);
        Assert.Matches($@"^AC-notes-1\s+{Regex.Escape(kind)}\s+{Regex.Escape(path)}:1$", result.Output.Trim());
    }

    [Fact]
    public void A_criterion_without_a_guardrail_fails_and_says_to_ask_the_owner()
    {
        WriteSpec("<li>AC-notes-1</li>\n<li>AC-notes-2</li>");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\")");

        var result = RunSample();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("AC-notes-2: no guardrail", result.Errors, StringComparison.Ordinal);
        Assert.Contains("ask the owner", result.Errors, StringComparison.Ordinal);
        Assert.DoesNotContain("AC-notes-1", result.Errors, StringComparison.Ordinal);
    }

    [Fact]
    public void A_guardrail_naming_a_criterion_no_spec_has_fails()
    {
        WriteSpec("<li>AC-notes-1</li>");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\"); it(\"AC-notes-10\")");

        var result = RunSample();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "AC-notes-10 at Suwpptx.Web/src/notes/notes.test.ts:1: no spec defines this criterion",
            result.Errors,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData("Suwpptx.Server/Notes/NoteStore.cs")]
    [InlineData("Suwpptx.Web/src/notes/NotesScreen.tsx")]
    [InlineData("Suwpptx.Web/knip.json")]
    public void An_id_where_the_gate_checks_nothing_is_not_a_guardrail(string path)
    {
        WriteSpec("<li>AC-notes-1</li>");
        Write(path, "// AC-notes-1");

        var result = RunSample();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains($"AC-notes-1 at {path}:1: not a guardrail", result.Errors, StringComparison.Ordinal);
    }

    [Fact]
    public void Prose_is_not_read()
    {
        WriteSpec("<li>AC-notes-1</li>");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\")");
        Write("README.md", "AC-notes-7");
        Write("docs/architecture.html", "AC-notes-8");

        Assert.Equal(0, RunSample().ExitCode);
    }

    [Fact]
    public void One_criterion_shows_only_its_guardrails()
    {
        WriteSpec("<li>AC-notes-1</li>\n<li>AC-notes-2</li>");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\")\nit(\"AC-notes-2\")");

        var result = RunSample("AC-notes-2");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("AC-notes-2", result.Output.Trim().Split(' ')[0]);
        Assert.Single(result.Output.Trim().Split('\n'));
    }

    [Theory]
    [InlineData("<p>no criteria section</p>", "no acceptance section")]
    [InlineData("<section data-role=\"acceptance\"><h2>Kryteria</h2></section>", "the acceptance section has no criteria")]
    public void A_spec_without_criteria_fails(string content, string expected)
    {
        Write(Spec, content);

        var result = RunSample();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains($"{Spec}: {expected}", result.Errors, StringComparison.Ordinal);
    }

    [Fact]
    public void A_criterion_without_an_id_fails_and_quotes_it()
    {
        WriteSpec("<li>AC-notes-1 a note is saved</li>\n<li data-status=\"pass\">a note is <b>pretty</b></li>");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\")");

        var result = RunSample();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains($"{Spec}: a criterion without an id: \"a note is pretty\"", result.Errors, StringComparison.Ordinal);
    }

    [Fact]
    public void A_criterion_with_several_ids_fails_once()
    {
        WriteSpec("<li>AC-notes-1 and AC-notes-2 together</li>");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\")");

        var result = RunSample();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("one criterion has several ids (AC-notes-1 AC-notes-2)", result.Errors, StringComparison.Ordinal);
        Assert.DoesNotContain("mentions", result.Errors, StringComparison.Ordinal);
    }

    [Fact]
    public void An_id_defined_in_two_specs_fails_and_names_both()
    {
        WriteSpec("<li>AC-notes-1 a note is saved</li>");
        WriteSpec("docs/visual/spec/notes-list.html", "<li>AC-notes-1 the list is sorted</li>");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\")");

        var result = RunSample();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains($"AC-notes-1 is defined twice (docs/visual/spec/notes-list.html,{Spec})", result.Errors, StringComparison.Ordinal);
    }

    [Fact]
    public void A_spec_may_mention_a_defined_criterion_but_not_an_undefined_one()
    {
        WriteSpec(Spec, "<li>AC-notes-1 a note is saved</li>", prose: "see AC-notes-1 and AC-notes-9");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\")");

        var result = RunSample();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains($"{Spec}:1: mentions AC-notes-9, which no acceptance section defines", result.Errors, StringComparison.Ordinal);
        Assert.DoesNotContain("mentions AC-notes-1", result.Errors, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_ignored_by_git_is_not_read()
    {
        WriteSpec("<li>AC-notes-1</li>");
        Write("Suwpptx.Web/src/notes/notes.test.ts", "it(\"AC-notes-1\")");
        Write(".gitignore", "**/dist/\n**/bin/\n");
        Write("Suwpptx.Web/dist/notes.test.ts", "it(\"AC-notes-9\")");
        Write("Suwpptx.Server/bin/Note.cs", "// AC-notes-8");

        var result = RunSample();

        Assert.True(result.ExitCode == 0, result.Errors);
        Assert.DoesNotContain("dist/", result.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AGENTS.md")]
    [InlineData("README.md")]
    public void Every_kind_of_guardrail_is_explained_where_people_and_agents_read(string document)
    {
        var kinds = KindLabel()
            .Matches(RepositoryFiles.Read("scripts/.internal/criteria.sh"))
            .Select(match => match.Groups[1].Value)
            .ToList();
        var text = RepositoryFiles.Read(document);

        Assert.NotEmpty(kinds);
        var missing = kinds.Where(kind => !text.Contains($"| `{kind}` |", StringComparison.Ordinal)).ToList();
        Assert.True(
            missing.Count == 0,
            $"{document} has no row for these kinds of guardrail of criteria.sh: add a row `| `<kind>` | where | how it names the id |` to its table: {string.Join(", ", missing)}"
        );
    }

    [GeneratedRegex(@"\) echo ""([^""]+)"" ;;")]
    private static partial Regex KindLabel();
}
