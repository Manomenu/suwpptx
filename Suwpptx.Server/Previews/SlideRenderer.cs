using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Suwpptx.Server.Previews;

internal sealed partial class SlideRenderer
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);

    private readonly string? soffice = OnPath("soffice");

    private readonly string? pdftoppm = OnPath("pdftoppm");

    public bool Available => soffice is not null && pdftoppm is not null;

    public static string Problem =>
        "Install LibreOffice Impress and poppler-utils (Fedora: sudo dnf install libreoffice-impress poppler-utils) to see slide previews.";

    [GeneratedRegex(@"-([0-9]+)\.png$")]
    private static partial Regex PageNumber();

    // Why: failures are reported as InvalidOperationException with a reason a person can act on; the worker writes it to error.txt
    public async Task<int> RenderAsync(string deck, string outputDirectory, CancellationToken cancellationToken)
    {
        if (soffice is null || pdftoppm is null)
        {
            throw new InvalidOperationException(Problem);
        }
        var work = Directory.CreateTempSubdirectory("suwpptx-render-");
        try
        {
            var profile = Directory.CreateDirectory(Path.Combine(work.FullName, "profile"));
            var pdfs = Directory.CreateDirectory(Path.Combine(work.FullName, "pdf"));
            // Why: a private profile keeps this render from clashing with a LibreOffice the user has open
            await RunAsync(
                soffice,
                [
                    "--headless",
                    "--norestore",
                    $"-env:UserInstallation={new Uri(profile.FullName + "/").AbsoluteUri}",
                    "--convert-to",
                    "pdf",
                    "--outdir",
                    pdfs.FullName,
                    deck,
                ],
                cancellationToken
            );
            var pdf =
                pdfs.EnumerateFiles("*.pdf").FirstOrDefault()
                ?? throw new InvalidOperationException("LibreOffice could not turn the deck into slides.");
            var pages = Directory.CreateDirectory(Path.Combine(work.FullName, "pages"));
            await RunAsync(pdftoppm, ["-png", "-scale-to", "1280", pdf.FullName, Path.Combine(pages.FullName, "page")], cancellationToken);
            var images = pages
                .EnumerateFiles("page-*.png")
                .Select(file => (file, number: int.Parse(PageNumber().Match(file.Name).Groups[1].Value, CultureInfo.InvariantCulture)))
                .OrderBy(image => image.number)
                .ToList();
            if (images.Count == 0)
            {
                throw new InvalidOperationException("The deck has no slides to show.");
            }
            for (var index = 0; index < images.Count; index++)
            {
                images[index].file.MoveTo(Path.Combine(outputDirectory, $"{index + 1:D2}.png"));
            }
            return images.Count;
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    private static async Task RunAsync(string program, string[] arguments, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }
        using var process = Process.Start(info) ?? throw new InvalidOperationException($"{Path.GetFileName(program)} did not start.");
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Timeout);
        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var errors = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException($"Drawing the slides took longer than {Timeout.TotalSeconds:0} seconds.");
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
        await output;
        var message = (await errors).Trim();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(program)} failed (exit code {process.ExitCode}){(message.Length == 0 ? "" : ": " + message)}"
            );
        }
    }

    private static string? OnPath(string program) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, program))
            .FirstOrDefault(File.Exists);
}
