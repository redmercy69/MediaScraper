using HtmlAgilityPack;

namespace CSharpHelper.ImageScrapers;
public static class KendallJennerScraper_Com_Br
{
    public static bool IsValidLink(string link) => link.StartsWith("https://kendalljenner.com.br");

    public static async Task<(string link, string name)[]> GetAlbumPhotos(string url)
    {
        HtmlDocument[] pages = await GetAlbumPages(url);
        var albumSize = GetAlbumSize(pages[0]);
        Console.WriteLine(albumSize);

        var images = new (string link, string name)[albumSize.imageCount];

        for (int i = 0; i < pages.Length; i++)
        {
            HtmlDocument page = pages[i];

            HtmlNodeCollection thumbnailNodes = page.DocumentNode.SelectNodes("//img[@class='image thumbnail']");
            for (int y = 0; y < thumbnailNodes.Count; y++)
            {
                HtmlNode thumbnailNode = thumbnailNodes[y];

                string src = $"https://kendalljenner.com.br/gallery/{thumbnailNode.GetAttributeValue("src", "No Source").Replace("thumb_", "")}";
                string fileName = thumbnailNode.GetAttributeValue("alt", "No Name");

                int imageIndex = (albumSize.pageCount * i) + y;
                images[imageIndex] = (src, fileName);
            }
        }

        return images;
    }

    public static async Task<HtmlDocument[]> GetAlbumPages(string url)
    {
        HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);
        var albumSize = GetAlbumSize(page);

        HtmlDocument[] pages = new HtmlDocument[albumSize.pageCount];
        pages[0] = page;

        for (int i = 1; i < albumSize.pageCount; i++)
            pages[i] = await Internet.GetStaticPage_HTTPClientAsync($"{url}&page={i + 1}");
        
        return pages;
    }
    public static (int pageCount, int imageCount) GetAlbumSize(HtmlDocument page)
    {
        HtmlNode infoNode = page.DocumentNode.SelectSingleNode("//*[text()[contains(., 'files on')]]");

        int pageCount = Convert.ToInt32(infoNode.InnerText.Split(" ")[^2]);
        int fileCount = Convert.ToInt32(infoNode.InnerText.Split(" ")[0]);

        return (pageCount, fileCount);
    }
}