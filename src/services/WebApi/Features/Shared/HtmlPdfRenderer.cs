using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace WebApi.Features.Shared;

/// <summary>
/// Renders filled HTML document templates to PDF via headless Chromium.
/// Layout lives in the editable template; this only prints the substituted HTML.
/// </summary>
public static class HtmlPdfRenderer
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IBrowser? _browser;

    public static async Task<byte[]> RenderAsync(string html, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(html))
            throw new ArgumentException("HTML content is required.", nameof(html));

        await Gate.WaitAsync(ct);
        try
        {
            var browser = await EnsureBrowserAsync(ct);
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle0],
                Timeout = 60_000
            });

            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                PreferCSSPageSize = true,
                MarginOptions = new MarginOptions
                {
                    Top = "8mm",
                    Bottom = "8mm",
                    Left = "8mm",
                    Right = "8mm"
                }
            });
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<IBrowser> EnsureBrowserAsync(CancellationToken ct)
    {
        if (_browser is { IsClosed: false })
            return _browser;

        var fetcher = new BrowserFetcher();
        await fetcher.DownloadAsync();
        ct.ThrowIfCancellationRequested();

        _browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-dev-shm-usage", "--font-render-hinting=none"]
        });
        return _browser;
    }
}
