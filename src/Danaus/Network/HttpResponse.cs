using System.Text;
using Danaus.Url;

namespace Danaus.Network;

public class HttpResponse(
    URL url,
    HttpMethod method,
    Dictionary<HttpResponseHeader, string> headers,
    string httpVersion,
    string httpStatusText,
    ushort httpStatusCode,
    StreamReader content)
{
    public URL Url { get; set; } = url;
    public HttpMethod Method { get; set; } = method;
    public Dictionary<HttpResponseHeader, string> Headers { get; set; } = headers;
    public string HttpVersion { get; set; } = httpVersion;
    public string HttpStatusText { get; set; } = httpStatusText;
    public ushort HttpStatusCode { get; set; } = httpStatusCode;
    public StreamReader Content { get; set; } = content;

    public byte[] GetBytes()
    {
        return System.Text.Encoding.UTF8.GetBytes(ToString());
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.Append($"{Method.Name} {(Url.Path.Count != 0 ? string.Join('/', Url.Path) : "/")} HTTP/1.0\r\n");
        foreach (KeyValuePair<HttpResponseHeader, string> kvp in Headers)
        {
            builder.Append($"{kvp.Key.Name} {kvp.Value}\r\n");
        }
        builder.Append("\r\n");

        return builder.ToString();
    }
}