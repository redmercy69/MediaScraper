using HtmlAgilityPack;

namespace CSharpHelper.ImageScrapers;
public static class SadieSinkFan_ComScraper
{
    public static bool IsSadieSink_Com(string url) => url.StartsWith("https://sadiesinkfan.com");
    public static bool IsAlbumUrl(string url) => url.StartsWith("https://sadiesinkfan.com/gallery/thumbnails.php?album");

    public const ushort IMAGES_PER_ALBUM_PAGE = 60;

    public static async Task Download(string url, DirectoryInfo parentFolder)
    {
        if (IsAlbumUrl(url))
            await DownloadAlbum(url, parentFolder);
    }

    public static async Task DownloadAlbum(string url, DirectoryInfo parentFolder)
    {
        Album album = await Album.Get(url);
        await album.Download(parentFolder);
    }

    public class Album
    {
        public string Name {get; private set;} = string.Empty;
        public (uint fileCount, ushort pageCount) Size {get; private set;}
        public (string link, string name)[] Images {get; private set;} = Array.Empty<(string link, string name)>();

        public static async Task<Album> Get(string url)
        {            
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);

            Album album = new Album
            {
                Name = GetName(page),
                Size = GetSize(page),
                Images = await GetImages(page)
            };

            return album;
        }


        public async Task Download(DirectoryInfo parentFolder)
        {
            DirectoryInfo directory = Directory.CreateDirectory($"{parentFolder}/{Name}");
            await Internet.DownloadImages(Images, directory);
        }

        public static string GetName(HtmlDocument page)
        {
            string name = page.DocumentNode.SelectSingleNode("/html/body/center/table/tr/td/table/tr/td/table[2]/tr[1]/td/table/tr/td[1]/h2").InnerText;
            return name;
        }
        public static (uint fileCount, ushort pageCount) GetSize(HtmlDocument page)
        {
            (uint fileCount, ushort pageCount) size = (0, 0);

            HtmlNode sizeNode = page.DocumentNode.SelectSingleNode("/html/body/center/table/tr/td/table/tr/td/table[2]/tr[12]/td/table/tr/td[1]");
            string[] sizeSubStrings = sizeNode.InnerText.Split(" ");

            size.fileCount = Convert.ToUInt32(sizeSubStrings[0]);
            size.pageCount = Convert.ToUInt16(sizeSubStrings[3]);

            return size;
        }
        public static async Task<(string link, string name)[]> GetImages(HtmlDocument page)
        {
            var albumSize = GetSize(page);
            var images = new (string link, string name)[albumSize.fileCount];

            HtmlDocument[] pages = await GetPages(page);

            for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
            {
                HtmlNodeCollection thumbnails = pages[pageIndex].DocumentNode.SelectNodes("//img[@class='image thumbnail']");
                for (int imageIndex = 0; imageIndex < thumbnails.Count; imageIndex++)
                {
                    HtmlNode thumbnail = thumbnails[imageIndex];
                    
                    string name = thumbnail.GetAttributeValue("alt", "null");
                    string href = thumbnail.GetAttributeValue("src", "null").Replace("thumb_", "");

                    string link = $"https://sadiesinkfan.com/gallery/{href}";

                    int index = (IMAGES_PER_ALBUM_PAGE * pageIndex) + imageIndex;
                    images[index] = (link, name);
                }
            }

            return images;
        }

        public static async Task<HtmlDocument[]> GetPages(HtmlDocument page)
        {
            var albumSize = GetSize(page);
            HtmlNode currentAlbumNode = page.DocumentNode.SelectSingleNode("/html/body/center/table/tr/td/table/tr/td/table[1]/tr/td/span/a[4]");
            string   currentAlbumNodeHref = currentAlbumNode.GetAttributeValue("href", "");

            string albumUrl = $"https://sadiesinkfan.com/gallery/{currentAlbumNodeHref}";

            HtmlDocument[] pages = new HtmlDocument[albumSize.pageCount];

            for (int i = 0; i < albumSize.pageCount; i++)
            {
                string pageUrl = $"{albumUrl}&page={i + 1}";
                pages[i] = await Internet.GetStaticPage_HTTPClientAsync(pageUrl); 
            }

            return pages;
        }
    }

}