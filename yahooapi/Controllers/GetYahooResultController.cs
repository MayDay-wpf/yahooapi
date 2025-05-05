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
    public async Task<List<SearchEngineResult>> YahooSearch(string query, int page = 1, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        // Encode the query
        string encodedQuery = HttpUtility.UrlEncode(query);

        // 计算b参数，分页公式
        int b = 1 + (page - 1) * 7;

        // 构建请求URL
        string requestUrl = string.Format(_configuration["YahooEndpoint:WebSearch"], encodedQuery, b);

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

                // 获取网页内容
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

                // 创建结果列表
                List<SearchEngineResult> results = new List<SearchEngineResult>();

                // 取较少的匹配数量，避免越界
                int count = Math.Min(maxResults,
                    Math.Min(titleMatches.Count,
                        Math.Min(contentMatches.Count,
                            urlMatches.Count)));

                for (int i = 0; i < count; i++)
                {
                    // 提取标题
                    string rawTitle = titleMatches[i].Groups[1].Value.Trim();
                    string title = Regex.Replace(rawTitle, "<.*?>", "");
                    title = HttpUtility.HtmlDecode(title);

                    // 处理面包屑，提取纯标题
                    if (title.Contains(" › "))
                    {
                        int lastArrowIndex = title.LastIndexOf("›");
                        if (lastArrowIndex >= 0 && lastArrowIndex < title.Length - 1)
                        {
                            title = title.Substring(lastArrowIndex + 1).Trim();
                        }
                    }

                    // 提取摘要内容
                    string snippet = Regex.Replace(contentMatches[i].Groups[1].Value, "<.*?>", "");
                    snippet = HttpUtility.HtmlDecode(snippet);

                    // 处理URL
                    string rawUrl = urlMatches[i].Groups[1].Value;
                    string cleanUrl = rawUrl;

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
            catch (HttpRequestException)
            {
                return new List<SearchEngineResult>();
            }
            catch (RegexMatchTimeoutException)
            {
                return new List<SearchEngineResult>();
            }
            catch (Exception)
            {
                return new List<SearchEngineResult>();
            }
        }
    }

    // GET&POST
    [HttpGet]
    [HttpPost]
    public async Task<List<SearchEngineVideoResults>> YahooSearchVideo(string query, int page = 1, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        string encodedQuery = HttpUtility.UrlEncode(query);
        int b = 1 + (page - 1) * 60; // 计算分页参数
        string requestUrl = string.Format(_configuration["YahooEndpoint:VideoSearch"], encodedQuery, b);

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

                // 处理可能的重定向
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

                // 正则匹配查询结果
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
                Console.WriteLine($"Error during Yahoo video search: {ex.Message}");
                return new List<SearchEngineVideoResults>();
            }
            catch (RegexMatchTimeoutException ex)
            {
                Console.WriteLine($"Regex timeout during Yahoo video search: {ex.Message}");
                return new List<SearchEngineVideoResults>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return new List<SearchEngineVideoResults>();
            }
        }
    }

    // GET&POST
    [HttpGet]
    [HttpPost]
    public async Task<List<SearchEngineImageResults>> YahooSearchImages(string query, int page = 1, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        string encodedQuery = HttpUtility.UrlEncode(query);
        int b = 1 + (page - 1) * 60; // 计算分页参数
        string requestUrl = string.Format(_configuration["YahooEndpoint:ImageSearch"], encodedQuery, b);
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
}