using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using HtmlAgilityPack;

public static class Instagram
{
    public static bool IsInstagramLink(string url) => url.StartsWith("https://www.instagram.com/");

    public static async Task DownloadAccountAsCalender(string accountlink, DirectoryInfo parentFolder)
    {
        //Console.WriteLine("(Instagram Login) Enter username:");
        //string ussername = Console.ReadLine();
        //Console.WriteLine("(Instagram Login) Enter password");
        //string password = Console.ReadLine();
        string username = "therealnina2010";
        string password = "^%@w4Xdu'X^B8y4";


        HtmlDocument page = await InstagramClient.DownloadPageAsync(username, password, accountlink);

        HtmlNode nameNode = page.DocumentNode.SelectSingleNode("/html/body/div[1]/div/div/div[2]/div/div/div[1]/div[2]/div/div[1]/section/main/div/header/section[4]/div/div[1]/span");
        File.WriteAllText("asd.html", page.ParsedText);

    }
}


public static class InstagramClient
{
    // Shared handler+container for caching cookies across calls
    private static readonly CookieContainer _cookieContainer = new CookieContainer();
    private static readonly HttpClientHandler _handler = new HttpClientHandler
    {
        CookieContainer = _cookieContainer,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };
    private static readonly HttpClient _httpClient = new HttpClient(_handler)
    {
        // you can tweak timeouts here if you like
    };

    static InstagramClient()
    {
        // set once at startup
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/99.0.4844.82 Safari/537.36"
        );
    }

    /// <summary>
    /// Ensures we have an authenticated session. If we've already got a sessionid cookie, it skips login.
    /// </summary>
    private static async Task EnsureLoggedInAsync(string username, string password)
    {
        var domain = new Uri("https://www.instagram.com/");
        var cookies = _cookieContainer.GetCookies(domain).Cast<Cookie>();

        // if we already have a sessionid cookie, assume it's still valid
        if (cookies.Any(c => c.Name == "sessionid" && !string.IsNullOrEmpty(c.Value)))
            return;

        // otherwise, run the login flow
        // 1) GET homepage to grab csrftoken
        var homeResp = await _httpClient.GetAsync(domain);
        homeResp.EnsureSuccessStatusCode();

        // 2) extract csrftoken
        var csrf = _cookieContainer
            .GetCookies(domain)
            .Cast<Cookie>()
            .FirstOrDefault(c => c.Name == "csrftoken")
            ?.Value
            ?? throw new InvalidOperationException("Could not find csrftoken.");

        // 3) build and send login POST
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var encPassword = $"#PWD_INSTAGRAM_BROWSER:0:{ts}:{password}";

        var form = new Dictionary<string, string>
        {
            ["username"]    = username,
            ["enc_password"]= encPassword
        };

        using var loginReq = new HttpRequestMessage(HttpMethod.Post, "https://www.instagram.com/accounts/login/ajax/")
        {
            Content = new FormUrlEncodedContent(form)
        };
        loginReq.Headers.Add("X-CSRFToken", csrf);
        loginReq.Headers.Referrer = new Uri("https://www.instagram.com/accounts/login/");

        var loginResp = await _httpClient.SendAsync(loginReq);
        loginResp.EnsureSuccessStatusCode();
        // after this, _cookieContainer holds both csrftoken & sessionid
    }

    /// <summary>
    /// Downloads the HTML of any Instagram page, logging in only if needed.
    /// </summary>
    public static async Task<HtmlDocument> DownloadPageAsync(string username, string password, string pageUrl)
    {
        await EnsureLoggedInAsync(username, password);
        var resp = await _httpClient.GetAsync(pageUrl);
        resp.EnsureSuccessStatusCode();
        string source = await resp.Content.ReadAsStringAsync();

        HtmlDocument page = new HtmlDocument();
        page.LoadHtml(source);

        return page;
    }
}