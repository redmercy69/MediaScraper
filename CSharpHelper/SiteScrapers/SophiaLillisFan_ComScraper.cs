using System.ComponentModel;
using HtmlAgilityPack;

namespace CSharpHelper.ImageScrapers;
public static class SophiaLillisFan_ComScraper
{
    public const ushort MAX_PHOTOS_PER_PAGE = 60;

    public static bool IsSophiaLillisFan_Com(string url) => url.StartsWith("https://sophialillisfan.com");
    public static bool IsAlbumUrl(string url) => url.StartsWith("https://sophialillisfan.com/gallery/thumbnails.php?album");
    public static bool IsCatergoryPage(string url) => url.StartsWith("https://sophialillisfan.com/gallery/index.php?cat");

//Download
    public static async Task Download(string link, DirectoryInfo parentFolder)
    {
        if (IsAlbumUrl(link))
            await DownloadAlbum(link, parentFolder);
        else if (IsCatergoryPage(link))
            await DownloadCategory(link, parentFolder);
    }
    
    public static async Task DownloadAlbum(string link, DirectoryInfo parentFolder)
    {
        if (!IsAlbumUrl(link))
            throw new Exception($"{link} is not a album");
            
        Album album = await Album.Get(link);
        
        await album.Download(parentFolder);
    }
    
    public static async Task DownloadCategory(string link, DirectoryInfo parentFolder)
    {
        Category category = await Category.Get(link);
        await DownloadCategory(category, parentFolder);
    }
    public static async Task DownloadCategory(Category category, DirectoryInfo parentFolder)
    {
        foreach (var album in category.Albums)
        {
            await album.Download(parentFolder);
        }

        foreach (var subCategory in category.SubCategories)
        {
            await DownloadCategory(subCategory, parentFolder);
        }
    }

    public class Album
    {
        public string Name {get; protected set;}

        public string WebsitePath {get; protected set;}

        public (string link, string name)[] Images {get; protected set;}
        public ushort ImageCount => Convert.ToUInt16(Images.Length);
        public ushort PageCount {get; protected set;}

        public async Task Download(DirectoryInfo parentDirectory)
        {
            DirectoryInfo directory = Directory.CreateDirectory($"{parentDirectory.FullName}/{WebsitePath}/{Name}");
            
            await Internet.DownloadImages(Images, directory);
        }

        public static async Task<Album> Get(string url)
        {
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);

            Album album = new()
            {
                Images = await GetImages(page),
                PageCount = GetSize(page).pageCount,
                WebsitePath = GetWebsitePath(page)
            };

            return album;
        }

        public static string GetName(HtmlDocument page)
        {
            HtmlNode nameNode = page.DocumentNode.SelectSingleNode("/html/body/center/table/tr/td/table/tr/td/table[2]/tr[1]/td/table/tr/td[1]/h2");
            string name = nameNode.InnerText;
            return name;
        }
        public static string GetWebsitePath(HtmlDocument page)
        {
            string path = string.Empty;

            HtmlNode statLinkNode = page.DocumentNode.SelectSingleNode("//span[@class='statlink']");
            HtmlNodeCollection children = statLinkNode.ChildNodes;

            foreach (var child in children)
            {
                string innerText = child.InnerText;

                if (innerText == "Home")
                    continue;

                innerText = innerText.Replace("&amp;", "&");

                path += innerText switch
                {
                    " > " => "/",
                    _ => innerText,
                };
            }

            return path;
        }

        public static (ushort pageCount, ushort fileCount) GetSize(HtmlDocument page)
        {
            HtmlNodeCollection h1Tables = page.DocumentNode.SelectNodes("//*[contains(@class,'tableh1')]");
            HtmlNode filesNode = null;
            foreach (var item in h1Tables)
            {
                if (item.InnerText.Trim().Contains("page(s)"))
                {
                    filesNode = item;
                }
            }
            string filesNodeInnerText = filesNode.InnerText.Trim();
            string[] filesNodeInnerTextSplitted = filesNodeInnerText.Split(" ");

            ushort fileCount = Convert.ToUInt16(filesNodeInnerTextSplitted[0]);
            ushort pageCount = Convert.ToUInt16(filesNodeInnerTextSplitted[3]);

            return (pageCount, fileCount);
        }

