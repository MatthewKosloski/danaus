namespace Danaus.Network;

public class HttpResponseHeader(string name) : HttpHeader(name)
{
    public static HttpResponseHeader ContentType
    {
        get { return new HttpResponseHeader("Content-Type"); }
    }

    public static HttpResponseHeader ETag
    {
        get { return new HttpResponseHeader("ETag"); }
    }

    public static HttpResponseHeader LastModified
    {
        get { return new HttpResponseHeader("Last-Modified"); }
    }

    public static HttpResponseHeader CacheControl
    {
        get { return new HttpResponseHeader("Cache-Control"); }
    }

    public static HttpResponseHeader Date
    {
        get { return new HttpResponseHeader("Date"); }
    }

    public static HttpResponseHeader ContentLength
    {
        get { return new HttpResponseHeader("Content-Length"); }
    }

    public static HttpResponseHeader Connection
    {
        get { return new HttpResponseHeader("Connection"); }
    }

    private static List<HttpResponseHeader> Headers()
    {
        return [
            CacheControl,
            Connection,
            ContentLength,
            ContentType,
            Date,
            ETag,
            LastModified,
        ];
    }

    public static HttpResponseHeader? GetByName(string name)
    {
        return Headers().FirstOrDefault(header => header.Name == name);
    }

    public override bool Equals(Object? other)
    {
        if (other == null || !(other is HttpResponseHeader))
        {
            return false;
        }
        else
        {
            var otherHttpResponseHeader = (HttpResponseHeader)other;
            return otherHttpResponseHeader.Name == this.Name;
        }
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}