using HtmlAgilityPack;

namespace CSharpHelper.ImageScrapers;

public static class EllaPurnellPhotos_ComScraper
{
    public static async Task Download(string url, DirectoryInfo parentFolder)
    {
        Album album = await Album.Get(url);
        await Internet.DownloadImages(album.Images, parentFolder.FullName);
    }
    public static async Task Download(HtmlDocument page, DirectoryInfo parentFolder)
    {
        Album album = await Album.Get(page);
        await Internet.DownloadImages(album.Images, parentFolder.FullName);
    }

    public class Album
    {
        public string Name { get; set; } = "";
        public (string link, string name)[] Images { get; set; } = [];

        public static async Task<Album> Get(string url)
        {
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);
            return await Get(page);
        }
        public static async Task<Album> Get(HtmlDocument page)
        {
            return new Album
            {
                Name = GetName(page),
                Images = await GetImages(page)
            };
        }
        public static string GetName(HtmlDocument page) => page.DocumentNode.SelectNodes("//span[@class='statlink']")[0].ChildNodes[^1].InnerText;
        public static string GetLink(HtmlDocument page) 
        {
            string href = page.DocumentNode.SelectNodes("//span[@class='statlink']")[0].ChildNodes[^1].GetAttributeValue("href", "no href");
            string link = $"https://ellapurnellphotos.com/{href}";
            return link;
        }
        public static string GetPageLink(HtmlDocument page, int pageIndex)
        {
            string link = GetLink(page);
            link = $"{link}&page={pageIndex}";
            return link;
        }
        public static string[] GetPageLinks(HtmlDocument page, int startPageIndex, int pageCount)
        {
            string firstPageLink = GetLink(page);
            string[] pageLinks = new string[pageCount];
            pageLinks[0] = firstPageLink;

            for (int i = 1; i < pageCount; i++)
            {
                pageLinks[i] = GetPageLink(page, startPageIndex + i);
            }

            return pageLinks;
        }

        public static async Task<(string link, string name)[]> GetImages(HtmlDocument page)
        {
            var size = GetSize(page);
            HtmlDocument[] pages = await GetPages(page, size.pageCount);
            List<(string link, string name)> images = [];

            Console.WriteLine(size.imageCount);
            Console.WriteLine(size.imagesPerPage);
            Console.WriteLine(size.pageCount);

            for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
            {
                HtmlNodeCollection imageNodes = pages[pageIndex].DocumentNode.SelectNodes("//img[@class='image thumbnail']");
                Console.WriteLine(imageNodes.Count);

                for (int imageIndex = 0; imageIndex < imageNodes.Count; imageIndex++)
                {
                    HtmlNode imageNode = imageNodes[imageIndex];

                    string imageName = imageNode.GetAttributeValue("alt", "no alt found");
                    string imageLink = $"https://ellapurnellphotos.com/{imageNode.GetAttributeValue("src", "no source").Replace("thumb_", "")}";

                    images.Add((imageLink, imageName));
                }
            }

            return [.. images];
        }

        public static (int pageCount, int imageCount, int imagesPerPage) GetSize(HtmlDocument page)
        {
            HtmlNode infoNode = page.DocumentNode.SelectSingleNode("//*[text()[contains(., 'files on')]]");

            int pageCount = Convert.ToInt32(infoNode.InnerText.Split(" ")[^2]);
            int imageCount = Convert.ToInt32(infoNode.InnerText.Split(" ")[0]);
            int imagesPerPage = imageCount;

            if (pageCount > 1)
            {
                HtmlNodeCollection imageLinkNodes = page.DocumentNode.SelectNodes("//img[@class='image thumbnail']");
                imagesPerPage = imageLinkNodes.Count;
            }

            return (pageCount, imageCount, imagesPerPage);
        }

        public static async Task<HtmlDocument[]> GetPages(HtmlDocument firstPage, int pageCount=-1)
        {
            if (pageCount==-1)
                pageCount = GetSize(firstPage).pageCount;
            
            if (pageCount == 1)
                return [firstPage];

            string[] links = GetPageLinks(firstPage, startPageIndex: 0, pageCount);
            HtmlDocument[] pages = await Internet.GetStaticPages_HTTPClientAsync(links);

            return pages;
        }

        public override string ToString()
        {
            return $"Name: {Name}";
        }
    }
}