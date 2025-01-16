using System.Text;
using Danaus.Url;

namespace Danaus.Network;

public class HttpRequest(URL url, HttpMethod method, Dictionary<HttpRequestHeader, string> headers)
{
    public const string USER_AGENT = "Danaus/0.0.0"; 

    public URL Url { get; set; } = url;
    public HttpMethod Method { get; set; } = method;
    public Dictionary<HttpRequestHeader, string> Headers { get; set; } = headers;

    public byte[] GetBytes()
    {
        return System.Text.Encoding.UTF8.GetBytes(ToString());
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.Append($"{Method.Name} {(Url.Path.Count != 0 ? string.Join('/', Url.Path) : "/")} HTTP/1.0\r\n");
        foreach (KeyValuePair<HttpRequestHeader, string> kvp in Headers)
        {
            builder.Append($"{kvp.Key.Name}: {kvp.Value}\r\n");
        }
        builder.Append("\r\n");

        return builder.ToString();
    }

    public static HttpRequest FromURLString(string url, HttpMethod? method = null, Dictionary<HttpRequestHeader, string>? headers = null)
    {
        return FromURL(URLParser.Parse(url).Url, method, headers);
    }

    public static HttpRequest FromURL(URL url, HttpMethod? method = null, Dictionary<HttpRequestHeader, string>? headers = null)
    {
        if (url.Host is null)
        {
            throw new ArgumentException("Url must have a non-null host.");
        }

        var userAgentHeaders = new Dictionary<HttpRequestHeader, string>()
        {
            { HttpRequestHeader.Host, url.Host },
            { HttpRequestHeader.UserAgent, USER_AGENT },
        };

        var combinedHeaders = headers is not null
            ? headers.Concat(userAgentHeaders)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            : userAgentHeaders;

        return new HttpRequest(url, method ?? HttpMethod.Get, combinedHeaders);
    }
}