namespace Danaus.Url;

// https://url.spec.whatwg.org/#special-scheme
public class SpecialScheme
{

    public string Scheme { get; }

    public ushort? Port { get; }

    public static SpecialScheme Ftp
    {
        get { return new SpecialScheme("ftp", 21); }
    }

    public static SpecialScheme File
    {
        get { return new SpecialScheme("file", null); }
    }

    public static SpecialScheme Http
    {
        get { return new SpecialScheme("http", 80); }
    }

    public static SpecialScheme Https
    {
        get { return new SpecialScheme("https", 443); }
    }

    public static SpecialScheme Ws
    {
        get { return new SpecialScheme("ws", 80); }
    }

    public static SpecialScheme Wss
    {
        get { return new SpecialScheme("ws", 443); }
    }

    private SpecialScheme(string scheme, ushort? port)
    {
        Scheme = scheme;
        Port = port;
    }

    public static List<SpecialScheme> SpecialSchemes()
    {
        return [Ftp, File, Http, Https, Ws, Wss];
    }

    public static bool IsSpecialScheme(string scheme)
    {
        return SpecialSchemes().Any(specialScheme => specialScheme.Scheme == scheme);
    }

    public static SpecialScheme? GetFromScheme(string scheme)
    {
        return SpecialSchemes().FirstOrDefault(specialScheme => specialScheme.Scheme == scheme);
    }

    public override string ToString()
    {
        return Scheme;
    }
}