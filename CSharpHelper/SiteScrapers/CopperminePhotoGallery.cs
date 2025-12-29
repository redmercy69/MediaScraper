using HtmlAgilityPack;

namespace CSharpHelper.ImageScrapers;
public static class CopperminePhotoGallery
{

    public static async Task<bool> IsCopperminePhotoGalleryPage(string url)
    {
        HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);
        return IsCopperminePhotoGalleryPage(page);
    }
    public static bool IsCopperminePhotoGalleryPage(HtmlDocument page) => page.ParsedText.Contains("Coppermine Photo Gallery");

    public static bool IsCategoryPage(string url) => url.Contains("index.php?cat");
    public static bool IsAlbumPage(string url) => url.Contains("thumbnails.php?album");

    public static async Task Download(string url, DirectoryInfo parentFolder)
    {
        if (IsCategoryPage(url))
        {
            Category category = await Category.Get(url);
            await Download(category, parentFolder);
        }
        else if (IsAlbumPage(url))
        {
            Album album = await Album.Get(url);
            await Download(album, parentFolder);
        }
    }

    public static async Task Download(Category category, DirectoryInfo parentFolder)
    {
        string folderPath = $"{parentFolder.FullName}/{category.Name}";
        DirectoryInfo folder = Directory.CreateDirectory(folderPath);

        for (int i = 0; i < category.Categories.Length; i++)
            await Download(category.Categories[i], folder);

            
        for (int i = 0; i < category.Albums.Length; i++)
            await Download(category.Albums[i], folder);
    }

    public static async Task Download(Album album, DirectoryInfo parentFolder)
    {
        string folderPath = $"{parentFolder.FullName}/{album.Name}";
        DirectoryInfo folder = Directory.CreateDirectory(folderPath);

        await Internet.DownloadImages(album.Images, folder);
    }

    public class Category
    {
        public string Name {get; protected set;} = string.Empty;
        public Category[] Categories {get; protected set;} = [];
        public Album[] Albums {get; protected set;} = [];

        public static async Task<Category> Get(string url)
        {
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);

            Category category = new()
            {
                Name = GetName(page),
                Categories = await GetCategories(page),
                Albums = await GetAlbums(url, page)

            };

            return category;
        }

        public static string GetName(HtmlDocument page) => page.DocumentNode.SelectNodes("//span[@class='statlink']")[0].ChildNodes[^1].InnerText;
        public static async Task<Category[]> GetCategories(HtmlDocument page)
        {
            HtmlNodeCollection catLinkNodes = page.DocumentNode.SelectNodes("//span[@class='catlink']");
            if (catLinkNodes is null)
                return [];

            var downloadCategorieTasks = new Task<Category>[catLinkNodes.Count];

            for (int i = 0; i < catLinkNodes.Count; i++)
            {
                string href = catLinkNodes[i].ChildNodes[0].GetAttributeValue("href", "No href");
                string link = $"https://kendalljenner.com.br/gallery/{href}";

                downloadCategorieTasks[i] = Get(link);
            }

            Category[] categories = await Task.WhenAll(downloadCategorieTasks);
            return categories;
        }
        public static async Task<Album[]> GetAlbums(string url, HtmlDocument? page=null)
        {
            page ??= await Internet.GetStaticPage_HTTPClientAsync(url);

            var size = GetSize(page);
            var downloadAlbumTasks = new Task<Album>[size.albumCount];

            HtmlDocument[] pages = await GetPages(url, page);
            for (int i = 0; i < pages.Length; i++)
            {
                HtmlNodeCollection albumNodes = pages[i].DocumentNode.SelectNodes("//a[@class='albums']");
                for (int y = 0; y < albumNodes.Count; y++)
                {
                    HtmlNode albumNode = albumNodes[y];

                    string href = albumNode.GetAttributeValue("href", "no href");
                    string albumUrl = $"https://kendalljenner.com.br/gallery/{href}";

                    int albumIndex = (size.albumsPerPage * i) + y;
                    downloadAlbumTasks[albumIndex] = Album.Get(albumUrl);
                }
            }

            Album[] albums = await Task.WhenAll(downloadAlbumTasks);
            return albums;
        }

        public static async Task<HtmlDocument[]> GetPages(string url, HtmlDocument? page=null)
        {
            page ??= await Internet.GetStaticPage_HTTPClientAsync(url);

            var size = GetSize(page);
            HtmlDocument[] pages = new HtmlDocument[size.pageCount];
            pages[0] = page;

            for (int i = 1; i < size.pageCount; i++)
            {
                int pageIndex = i + 1;
                string pageUrl = $"{url}&page={pageIndex}";

                pages[i] = await Internet.GetStaticPage_HTTPClientAsync(pageUrl);
            }

            return pages;
        }

        public static (int albumCount, int pageCount, int albumsPerPage) GetSize(HtmlDocument page)
        {
            HtmlNode infoNode = page.DocumentNode.SelectSingleNode("//*[text()[contains(., 'albums on')]]");

            int pageCount = Convert.ToInt32(infoNode.InnerText.Split(" ")[^2]);
            int albumCount = Convert.ToInt32(infoNode.InnerText.Split(" ")[0]);
            int albumsPerPage = albumCount;

            if (pageCount > 1)
            {
                HtmlNodeCollection albumLinkNodes = page.DocumentNode.SelectNodes("//span[@class='alblink']");
                albumsPerPage = albumLinkNodes.Count;
            }

            return (albumCount, pageCount, albumsPerPage);
        }

    }

    public class Album
    {
        public string Name {get; protected set;} = string.Empty;
        public (string link, string name)[] Images {get; protected set;} = [];

        public static async Task<Album> Get(string url)
        {
            HtmlDocument page = await Internet.GetStaticPage_HTTPClientAsync(url);

            return new Album
            {
                Name = GetName(page),
                Images = await GetPhotos(url)
            };
        }

        public static string GetName(HtmlDocument page) => page.DocumentNode.SelectNodes("//span[@class='statlink']")[0].ChildNodes[^1].InnerText;

        public static async Task<(string link, string name)[]> GetPhotos(string url)
        {
            string baseUrl = url[..url.LastIndexOf('/')];

            HtmlDocument[] pages = await GetAlbumPages(url);
            var albumSize = GetAlbumSize(pages[0]);
            Console.WriteLine(albumSize);

            var images = new (string link, string name)[albumSize.imageCount];

            for (int i = 0; i < pages.Length; i++)
            {
                HtmlDocument page = pages[i];

                HtmlNodeCollection thumbnailNodes = page.DocumentNode.SelectNodes("//img[@class='image thumbnail']");
                if (thumbnailNodes.Count == 0)
                    throw new Exception($"Could not find any thumbnail nodes for {url} with selector '//img[@class='image thumbnail']'");

                for (int y = 0; y < thumbnailNodes.Count; y++)
                {
                    HtmlNode thumbnailNode = thumbnailNodes[y];

                    string hrefLink = thumbnailNode.GetAttributeValue("src", "No Source").Replace("thumb_", "");

                    string src = $"{baseUrl}/{hrefLink}";
                    string fileName = thumbnailNode.GetAttributeValue("alt", "No Name");

                    int imageIndex = (albumSize.imagesPerPage * i) + y;
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

        public static (int pageCount, int imageCount, int imagesPerPage) GetAlbumSize(HtmlDocument page)
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
    }
}