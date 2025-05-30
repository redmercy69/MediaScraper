using HtmlAgilityPack;
using CSharpHelper;

using CSharpHelper.ImageScrapers;

namespace ImageScraper;
public class Program
{
    public static DirectoryInfo downloadedMediaFolder;

    public static void Main(string[] args)
    {
        if (args.Length == 0)
            throw new Exception("No links to parse, pass them as a program argument");
            
        downloadedMediaFolder = new DirectoryInfo("Downloaded Media");
        Directory.CreateDirectory(downloadedMediaFolder.FullName);

        Task scrapeLinksTake = ScrapeLinks(args);
        scrapeLinksTake.Wait();
    }

    public static async Task ScrapeLinks(string[] links)
    {
        foreach (var link in links)
        {
            Console.WriteLine($"scraping {link}");
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(link);
            
            if (CopperminePhotoGallery.IsCopperminePhotoGalleryPage(page))
                await CopperminePhotoGallery.Download(link, downloadedMediaFolder);
            else if (HaileeSteinfeld_ComScraper.IsHaileeSteinfeld_ComUrl(link))
                await HaileeSteinfeld_ComScraper.Download(link, downloadedMediaFolder);
            else if (SadieSinkFan_ComScraper.IsSadieSink_Com(link))
                await SadieSinkFan_ComScraper.Download(link, downloadedMediaFolder);
            else if (SophiaLillisFan_ComScraper.IsSophiaLillisFan_Com(link))
                await SophiaLillisFan_ComScraper.Download(link, downloadedMediaFolder);
            else
            {
                HtmlNodeCollection imageNodes = page.DocumentNode.SelectNodes("//img");
                var images = new (string link, string name)[imageNodes.Count];

                Uri uri = new Uri(link);
                string host = uri.Host;
                string scheme = uri.Scheme;
                
                for (int i = 0; i < images.Length; i++)
                {
                    HtmlNode imageNode = imageNodes[i];

                    string src = imageNode.GetAttributeValue("src", "no src");
                    string fileName = src.Split("/")[^1];

                    string imageLink = scheme + "://" + host + src;
                    images[i] = (imageLink, fileName);
                }

                await Internet.DownloadImages(images, downloadedMediaFolder);
            }
            
        }
    }
}
    