using System.Diagnostics;

namespace Danaus.Url;

// https://url.spec.whatwg.org/#concept-url
public class URL(
    string scheme = "",
    string username = "",
    string password = "",
    string? host = null,
    ushort? port = null,
    List<string>? path = null,
    string? query = null,
    string? fragment = null)
{
    public string Scheme { get; set; } = scheme;
    public string Username { get; set; } = username;
    public string Password { get; set; } = password;
    public string? Host { get; set; } = host;
    public ushort? Port { get; set; } = port;
    public IList<string> Path { get; set; } = path ?? [];
    public string? Query { get; set; } = query;
    public string? Fragment { get; set; } = fragment;

    public SpecialScheme? SpecialScheme
    {
        get
        {
          return SpecialScheme.GetFromScheme(Scheme);
        }
    }

    public bool IsSpecial()
    {
        return SpecialScheme.IsSpecialScheme(Scheme);
    }

    public bool HasOpaquePath()
    {
        return Path.Count == 1 && Path[0] == "";
    }

    public bool HasPort()
    {
        return Port is not null;
    }

    public bool HasEmptyHost()
    {
        return Host == string.Empty;
    }

    public bool HasEmptyPath()
    {
        return Path.Count == 0;
    }

    public bool HasBlobScheme()
    {
        return Scheme == "blob";
    }

    public bool HasFtpScheme()
    {
        return Scheme == SpecialScheme.Ftp.Scheme;
    }

    public bool HasFileScheme()
    {
        return Scheme == SpecialScheme.File.Scheme;
    }

    public bool HasHttpScheme()
    {
        return Scheme == SpecialScheme.Http.Scheme;
    }

    public bool HasHttpsScheme()
    {
        return Scheme == SpecialScheme.Https.Scheme;
    }

    public bool HasWsScheme()
    {
        return Scheme == SpecialScheme.Ws.Scheme;
    }

    public bool HasWssScheme()
    {
        return Scheme == SpecialScheme.Wss.Scheme;
    }

    public bool IncludesCredentials()
    {
        return Username != string.Empty || Password != string.Empty;
    }
}