using HtmlAgilityPack;

namespace CSharpHelper.ImageScrapers;
public class HaileeSteinfeld_ComScraper
{
    public static bool IsHaileeSteinfeld_ComUrl(string url) => url.StartsWith("https://hailee-steinfeld.com");
    public static bool IsAlbmUrl(string url) => url.StartsWith("https://hailee-steinfeld.com/photos/thumbnails.php?album=");
    public static bool IsCategoryUrl(string url) => url.StartsWith("https://hailee-steinfeld.com/photos/index.php?cat=");

    public static async Task Download(string url, DirectoryInfo parentFolder)
    {
        if (IsAlbmUrl(url))
        {
            Album album = await Album.Get(url);
            await album.Download(parentFolder);
        }
        else if (IsCategoryUrl(url))
        {
            Category category = await Category.Get(url);
            await category.Download(parentFolder);
        }
    }

    public class Album
    {
        public string Name {get; protected set;} = string.Empty;
        public (ushort pageCount, ushort fileCount) Size {get; protected set;}
        public (string link, string name)[] Images {get; protected set;} = [];

        public const ushort MAX_PHOTOS_PER_PAGE = 125;

        public async Task Download(DirectoryInfo parentFolder)
        {
            DirectoryInfo folder = Directory.CreateDirectory($"{parentFolder.FullName}/{Name}");
            await Internet.DownloadImages(Images, folder);
        }

        public static async Task<Album> Get(string url)
        {
            if (!IsAlbmUrl(url))
                throw new Exception($"{url} is not a album");

            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);

            Album album = new Album
            {
                Name = GetName(page),
                Size = GetSize(page),
                Images = await GetImages(page),
            };

            return album;
        }

        public static string GetName(HtmlDocument page)
        {
            HtmlNode nameNode = page.DocumentNode.SelectSingleNode("/html/body/div[3]/article/table[2]/tr[1]/td/span/table/tr/td[1]/h2");
            if (nameNode is null)
                throw new NullReferenceException("Could not find name node");

            string name = nameNode.InnerText.Replace("&#039;", "'").Replace("&quot;", "\"");

            return name;
        }
        public static (ushort pageCount, ushort fileCount) GetSize(HtmlDocument page)
        {
            HtmlNode sizeNode = page.DocumentNode.SelectSingleNode("//span[@class='statlink2']");
            if (sizeNode is null)
                throw new NullReferenceException("Could not find size node");

            string[] innertTextSplitted = sizeNode.InnerText.Split(' ');

            ushort pageCount = Convert.ToUInt16(innertTextSplitted[3]);
            ushort fileCount = Convert.ToUInt16(innertTextSplitted[0]);

            return (pageCount, fileCount);
        }
        public static async Task<(string link, string name)[]> GetImages(HtmlDocument page)
        {
            var size = GetSize(page);
            (string link, string name)[] images = new (string, string)[size.fileCount];
            
            HtmlDocument[] pages = await GetPages(page);

            for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
            {
                HtmlDocument currentPage = pages[pageIndex];
                HtmlNodeCollection thumbnailNodes = currentPage.DocumentNode.SelectNodes("//img[@class='image thumbnail']");

                for (int imageIndex = 0; imageIndex < thumbnailNodes.Count; imageIndex++)
                {
                    HtmlNode thumbnailNode = thumbnailNodes[imageIndex];

                    string src = thumbnailNode.GetAttributeValue("src", "null")
                                              .Replace("thumb_", "");

                    string link = $"https://hailee-steinfeld.com/photos/{src}";
                    string name = src.Split("/")[^1];

                    int index = (pageIndex * MAX_PHOTOS_PER_PAGE) + imageIndex;
                    
                    images[index] = (link, name);
                }
            }

            return images;
        }

