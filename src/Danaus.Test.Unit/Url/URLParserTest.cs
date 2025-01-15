using Danaus.Url;

namespace Danaus.Test.Unit.Url;

public class URLParserTest
{

    public static IEnumerable<object[]> AllPartsData()
    {
        /*
         *                          URL                                                Scheme           Username        Password         Host              Port          Path                                Query            Fragment       ValidationErrors
         */
        yield return new object[] { "http://user:pass@foo:21/bar;par?b=c&d=e#f",       "http",          "user",         "pass",          "foo",            (ushort)21,   new List<string>() { "bar;par" },   "b=c&d=e",       "f",           new List<ValidationError>() { ValidationError.InvalidCredentials } };
        yield return new object[] { "http://user:pass@foo:21/bar;par?b=c&d=e",         "http",          "user",         "pass",          "foo",            (ushort)21,   new List<string>() { "bar;par" },   "b=c&d=e",       null,          new List<ValidationError>() { ValidationError.InvalidCredentials } };
        yield return new object[] { "http://user:pass@foo:21/bar;par?",                "http",          "user",         "pass",          "foo",            (ushort)21,   new List<string>() { "bar;par" },   string.Empty,    null,          new List<ValidationError>() { ValidationError.InvalidCredentials } };
        yield return new object[] { "http://user:pass@foo:21/bar;par",                 "http",          "user",         "pass",          "foo",            (ushort)21,   new List<string>() { "bar;par" },   null,            null,          new List<ValidationError>() { ValidationError.InvalidCredentials } };
        yield return new object[] { "http://user:pass@foo:21/",                        "http",          "user",         "pass",          "foo",            (ushort)21,   new List<string>(),                 null,            null,          new List<ValidationError>() { ValidationError.InvalidCredentials } };
        yield return new object[] { "http://user:pass@foo:21",                         "http",          "user",         "pass",          "foo",            (ushort)21,   new List<string>(),                 null,            null,          new List<ValidationError>() { ValidationError.InvalidCredentials } };
        yield return new object[] { "http://user:pass@foo:",                           "http",          "user",         "pass",          "foo",            null,         new List<string>(),                 null,            null,          new List<ValidationError>() { ValidationError.InvalidCredentials } };
        yield return new object[] { "http://user:pass@foo",                            "http",          "user",         "pass",          "foo",            null,         new List<string>(),                 null,            null,          new List<ValidationError>() { ValidationError.InvalidCredentials } };
        yield return new object[] { "http://host",                                     "http",          string.Empty,   string.Empty,    "host",           null,         new List<string>(),                 null,            null,          new List<ValidationError>() };
        yield return new object[] { "http://user@",                                    "http",          "user",         string.Empty,    null,             null,         new List<string>(),                 null,            null,          new List<ValidationError>() { ValidationError.InvalidCredentials, ValidationError.HostMissing } };
        yield return new object[] { "http:",                                           "http",          string.Empty,   string.Empty,    null,             null,         new List<string>(),                 null,            null,          new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus, ValidationError.HostMissing } };
        yield return new object[] { "http",                                            string.Empty,    string.Empty,   string.Empty,    null,             null,         new List<string>(),                 null,            null,          new List<ValidationError>() { ValidationError.MissingSchemeNonRelativeURL } };
        yield return new object[] { string.Empty,                                      string.Empty,    string.Empty,   string.Empty,    null,             null,         new List<string>(),                 null,            null,          new List<ValidationError>() { ValidationError.MissingSchemeNonRelativeURL } };
    }

    [Theory]
    [MemberData(nameof(AllPartsData))]
    public void Parse_AllParts_ShouldParse(string url, string scheme, string username, string password, string? host, ushort? port,
        List<string> path, string? query, string? fragment, List<ValidationError> validationErrors)
    {
        var expected = new ParseResult(new URL()
        {
            Scheme = scheme,
            Username = username,
            Password = password,
            Host = host,
            Port = port,
            Path = path,
            Query = query,
            Fragment = fragment
        }, validationErrors);
        var actual = URLParser.Parse(url);

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> FileData()
    {
        /*
         *                          URL                          Scheme          Host                  Path                                          ValidationErrors
         */
        // No slashes.
        yield return new object[] { "file:",                     "file",         string.Empty,         new List<string>(),                           new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus  } };
        yield return new object[] { "file:path",                 "file",         string.Empty,         new List<string>() { "path" },                new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus  } };
        yield return new object[] { "file:path/",                "file",         string.Empty,         new List<string>() { "path" },                new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus  } };
        yield return new object[] { "file:path/f.txt",           "file",         string.Empty,         new List<string>() { "path", "f.txt" },       new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus  } };
        // One slash.
        yield return new object[] { "file:/",                    "file",         string.Empty,         new List<string>(),                           new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus  } };
        yield return new object[] { "file:/path",                "file",         string.Empty,         new List<string>() { "path" },                new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus  } };
        yield return new object[] { "file:/path/",               "file",         string.Empty,         new List<string>() { "path" },                new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus  } };
        yield return new object[] { "file:/path/f.txt",          "file",         string.Empty,         new List<string>() { "path", "f.txt" },       new List<ValidationError>() { ValidationError.SpecialSchemeMissingFollowingSolidus  } };
        // Two slashes.
        yield return new object[] { "file://",                   "file",         string.Empty,         new List<string>(),                           new List<ValidationError>() };
        yield return new object[] { "file://server",             "file",         "server",             new List<string>(),                           new List<ValidationError>() };
        yield return new object[] { "file://server/",            "file",         "server",             new List<string>(),                           new List<ValidationError>() };
        yield return new object[] { "file://server/f.txt",       "file",         "server",             new List<string>() { "f.txt" },               new List<ValidationError>() };
        // Three slashes.
        yield return new object[] { "file:///",                  "file",         string.Empty,         new List<string>(),                           new List<ValidationError>() };
        yield return new object[] { "file:///path",              "file",         string.Empty,         new List<string>() { "path" },                new List<ValidationError>() };
        yield return new object[] { "file:///path/",             "file",         string.Empty,         new List<string>() { "path" },                new List<ValidationError>() };
        yield return new object[] { "file:///path/f.txt",        "file",         string.Empty,         new List<string>() { "path", "f.txt" },       new List<ValidationError>() };
    }

    [Theory]
    [MemberData(nameof(FileData))]
    public void Parse_FileScheme_ShouldParse(string url, string scheme, string? host, List<string> path, List<ValidationError> validationErrors)
    {
        var expected = new ParseResult(new URL()
        {
            Scheme = scheme,
            Username = string.Empty,
            Password = string.Empty,
            Host = host,
            Port = null,
            Path = path,
            Query = null,
            Fragment = null
        }, validationErrors);
        var actual = URLParser.Parse(url);

        Assert.Equal(expected, actual);
    }
}