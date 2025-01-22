namespace Danaus.Network;

public class HttpMethod(string name)
{
    public string Name { get; } = name;

    public static HttpMethod Get
    {
        get { return new HttpMethod("GET"); }
    }
}