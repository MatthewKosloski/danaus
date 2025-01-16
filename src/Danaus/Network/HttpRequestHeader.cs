namespace Danaus.Network;

public class HttpRequestHeader(string name) : HttpHeader(name)
{
    public static HttpRequestHeader Host
    {
        get { return new HttpRequestHeader("Host"); }
    }

    public static HttpRequestHeader UserAgent
    {
        get { return new HttpRequestHeader("UserAgent"); }
    }
}