        public static async Task<HtmlDocument[]> GetPages(HtmlDocument page)
        {
            var size = GetSize(page);
            
            HtmlDocument[] pages = new HtmlDocument[size.pageCount];
            pages[0] = page;

            if (size.pageCount == 1)
                return pages;

            HtmlNode albumLinkNode = page.DocumentNode.SelectSingleNode("/html/body/div[3]/article/table[1]/tr/td/span").ChildNodes[^1];
            string href = albumLinkNode.GetAttributeValue("href", "null");
            string link = $"https://hailee-steinfeld.com/photos/{href}";

            for (int i = 1; i < size.pageCount; i++)
            {
                string pageLink = $"{link}&page={i + 1}";
                pages[i] = await Internet.GetStaticPage_HTTPClientAsync(pageLink);
            }

            return pages;
        }
    }
    public class Category
    {
        public string Name {get; protected set;} = string.Empty;
        public Category[] SubCategories {get; protected set;} = [];
        public Album[] Albums {get; protected set;} = [];


        public async Task Download(DirectoryInfo parentFolder)
        {
            DirectoryInfo folder = Directory.CreateDirectory($"{parentFolder.FullName}/{Name}");   

            foreach (var item in SubCategories)
            {
                await item.Download(folder);
            }

            foreach (var item in Albums)
            {
                await item.Download(folder);
            }
        }

        public static async Task<Category> Get(string url)
        {
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);

            Category category = new Category
            {
                Name = GetName(page),
                SubCategories = await GetSubCategories(page),
                Albums = await GetAlbums(page)
            };

            return category;
        }

        public static string GetName(HtmlDocument page)
        {
            HtmlNode nameNode = page.DocumentNode.SelectNodes("//td[@class='tableh1']")[0];

            string name = nameNode.InnerText.Split("-")[1]
                                            .Trim();

            return name;
        }
        public static async Task<Category[]> GetSubCategories(HtmlDocument page)
        {
            HtmlNodeCollection catLinkNodes = page.DocumentNode.SelectNodes("//span[@class='catlink']");
            if (catLinkNodes is null)
                return Array.Empty<Category>();

            Category[] categories = new Category[catLinkNodes.Count];
            
            for (int i = 0; i < categories.Length; i++)
            {
                HtmlNode catLinkNode = catLinkNodes[i];
                
                string href = catLinkNode.ChildNodes[0].GetAttributeValue("href", "null");
                string link = $"https://hailee-steinfeld.com/photos/{href}";

                Category category = await Get(link);
                categories[i] = category;
            }

            return categories;
        }
        public static async Task<Album[]> GetAlbums(HtmlDocument page)
        {
            HtmlNode currentPageNode = page.DocumentNode.SelectSingleNode("//span[@class='breadstat']").ChildNodes[^1];
            string currentPageHref = currentPageNode.GetAttributeValue("href", "null");
            string currentPageLink = $"https://hailee-steinfeld.com/photos/{currentPageHref}";

            HtmlNode infoNode = page.DocumentNode.SelectSingleNode("//span[@class='statlink2']");

            ushort albumCount = Convert.ToUInt16(infoNode.InnerText.Split(" ")[0]);
            ushort pageCount = Convert.ToUInt16(infoNode.InnerText.Split(" ")[3]);

            List<Album> albums = new List<Album>();
            HtmlDocument[] pages = new HtmlDocument[pageCount];
            pages[0] = page;

            for (int i = 1; i < pageCount; i++)
            {
                string pageUrl = $"{currentPageLink}&page={i + 1}";
                pages[i] = await Internet.GetStaticPage_HTTPClientAsync(pageUrl);
            }

            for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
            {
                HtmlNodeCollection albumNodes = pages[pageIndex].DocumentNode.SelectNodes("//a[@class='albums']");
                
                for (int albumNodeIndex = 0; albumNodeIndex < albumNodes.Count; albumNodeIndex++)
                {
                    HtmlNode albumNode = albumNodes[albumNodeIndex];

                    string href = albumNode.GetAttributeValue("href", "null");
                    string link = $"https://hailee-steinfeld.com/photos/{href}";

                    Album album = await Album.Get(link);
                    albums.Add(album);
                }
            }

            return albums.ToArray();
        }
    }
}