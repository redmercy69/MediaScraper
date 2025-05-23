using System.IO;
using HtmlAgilityPack;

namespace CSharpHelper.ImageScrapers;
public static class Listal_ComScraper
{
    public static bool IsListalUrl(string url) => url.StartsWith("https://www.listal.com");
    public static bool IsPicturesUrl(string url) => url.StartsWith("https://www.listal.com") && url.EndsWith("/pictures");

    public static async Task Download(string url, DirectoryInfo parentFolder)
    {
        if (!IsListalUrl(url))
            throw new NotSupportedException($"{url} does not point to listal.com");

        if (IsPicturesUrl(url))
            await DownloadPictures(url, parentFolder);
    }

    public static async Task DownloadPictures(string url, DirectoryInfo parentFolder)
    {
        if (!IsPicturesUrl(url))
            throw new NotSupportedException($"{url} is not a listal pictures url");

        List<HtmlDocument> pages = new List<HtmlDocument>();

        HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);
        pages.Add(page);

        //Get Name
        HtmlNode nameNode = page.DocumentNode.SelectSingleNode("/html/body/div[3]/div[2]/div/div[1]/div[1]/h1/a");
        string name = nameNode.InnerText;

        DirectoryInfo picturesFolder = Directory.CreateDirectory($"{parentFolder.FullName}/{name}/pictures");

        //Get Pages
        HtmlNode pagesNode = page.DocumentNode.SelectSingleNode("//div[@class='pages']");
        HtmlNodeCollection pageLinkNodes = pagesNode.ChildNodes;
        
        for (int i = 3; i < pageLinkNodes.Count; i += 2)
        {
            HtmlNode node = pageLinkNodes[i];
            
            string href = node.GetAttributeValue("href", "null");
            string link = $"https://www.listal.com{href}";

            pages.Add(await Internet.GetStaticPage_HTTPClientAsync(link));
        }
    }
}