using HtmlAgilityPack;

namespace CSharpHelper.ImageScrapers;
public static class MillieBobbyBrown_Com_BrScraper
{
    public const ushort MAX_PHOTOS_PER_ALBUM_PAGE = 18,
                        MAX_ALBUMS_PER_CATEGORY_PAGE = 12;

    public static bool IsMillieBobbyBrown_Com_Br(string url) => url.StartsWith("https://milliebobbybrown.com.br/");
    public static bool IsAlbumUrl(string url) => url.StartsWith("https://milliebobbybrown.com.br/galeria/thumbnails.php?album");
    public static bool IsCategoryUrl(string url) => url.StartsWith("https://milliebobbybrown.com.br/galeria/index.php?cat");

    public static async Task Download(string url, DirectoryInfo parentFolder)
    {
        if (IsAlbumUrl(url))
            await DownloadAlbum(url, parentFolder);
        else if (IsCategoryUrl(url))
            await DownloadCategory(url, parentFolder);
    }
    public static async Task DownloadAlbum(string url, DirectoryInfo parentFolder)
    {
        Album album = await Album.Get(url);
        await album.Download(parentFolder);
    }
    public static async Task DownloadCategory(string url, DirectoryInfo parentFolder)
    {
        Category category = await Category.Get(url);
        await category.Download(parentFolder);
    }

    public class Album
    {
        public string Name {get; protected set;}

        public (string link, string name)[] Images {get; protected set;}
        public ushort PageCount {get; protected set;}

        public string Url {get; protected set;}
        public HtmlDocument Page {get; protected set;}

        public async Task Download(DirectoryInfo parentFolder)
        {
            DirectoryInfo directory = Directory.CreateDirectory($"{parentFolder}/{Name}");
            await Internet.DownloadImages(Images, directory);
        }
        public static async Task Download(Album[] albums, DirectoryInfo parentFolder)
        {
            Task[] downloadTasks = new Task[albums.Length];

            for (int i = 0; i < downloadTasks.Length; i++)
            {
                downloadTasks[i] = albums[i].Download(parentFolder);
            }

            await Task.WhenAll(downloadTasks);
        }

        public static async Task<Album> Get(string url)
        {
            Console.WriteLine($"Getting MBBR album from {url}");
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);

            Album album = new Album
            {
                Name = GetName(page),
                Images = await GetImages(page),
                PageCount = GetSize(page).pageCount,

                Url = url,
                Page = page
            };

            return album;
        }

        public static string GetName(HtmlDocument page)
        {
            HtmlNode statLinkNode = page.DocumentNode.SelectSingleNode("//span[@class='statlink']");
            HtmlNodeCollection childNodes = statLinkNode.ChildNodes;
            string name = childNodes[^1].InnerText;

            return name;
        }
        public static (ushort pageCount, ushort fileCount) GetSize(HtmlDocument page)
        {
            const string INFO_NODE_XPATH = "//td[@class='tableh1_info']";

            if (page is null)
                throw new NullReferenceException("page is null");

            if (!IsAlbumPage(page))
                throw new Exception("the page given was not a album page");

            HtmlNode infoNode = page.DocumentNode.SelectSingleNode(INFO_NODE_XPATH);

            string[] innerTexts = infoNode.InnerText.Split(" ");

            ushort pageCount = Convert.ToUInt16(innerTexts[3]);
            ushort fileCount = Convert.ToUInt16(innerTexts[0]);

            return (pageCount, fileCount);
        }
        public static async Task<HtmlDocument[]> GetPages(HtmlDocument page)
        {
            ushort pageCount = GetSize(page).pageCount;
            HtmlDocument[] pages = new HtmlDocument[pageCount];
            pages[0] = page;

            HtmlNode firstThumbnailNode = page.DocumentNode.SelectNodes("//img[@class='image thumbnail']")[0];
            string firstThumbnailHref = firstThumbnailNode.ParentNode.GetAttributeValue("href", "null");

            string albumLink = $"https://milliebobbybrown.com.br/galeria/{firstThumbnailHref.Split("&")[0].Replace("displayimage", "thumbnails")}";

            if (pageCount == 1)
                return pages;

            for (int i = 1; i < pageCount; i++)
            {
                string pageUrl = $"{albumLink}&page={i + 1}";
                pages[i] = await Internet.GetStaticPage_HTTPClientAsync(pageUrl);
            }

            return pages;
        }

