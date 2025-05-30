using HtmlAgilityPack;
using PuppeteerSharp;
using System.IO;
using System.Net;
using System.Net.Http.Headers;

namespace CSharpHelper;

#pragma warning disable
public static class Internet
{
    private static HttpClient m_httpClient;
    private static WebClient m_webClient;

    public static bool HasBeenInitialized {get; private set;} = false;

    public static ushort RetryLimit = 10;

    public static async Task Init()
    {
        Console.WriteLine("Initializing CSharpHelper.Internet");

        HasBeenInitialized = true;
        m_httpClient = new HttpClient();
        m_webClient = new WebClient();

        m_httpClient.Timeout = TimeSpan.FromMinutes(5);

        Console.WriteLine("Initialized CSharpHelper.Internet");
    }

    public static async Task<HtmlDocument> GetStaticPage_WebClientSync(string url)
    {
        if (!HasBeenInitialized)
            await Init();

        try
        {
            HtmlDocument page = new HtmlDocument();

            Console.WriteLine($"Downloading html from {url}");
            string html = m_webClient.DownloadString(url);
            Console.WriteLine($"Downloaded html from {url}");

            File.WriteAllText("page.html", html);

            page.LoadHtml(html);
            return page;
        }
        catch (Exception)
        {
            Console.WriteLine($"failed to download html from '{url}'");
            throw;
        }
    }
    public static async Task<HtmlDocument[]> GetStaticPages_HTTPClientAsync(string[] urls)
    {
        if (!HasBeenInitialized)
            await Init();

        HtmlDocument[] pages = new HtmlDocument[urls.Length];
        Task<HtmlDocument>[] downloadTasks = new Task<HtmlDocument>[urls.Length];

        for (int i = 0; i < urls.Length; i++)
        {
            string url = urls[i];
            downloadTasks[i] = GetStaticPage_HTTPClientAsync(url);
        }
        
        pages = await Task.WhenAll(downloadTasks);
        return pages;
    }
    public static async Task<HtmlDocument> GetStaticPage_HTTPClientAsync(string url)
    {
        if (!HasBeenInitialized)
            await Init();

        try
        {
            HtmlDocument page = new HtmlDocument();

            Console.WriteLine($"Downloading html from {url}");
            string html = await m_httpClient.GetStringAsync(url);
            Console.WriteLine($"Downloaded html from {url}");

            page.LoadHtml(html);
            return page;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"failed to download html from '{url}'");
            throw ex;
        }
    }
    public static async Task<HtmlDocument> GetDynamicPage(string url)
    {
        if (!HasBeenInitialized)
            await Init();

        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        await using var browser = await Puppeteer.LaunchAsync(
            new LaunchOptions { Headless = true });

        var page = await browser.NewPageAsync();
        await page.GoToAsync(url, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
        });
        
        string content = await page.GetContentAsync();

        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);

        return htmlDocument;
    }
    
    public static async Task DownloadImages((string imageLink, string imageName)[] images, string folderPath)
    {
        DirectoryInfo directory = new DirectoryInfo(folderPath);

        if (!Directory.Exists(folderPath))
            directory = Directory.CreateDirectory(folderPath);

        await DownloadImages(images, directory);
        
    }
    public static async Task DownloadImages((string imageLink, string imageName)[] images, DirectoryInfo folder)
    {
        if (!HasBeenInitialized)
            await Init();

        Task[] downloadTasks = new Task[images.Length];

        for (int i = 0; i < images.Length; i++)
        {
            if (!Path.HasExtension(images[i].imageName))
                images[i].imageName = Path.GetExtension(images[i].imageLink);

            downloadTasks[i] = DownloadImage(images[i], folder);
        }

        Task.WaitAll(downloadTasks);
    }

    public static async Task DownloadImage((string link, string name) image, DirectoryInfo directory)
    {
        if (!HasBeenInitialized)
            await Init();
            
        if (directory == null)
            throw new DirectoryNotFoundException($"directory not found");

        string filePath = $"{directory.FullName}/{image.name}";

        if (File.Exists(filePath))
        {
            Console.WriteLine($"{filePath} already exist");
            return;
        }

        try
        {
            Console.WriteLine($"Downloading image from {image.link} to {filePath}");
            byte[] imageData = await m_httpClient.GetByteArrayAsync(image.link);

            Console.WriteLine($"Saving image from {image.link} to {filePath}");
            File.WriteAllBytes(filePath, imageData);
            Console.WriteLine($"Saved image from {image.link} to {filePath}");
        }
        catch (System.Exception Exception)
        {
            Console.WriteLine($"(Failure) Image download Failed: {image}, {Exception.Message}");
        }
    }
}