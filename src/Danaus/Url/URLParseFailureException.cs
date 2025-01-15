namespace Danaus.Url;

public class URLParseFailureException : Exception
{
    public URLParseFailureException() 
    { 
    }

    public URLParseFailureException(string message) 
        : base(message) 
    { 
    }

    public URLParseFailureException(string message, Exception innerException) 
        : base(message, innerException) 
    { 
    }
}