        public static async Task<(string link, string name)[]> GetImages(HtmlDocument firstPage)
        {
            HtmlDocument[] pages = await GetPages(firstPage);
            var images = await GetImages(pages);

            return images;
        }

        public static async Task<HtmlDocument[]> GetPages(HtmlDocument firstPage)
        {
            var albumSize = GetSize(firstPage);

            HtmlNode firstImageNode = firstPage.DocumentNode.SelectNodes("//img[@class='image thumbnail']")[0];
            HtmlNode parentAnchor = firstImageNode.ParentNode;
            string parentAnchorHref = parentAnchor.GetAttributeValue("href", "");
        
            string albumUrl = $"https://sophialillisfan.com/gallery/{parentAnchorHref.Split("&")[0].Replace("displayimage", "thumbnails")}";

            HtmlDocument[] pages = new HtmlDocument[albumSize.pageCount];
            pages[0] = firstPage;

            for (int i = 1; i < albumSize.pageCount; i++)
            {
                string url = $"{albumUrl}&page={i + 1}";
                pages[i] = await Internet.GetStaticPage_HTTPClientAsync(url);
            }

            return pages;
        }
        public static async Task<(string link, string name)[]> GetImages(HtmlDocument[] pages)
        {
            var size = GetSize(pages[0]);
            var images = new (string link, string name)[size.fileCount];

            for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
            {
                HtmlDocument page = pages[pageIndex];
                HtmlNodeCollection imageNodes = page.DocumentNode.SelectNodes("//img[@class='image thumbnail']");

                for (int imageNodeIndex = 0; imageNodeIndex < imageNodes.Count; imageNodeIndex++)
                {
                    HtmlNode imageNode = imageNodes[imageNodeIndex];

                    string src = imageNode.GetAttributeValue("src", " null").Replace("thumb_", string.Empty);
                    src = $"https://sophialillisfan.com/gallery/{src}";

                    string alt = imageNode.GetAttributeValue("alt", " null");
                    
                    int imageIndex = (MAX_PHOTOS_PER_PAGE * pageIndex) + imageNodeIndex;

                    images[imageIndex] = (src, alt);
                }
            }

            return images;
        }
    }
    public class Category
    {
        public string Name {get; protected set;}

        public Album[] Albums {get; protected set;}
        public Category[] SubCategories {get; protected set;}

        public static async Task<Category> Get(string url)
        {
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);
            Category subCategory = new Category
            {
                Albums = await GetAlbums(page),
                SubCategories = await GetCategories(page)
            };

            return subCategory;
        }
        public static async Task<Album[]> GetAlbums(HtmlDocument page)
        {
            HtmlNodeCollection albumLinkNodes = page.DocumentNode.SelectNodes("//a[@class='albums']");
            Album[] albums = new Album[albumLinkNodes != null ? albumLinkNodes.Count : 0];

            for (int i = 0; i < albums.Length; i++)
            {
                HtmlNode albumLinkNode = albumLinkNodes[i];

                string href = albumLinkNode.GetAttributeValue("href", "null");
                string link = $"https://sophialillisfan.com/gallery/{href}";

                Album album = await Album.Get(link);
                albums[i] = album;
            }

            return albums;
        }
        public static async Task<Category[]> GetCategories(HtmlDocument page)
        {
            HtmlNodeCollection catLinkNodes = page.DocumentNode.SelectNodes("//span[@class='catlink']");
            Category[] categories = new Category[catLinkNodes != null ? catLinkNodes.Count : 0];


            for (int i = 0; i < categories.Length; i++)
            {
                HtmlNode catLinkNode = catLinkNodes[i].ChildNodes[0];

                string href = catLinkNode.GetAttributeValue("href", "null");
                string link = $"https://sophialillisfan.com/gallery/{href}";

                Category category = await Get(link);
                categories[i] = category;
            }

            return categories;
        }
    }
}