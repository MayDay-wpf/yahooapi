using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using yahooapi.Dtos;

namespace yahooapi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class GetYahooResultController : Controller
{
    private readonly IConfiguration _configuration;

    public GetYahooResultController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // GET&POST
    [HttpGet]
    [HttpPost]
    public async Task<List<SearchEngineResult>> YahooSearch(string query, string apikey, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(apikey))
        {
            throw new ArgumentException("apikey cannot be null or empty.", nameof(apikey));
        }

        string systemApiKey = MD5Hash(_configuration["ApiKey"]);
        if (systemApiKey != apikey)
        {
            throw new ArgumentException("apikey is invalid.", nameof(apikey));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults must be greater than 0.");
        }

        // Encode the query
        string encodedQuery = HttpUtility.UrlEncode(query);
        string requestUrl = $"https://sg.search.yahoo.com/search?p={encodedQuery}&ei=UTF-8";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
                client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

                // Make the request
                string response = await client.GetStringAsync(requestUrl);

                MatchCollection urlMatches = Regex.Matches(response,
                    "<a[^>]*class=\"[^\"]*d-ib fz-20 lh-26[^\"]*\"[^>]*href=\"(https?:\\/\\/[^\\s\"]+)\"[^>]*>",
                    RegexOptions.IgnoreCase);

                MatchCollection titleMatches = Regex.Matches(response,
                    "<a[^>]*class=\"[^\"]*d-ib fz-20 lh-26[^\"]*\"[^>]*>(?:<span[^>]*>[^<]*</span>)?(.+?)</a>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                MatchCollection contentMatches = Regex.Matches(response,
                    "<span class=\"\\s*fc-falcon\\s*\"[^>]*>(.*?)</span>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                // Create a list to store the results
                List<SearchEngineResult> results = new List<SearchEngineResult>();

                // Combine the matches into SearchEngineResult objects
                int count = Math.Min(maxResults,
                    Math.Min(titleMatches.Count,
                        Math.Min(contentMatches.Count,
                            urlMatches.Count)));

                for (int i = 0; i < count; i++)
                {
                    // 标题处理 - 提取文本并清理HTML
                    string rawTitle = titleMatches[i].Groups[1].Value.Trim();
                    string title = Regex.Replace(rawTitle, "<.*?>", ""); // 移除可能存在的HTML标签
                    title = HttpUtility.HtmlDecode(title); // 解码HTML实体

                    // 标题清洗 - 移除面包屑部分，只保留实际的标题部分
                    // 面包屑通常是 "domain.com › path › path 实际标题"
                    if (title.Contains(" › "))
                    {
                        // 找到最后一个 "›" 后的内容
                        int lastArrowIndex = title.LastIndexOf("›");
                        if (lastArrowIndex >= 0 && lastArrowIndex < title.Length - 1)
                        {
                            title = title.Substring(lastArrowIndex + 1).Trim();
                        }
                    }

                    string snippet = Regex.Replace(contentMatches[i].Groups[1].Value, "<.*?>", ""); // Remove HTML tags
                    snippet = HttpUtility.HtmlDecode(snippet); // 解码HTML实体

                    // 清理URL，提取真实链接
                    string rawUrl = urlMatches[i].Groups[1].Value;
                    string cleanUrl = rawUrl;

                    // 从Yahoo重定向URL中提取真实URL
                    if (rawUrl.Contains("/RU=") && rawUrl.Contains("/RK="))
                    {
                        int startIndex = rawUrl.IndexOf("/RU=") + 4;
                        int endIndex = rawUrl.IndexOf("/RK=");
                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            cleanUrl = rawUrl.Substring(startIndex, endIndex - startIndex);
                            cleanUrl = HttpUtility.UrlDecode(cleanUrl);
                        }
                    }

                    results.Add(new SearchEngineResult
                    {
                        Title = title,
                        Url = cleanUrl,
                        Snippet = snippet
                    });
                }

