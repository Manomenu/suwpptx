using System.Diagnostics;
using PDFtoImage;

namespace Suwpptx.Server.Previews;

internal sealed class SlideRenderer
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);

    private readonly string? soffice = LibreOfficeLocation.Find(
        Environment.GetEnvironmentVariable("PATH"),
        Environment.GetEnvironmentVariable("PATHEXT"),
        OperatingSystem.IsWindows(),
        LibreOfficeLocation.InstallFolders(
            OperatingSystem.IsWindows(),
            Environment.GetEnvironmentVariable("ProgramFiles"),
            Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
            "/opt"
        )
    );

    private const int SlideWidth = 1280;

    public bool Available => soffice is not null;

    public static string Problem => LibreOfficeLocation.Problem(OperatingSystem.IsWindows());

    // Why: failures are reported as InvalidOperationException with a reason a person can act on; the worker writes it to error.txt
    public async Task<int> RenderAsync(string deck, string outputDirectory, CancellationToken cancellationToken)
    {
        if (soffice is null)
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
            // Why: PDFium is native code that cannot be cancelled, so the pages are drawn on a pool thread and abandoned if the render is cancelled
            return await Task.Run(() => DrawPages(pdf.FullName, outputDirectory, cancellationToken), cancellationToken);
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    // Why: the PDF library declares the systems it runs on; the app ships for Windows and Linux only
    private static int DrawPages(string pdf, string outputDirectory, CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            using var stream = File.OpenRead(pdf);
            var count = Conversion.GetPageCount(stream, leaveOpen: true);
            if (count == 0)
            {
                throw new InvalidOperationException("The deck has no slides to show.");
            }
            for (var index = 0; index < count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Conversion.SavePng(
                    Path.Combine(outputDirectory, $"{index + 1:D2}.png"),
                    stream,
                    index,
                    leaveOpen: true,
                    options: new RenderOptions(Width: SlideWidth)
                );
            }
            return count;
        }
        throw new PlatformNotSupportedException("Slide previews run on Windows and Linux.");
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
}
