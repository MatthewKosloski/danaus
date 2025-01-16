using System.Text;
using Danaus.Url;

namespace Danaus.Network;

public class HttpRequest(URL url, HttpMethod method, Dictionary<HttpRequestHeader, string> headers)
{
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
}