                return results;
            }
            catch (HttpRequestException ex)
            {
                return new List<SearchEngineResult>();
            }
            catch (RegexMatchTimeoutException ex)
            {
                return new List<SearchEngineResult>();
            }
            catch (Exception ex)
            {
                return new List<SearchEngineResult>();
            }
        }
    }

    // GET&POST
    [HttpGet]
    [HttpPost]
    public async Task<List<SearchEngineVideoResults>> YahooSearchVideo(string query, string apikey, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(apikey))
        {
            throw new ArgumentException("apikey cannot be null or empty.", nameof(apikey));
        }

        string systemApiKey = MD5Hash(_configuration["ApiKey"]);
        if (systemApiKey != apikey)
        {
            throw new ArgumentException("apikey is invalid.", nameof(apikey));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults must be greater than 0.");
        }

        string encodedQuery = HttpUtility.UrlEncode(query);
        string requestUrl = $"https://sg.search.yahoo.com/search/video?p={encodedQuery}&ei=UTF-8";
        int maxRedirects = 10;

        using (HttpClientHandler handler = new HttpClientHandler() { AllowAutoRedirect = false })
        using (HttpClient client = new HttpClient(handler))
        {
            try
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
                client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                int redirectCount = 0;

                while ((response.StatusCode == HttpStatusCode.MovedPermanently ||
                        response.StatusCode == HttpStatusCode.Found ||
                        response.StatusCode == HttpStatusCode.TemporaryRedirect ||
                        response.StatusCode == HttpStatusCode.PermanentRedirect) &&
                       redirectCount < maxRedirects)
                {
                    string redirectUrl = response.Headers.Location.ToString();

                    if (!Uri.IsWellFormedUriString(redirectUrl, UriKind.Absolute))
                    {
                        redirectUrl = new Uri(new Uri(requestUrl), redirectUrl).ToString();
                    }

                    requestUrl = redirectUrl;
                    response = await client.GetAsync(redirectUrl);
                    redirectCount++;
                }

                if (redirectCount >= maxRedirects)
                {
                    Console.WriteLine("Maximum number of redirects exceeded.");
                    return new List<SearchEngineVideoResults>();
                }

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();


                // --- KEY CHANGE: More Specific Regex ---
                // Match <li> elements with class "vr vres", then find <a> tags within them.
                MatchCollection titleMatches = Regex.Matches(responseBody,
                    @"<li[^>]*class=""vr vres""[^>]*>.*?<a[^>]*aria-label=""([^""]*)""[^>]*>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                MatchCollection urlMatches = Regex.Matches(responseBody,
                    @"<li[^>]*class=""vr vres""[^>]*>.*?<a[^>]*data-rurl=""([^""]*)""[^>]*>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                MatchCollection imageMatches = Regex.Matches(responseBody,
                    @"<li[^>]*class=""vr vres""[^>]*>.*?<img[^>]*src=""([^""]*)""[^>]*class=""thm""[^>]*>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                List<SearchEngineVideoResults> results = new List<SearchEngineVideoResults>();
                int count = Math.Min(maxResults,
                    Math.Min(titleMatches.Count, Math.Min(urlMatches.Count, imageMatches.Count)));

                for (int i = 0; i < count; i++)
                {
                    string title = HttpUtility.HtmlDecode(titleMatches[i].Groups[1].Value);
                    title = Regex.Replace(title, @"</?b>", "", RegexOptions.IgnoreCase);
                    string url = HttpUtility.UrlDecode(urlMatches[i].Groups[1].Value);
                    string image = HttpUtility.UrlDecode(imageMatches[i].Groups[1].Value);

                    results.Add(new SearchEngineVideoResults
                    {
                        Title = title,
                        Url = url,
                        Image = image
                    });
                }

                return results;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error during Yahoo search: {ex.Message}");
                return new List<SearchEngineVideoResults>();
            }
            catch (RegexMatchTimeoutException ex)
            {
                Console.WriteLine($"Regex timeout during Yahoo search: {ex.Message}");
                return new List<SearchEngineVideoResults>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return new List<SearchEngineVideoResults>();
            }
        }
    }

    [HttpGet]
    [HttpPost]
    public async Task<List<SearchEngineImageResults>> YahooSearchImages(string query, string apikey, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(apikey))
        {
            throw new ArgumentException("apikey cannot be null or empty.", nameof(apikey));
        }

        string systemApiKey = MD5Hash(_configuration["ApiKey"]);
        if (systemApiKey != apikey)
        {
            throw new ArgumentException("apikey is invalid.", nameof(apikey));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults must be greater than 0.");
        }

        string encodedQuery = HttpUtility.UrlEncode(query);
        string requestUrl = $"https://sg.search.yahoo.com/search/images?p={encodedQuery}&ei=UTF-8";
        int maxRedirects = 10;

        using (HttpClientHandler handler = new HttpClientHandler() { AllowAutoRedirect = false })
        using (HttpClient client = new HttpClient(handler))
        {
            try
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
                client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                int redirectCount = 0;

                while ((response.StatusCode == HttpStatusCode.MovedPermanently ||
                        response.StatusCode == HttpStatusCode.Found ||
                        response.StatusCode == HttpStatusCode.TemporaryRedirect ||
                        response.StatusCode == HttpStatusCode.PermanentRedirect) &&
                       redirectCount < maxRedirects)
                {
                    string redirectUrl = response.Headers.Location.ToString();

                    if (!Uri.IsWellFormedUriString(redirectUrl, UriKind.Absolute))
                    {
                        redirectUrl = new Uri(new Uri(requestUrl), redirectUrl).ToString();
                    }

                    requestUrl = redirectUrl;
                    response = await client.GetAsync(redirectUrl);
                    redirectCount++;
                }

                if (redirectCount >= maxRedirects)
                {
                    Console.WriteLine("Maximum number of redirects exceeded.");
                    return new List<SearchEngineImageResults>();
                }

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // 更新的正则表达式，匹配图片元素中的data-src属性
                MatchCollection imageMatches = Regex.Matches(responseBody,
                    @"<img\s+(?:.*?\s+)?data-src=['""]([^'""]+)['""](?:.*?\s+)?class=['""]process['""]",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                // 如果以上匹配为空，尝试备选正则表达式
                if (imageMatches.Count == 0)
                {
                    imageMatches = Regex.Matches(responseBody,
                        @"<img\s+(?:.*?\s+)?class=['""]process['""](?:.*?\s+)?data-src=['""]([^'""]+)['""]",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }

                List<SearchEngineImageResults> results = new List<SearchEngineImageResults>();
                int count = Math.Min(maxResults, imageMatches.Count);

                for (int i = 0; i < count; i++)
                {
                    string imageUrl = HttpUtility.UrlDecode(imageMatches[i].Groups[1].Value);

                    if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                    {
                        imageUrl = new Uri(new Uri(requestUrl), imageUrl).ToString();
                    }

                    results.Add(new SearchEngineImageResults
                    {
                        Url = imageUrl
                    });
                }

                return results;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error during Yahoo search: {ex.Message}");
                return new List<SearchEngineImageResults>();
            }
            catch (RegexMatchTimeoutException ex)
            {
                Console.WriteLine($"Regex timeout during Yahoo search: {ex.Message}");
                return new List<SearchEngineImageResults>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return new List<SearchEngineImageResults>();
            }
        }
    }

    private static string MD5Hash(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }
}