        public static async Task<(string link, string name)[]> GetImages(HtmlDocument firstPage)
        {
            var size = GetSize(firstPage);
            var images = new (string link, string name)[size.fileCount];

            HtmlDocument[] pages = await GetPages(firstPage);

            for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
            {
                HtmlDocument page = pages[pageIndex];
                HtmlNodeCollection thumbnailNodes = page.DocumentNode.SelectNodes("//img[@class='image thumbnail']");

                for (int imageIndex = 0; imageIndex < thumbnailNodes.Count; imageIndex++)
                {
                    HtmlNode thumbnailNode = thumbnailNodes[imageIndex];

                    string src = thumbnailNode.GetAttributeValue("src", "null");
                    string imageName = thumbnailNode.GetAttributeValue("alt", "null");

                    string link = $"https://milliebobbybrown.com.br/galeria/{src.Replace("thumb_", "")}";

                    int index = (pageIndex * MAX_PHOTOS_PER_ALBUM_PAGE) + imageIndex;
                    images[index] = (link, imageName);
                }
            }

            return images;
        }
    
        public static bool IsAlbumPage(HtmlDocument page)
        {
            HtmlNode statLinkNode = page.DocumentNode.SelectSingleNode("//span[@class='statlink']");
            HtmlNodeCollection childNodes = statLinkNode.ChildNodes;
            HtmlNode currentPageNode = childNodes[^1];

            string href = currentPageNode.GetAttributeValue("href", "null");

            return href.StartsWith("thumbnails.php?album");
        }
    }

    public class Category
    {
        public string Name {get; protected set;}
        public Album[] Albums {get; protected set;}
        public Category[] SubCategories {get; protected set;}

        public string Url {get; protected set;}
        public HtmlDocument Page {get; protected set;}


        public async Task Download(DirectoryInfo parentFolder)
        {
            DirectoryInfo directory = Directory.CreateDirectory($"{parentFolder.FullName}/{Name}");

            for (int i = 0; i < Albums.Length; i++)
            {
                Album album = Albums[i];
                await album.Download(directory);
            }

            for (int i = 0; i < SubCategories.Length; i++)
            {
                Category subCategory = SubCategories[i];
                await subCategory.Download(directory);
            }
        }

        public static async Task<Category> Get(string url)
        {
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);

            Category category = new Category
            {
                Name = GetName(page),
                Albums = await GetAlbums(page),
                SubCategories = await GetSubCategories(page),

                Url = url,
                Page = page
            };

            return category;
        }

        public static string GetName(HtmlDocument page)
        {
            HtmlNodeCollection statLinkNodes = page.DocumentNode.SelectSingleNode("//span[@class='statlink']").ChildNodes;
            HtmlNode nameNode = statLinkNodes[^1];

            return nameNode.InnerText;
        }
        public static async Task<Album[]> GetAlbums(HtmlDocument page)
        {
            HtmlNode infoNode = page.DocumentNode.SelectSingleNode("//td[@class='tableh1_info']");
            if (infoNode is null)
                return Array.Empty<Album>();

            HtmlNode statLinkLastChild = page.DocumentNode.SelectSingleNode("//span[@class='statlink']").ChildNodes[^1];

            string baseLink = $"https://milliebobbybrown.com.br/galeria/{statLinkLastChild.GetAttributeValue("href", "null")}";

            ushort albumCount = Convert.ToUInt16(infoNode.InnerText.Split(" ")[0]);
            ushort pageCount = Convert.ToUInt16(infoNode.InnerText.Split(" ")[3]);

            HtmlDocument[] pages = new HtmlDocument[pageCount];
            pages[0] = page;

            for (int i = 1; i < pageCount; i++)
            {
                string pageUrl = $"{baseLink}&page={i + 1}";
                pages[i] = await Internet.GetStaticPage_HTTPClientAsync(pageUrl);
            }

            Album[] albums = new Album[albumCount];

            for (int i = 0; i < pages.Length; i++)
            {
                HtmlNodeCollection albumNodes = pages[i].DocumentNode.SelectNodes("//a[@class='albums']");

                for (int albumNodeIndex = 0; albumNodeIndex < albumNodes.Count; albumNodeIndex++)
                {
                    HtmlNode albumNode = albumNodes[albumNodeIndex];

                    string href = albumNode.GetAttributeValue("href", "null");
                    string link = $"https://milliebobbybrown.com.br/galeria/{href}";

                    int albumIndex = (i * MAX_ALBUMS_PER_CATEGORY_PAGE) + albumNodeIndex;

                    Album album = await Album.Get(link);
                    albums[albumIndex] = album;
                }
            }

            return albums;
        }
        public static async Task<Category[]> GetSubCategories(HtmlDocument page)
        {
            HtmlNodeCollection catLinks = page.DocumentNode.SelectNodes("//span[@class='catlink']");
            if (catLinks is null)
                return Array.Empty<Category>();

            Category[] categories = new Category[catLinks.Count];

            for (int i = 0; i < catLinks.Count; i++)
            {
                HtmlNode catLink = catLinks[i].ChildNodes[0];

                string href = catLink.GetAttributeValue("href", "null");
                string link = $"https://milliebobbybrown.com.br/galeria/{href}";
                
                Category category = await Get(link);
                categories[i] = category;
            }

            return categories;
        }
    }
}