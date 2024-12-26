using System.Diagnostics;

namespace Danaus.Url;

public enum Encoding
{
    UTF8
}

public enum State
{
    Authority,
    FileHost,
    FileSlash,
    File,
    Fragment,
    Hostname,
    Host,
    NoScheme,
    OpaquePath,
    PathOrAuthority,
    PathStart,
    Path,
    Port,
    Query,
    RelativeSlash,
    Relative,
    SchemeStart,
    Scheme,
    SpecialAuthorityIgnoreSlashes,
    SpecialAuthoritySlashes,
    SpecialRelativeOrAuthority,
}

public enum CodePoint
{
    ApplicationProgramCommand = 0x009F,
    CarriageReturn = 0x000D,
    Delete = 0x007F,
    LineFeed = 0x000A,
    LowercaseA = 0x0061,
    LowercaseZ = 0x007A,
    Nine = 0x0039,
    NullCharacter = 0x0000,
    Space = 0x0020,
    Tab = 0x0009,
    UnitSeparator = 0x001F,
    UppercaseA = 0x0041,
    UppercaseZ = 0x005A,
    Zero = 0x0030,
    Plus = 0x002B,
    HyphenMinus = 0x002D,
    Period = 0x002E,
    Colon = 0x003A,
    Number = 0x0023,
    Solidus = 0x002F,
    ReverseSolidus = 0x005C,
    QuestionMark = 0x003F,
    VerticalLine = 0x007C,
}

public class BasicParseFailure(string message)
{
    public string Message { get; } = message;
}

public class BasicParseResult(URL? url, BasicParseFailure? failure)
{
    public URL? Url { get; } = url;
    public BasicParseFailure? Failure { get; } = failure;

    public bool IsFailure()
    {
        return Failure != null;
    }
}

public class URLParser
{

    // https://url.spec.whatwg.org/#url-parsing
    public static URL Parse(string input, URL? baseUrl = null, Encoding encoding = Encoding.UTF8)
    {
        // 1. Let url be the result of running the basic URL parser on input with base and encoding.
        var url = BasicParse(input, baseUrl, encoding);

        // 2. If url is failure, return failure.
        // TODO

        // 3. If url's scheme is not "blob", return url.
        // TODO

        // 4. Set url's blob URL entry to the result of resolving the blob URL url, if that did not return failure, and null otherwise.
        // TODO

        // 5. Return url.
        // TODO
        return new URL();
    }

    // https://url.spec.whatwg.org/#concept-basic-url-parser
    private static URL? BasicParse(string input, URL? baseUrl = null, Encoding encoding = Encoding.UTF8, URL? url = null, State? stateOverride = null)
    {

        var hasStateOverride = stateOverride is not null;
        var processedInput = input;
        var hasValidationError = false;


        // 1. If url is not given:
        if (url == null)
        {
            // 1. Set url to a new URL.
            url = new URL();
            
            // 2. If input contains any leading or trailing C0 control or space, invalid-URL-unit validation error.
            var startIndex = 0;
            var endIndex = input.Length - 1;

            for (; startIndex < input.Length; startIndex++)
            {
                if (IsC0ControlOrSpace(input[startIndex]))
                {
                    hasValidationError = true;
                    continue;
                }

                break;
            }

            for (; endIndex > startIndex; endIndex--)
            {
                if (IsC0ControlOrSpace(input[endIndex]))
                {
                    hasValidationError = true;
                    continue;
                }

                break;
            }

            // 3. Remove any leading and trailing C0 control or space from input.
            processedInput = input.Substring(startIndex, endIndex);
        }

        // 2. If input contains any ASCII tab or newline, invalid-URL-unit validation error.
        // 3. Remove all ASCII tab or newline from input.
        foreach (char ch in processedInput)
        {
            if (IsASCIITabOrNewLine(ch))
            {
                hasValidationError = true;
                processedInput = processedInput.Replace((char)CodePoint.Tab, (char)CodePoint.NullCharacter)
                    .Replace((char)CodePoint.LineFeed, (char)CodePoint.NullCharacter)
                    .Replace((char)CodePoint.CarriageReturn, (char)CodePoint.NullCharacter);
                break;
            }
        }

        // 4. Let state be state override if given, or scheme start state otherwise.
        var state = stateOverride ?? State.SchemeStart;

        // 5. Set encoding to the result of getting an output encoding from encoding.
        // TODO

        // 6. Let buffer be the empty string.
        var buffer = string.Empty;

        // 7. Let atSignSeen, insideBrackets, and passwordTokenSeen be false.
        bool atSignSeen = false, insideBrackets = false, passwordTokenSeen = false;

        // 8. Let pointer be a pointer for input.
        var pointer = 0;
        var remaining = input[(pointer + 1)..];

        // 9. Keep running the following state machine by switching on state. 
        //    If, after a run, pointer points to the EOF code point, go to the next step.
        //    Otherwise, increase pointer by 1 and continue with the state machine.
        while (pointer < input.Length)
        {
            char c = input[pointer];

            switch (state)
            {
                case State.SchemeStart:
                    // 1. If c is an ASCII alpha, append c, lowercased, to buffer, and set state to scheme state.
                    if (IsASCIIAlpha(c))
                    {
                        buffer += char.ToLower(c);
                        state = State.Scheme;
                    }
                    // 2. Otherwise, if state override is not given, set state to no scheme state and decrease pointer by 1.
                    else if (!hasStateOverride)
                    {
                        state = State.NoScheme;
                        pointer--;
                    }
                    // 3. Otherwise, return failure.
                    else
                    {
                        // TODO
                    }
                    break;
                case State.Scheme:
                    // 1. If c is an ASCII alphanumeric, U+002B (+), U+002D (-), or U+002E (.), append c, lowercased, to buffer.
                    if (IsASCIIAlphaNumeric(c) || IsOneOf(c, CodePoint.Plus, CodePoint.HyphenMinus, CodePoint.Period))
                    {
                        buffer += char.ToLower(c);
                    }
                    // 2. Otherwise, if c is U+003A (:), then:
                    else if (c == (int)CodePoint.Colon)
                    {
                        // 1. If state override is given, then:
                        if (hasStateOverride)
                        {
                            // 1. If url’s scheme is a special scheme and buffer is not a special scheme, then return.
                            // 2. If url’s scheme is not a special scheme and buffer is a special scheme, then return.
                            if (url.IsSpecial() && !SpecialScheme.IsSpecialScheme(buffer)
                                || !url.IsSpecial() && SpecialScheme.IsSpecialScheme(buffer))
                            {
                                return url;
                            }

                            // 3. If url includes credentials or has a non-null port, and buffer is "file", then return.
                            if ((url.IncludesCredentials() || url.HasPort()) && buffer == "file")
                            {
                                return url;
                            }

                            // 4. If url’s scheme is "file" and its host is an empty host, then return.
                            if (url.HasFileScheme() && url.HasEmptyHost())
                            {
                                return url;
                            }
                        }

                        // 2. Set url’s scheme to buffer.
                        url.Scheme = buffer;

                        // 3. If state override is given, then:
                        if (hasStateOverride)
                        {
                            // 1. If url’s port is url’s scheme’s default port, then set url’s port to null.
                            if (url.Port == url.SpecialScheme?.Port)
                            {
                                url.Port = null;
                            }

                            // 2. Return.
                            return url;
                        }

                        // 4. Set buffer to the empty string.
                        buffer = string.Empty;

                        // 5. If url’s scheme is "file", then:
                        if (url.HasFileScheme())
                        {
                            // 1. If remaining does not start with "//", special-scheme-missing-following-solidus validation error.
                            if(!remaining.StartsWith("//"))
                            {
                                hasValidationError = true;
                            }
                            state = State.File;
                        }
                        // 6. Otherwise, if url is special, base is non-null, and base’s scheme is url’s scheme:
                        else if (url.IsSpecial() && baseUrl?.Scheme == url.Scheme)
                        {
                            // 1. Assert: base is special (and therefore does not have an opaque path).
                            Debug.Assert(baseUrl.IsSpecial());

                            // 2. Set state to special relative or authority state.
                            state = State.SpecialRelativeOrAuthority;
                        }
                        // 7. Otherwise, if url is special, set state to special authority slashes state.
                        else if (url.IsSpecial())
                        {
                            state = State.SpecialAuthoritySlashes;
                        }
                        // 8. Otherwise, if remaining starts with an U+002F (/),
                        //    set state to path or authority state and increase pointer by 1.
                        else if (remaining.StartsWith("/"))
                        {
                            state = State.PathOrAuthority;
                            pointer++;
                        }
                        // 9. Otherwise, set url’s path to the empty string and set state to opaque path state.
                        else
                        {
                            url.Path = [""];
                            state = State.OpaquePath;
                        }
                    }
                    // 3. Otherwise, if state override is not given,
                    //    set buffer to the empty string, state to no scheme state,
                    //    and start over (from the first code point in input).
                    else if (!hasStateOverride)
                    {
                        buffer = string.Empty;
                        state = State.NoScheme;
                        pointer = -1;
                    }
                    // 4. Otherwise, return failure.
                    else
                    {
                        // TODO
                    }

                    break;
                case State.NoScheme:
                {
                    // 1. If base is null, or base has an opaque path and c is not U+0023 (#),
                    //    missing-scheme-non-relative-URL validation error, return failure.
                    if (baseUrl is null || baseUrl is not null && baseUrl.HasOpaquePath() && !IsOneOf(c, CodePoint.Number))
                    {
                        hasValidationError = true;
                    } 
                    // 2. Otherwise, if base has an opaque path and c is U+0023 (#), set url’s scheme to base’s scheme,
                    //    url’s path to base’s path, url’s query to base’s query, url’s fragment to the empty string,
                    //    and set state to fragment state.
                    else if (baseUrl is not null && baseUrl.HasOpaquePath() && IsOneOf(c, CodePoint.Number))
                    {
                        url.Scheme = baseUrl.Scheme;
                        url.Path = baseUrl.Path;
                        url.Query = baseUrl.Query;
                        url.Fragment = string.Empty;
                        state = State.Fragment;
                    }
                    // 3. Otherwise, if base’s scheme is not "file", set state to relative state and decrease pointer by 1.
                    else if (baseUrl is not null && !baseUrl.HasFileScheme())
                    {
                        state = State.Relative;
                        pointer--;
                    }
                    // 4. Otherwise, set state to file state and decrease pointer by 1.
                    else
                    {
                        state = State.File;
                        pointer--;
                    }

                    break;
                }
                case State.SpecialRelativeOrAuthority:
                {
                    // 1. If c is U+002F (/) and remaining starts with U+002F (/), then
                    //    set state to special authority ignore slashes state and increase pointer by 1.
                    if (IsOneOf(c, CodePoint.Solidus) && remaining.StartsWith("/"))
                    {
                        state = State.SpecialAuthorityIgnoreSlashes;
                        pointer++;
                    }
                    // 2. Otherwise, special-scheme-missing-following-solidus validation error,
                    //    set state to relative state and decrease pointer by 1.
                    else
                    {
                        hasValidationError = true;
                        state = State.Relative;
                        pointer--;
                    }

                    break;
                }
                case State.PathOrAuthority:
                {
                    // 1. If c is U+002F (/), then set state to authority state.
                    if (IsOneOf(c, CodePoint.Solidus))
                    {
                        state = State.Authority;
                    }
                    // 2. Otherwise, set state to path state, and decrease pointer by 1.
                    else
                    {
                        state = State.Path;
                        pointer--;
                    }
                    
                    break;
                }
                case State.Relative:
                {
                    // 1. Assert: base’s scheme is not "file".
                    Debug.Assert(baseUrl is not null && baseUrl.Scheme != "file");

                    // 2. Set url’s scheme to base’s scheme.
                    url.Scheme = baseUrl.Scheme;

                    // 3. If c is U+002F (/), then set state to relative slash state.
                    if (IsOneOf(c, CodePoint.Solidus))
                    {
                        state = State.RelativeSlash;
                    }
                    // 4. Otherwise, if url is special and c is U+005C (\),
                    //    invalid-reverse-solidus validation error, set state to relative slash state.
                    else if (url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus))
                    {
                        hasValidationError = true;
                        state = State.RelativeSlash;
                    }
                    // 5. Otherwise:
                    else
                    {
                        // 1. Set url’s username to base’s username, url’s password to base’s password,
                        //    url’s host to base’s host, url’s port to base’s port,
                        //    url’s path to a clone of base’s path, and url’s query to base’s query.
                        url.Username = baseUrl.Username;
                        url.Password = baseUrl.Password;
                        url.Host = baseUrl.Host;
                        url.Port = baseUrl.Port;
                        url.Path = [.. baseUrl.Path];
                        url.Query = baseUrl.Query;

                        // 2. If c is U+003F (?), then set url’s query to the empty string, and state to query state.
                        if (IsOneOf(c, CodePoint.QuestionMark))
                        {
                            url.Query = string.Empty;
                            state = State.Query;
                        }
                        // 3. Otherwise, if c is U+0023 (#), set url’s fragment to the empty string and state to fragment state.
                        else if (IsOneOf(c, CodePoint.Number))
                        {
                            url.Fragment = string.Empty;
                            state = State.Fragment;
                        }
                        // 4. Otherwise, if c is not the EOF code point:
                        else
                        {
                            // 1. Set url’s query to null.
                            url.Query = null;

                            // 2. Shorten url’s path.
                            ShortenURLPath(url);

                            // 3. Set state to path state and decrease pointer by 1.
                            state = State.Path;
                            pointer--;
                        }
                    }

                    break;
                }
                case State.RelativeSlash:
                {
                    // 1. If url is special and c is U+002F (/) or U+005C (\), then:
                    if (url.IsSpecial() && IsOneOf(c, CodePoint.Solidus, CodePoint.ReverseSolidus))
                    {
                        // 1. If c is U+005C (\), invalid-reverse-solidus validation error.
                        if (IsOneOf(c, CodePoint.ReverseSolidus))
                        {
                            hasValidationError = true;
                        }

                        // 2. Set state to special authority ignore slashes state.
                        state = State.SpecialAuthorityIgnoreSlashes;
                    }
                    // 2. Otherwise, if c is U+002F (/), then set state to authority state.
                    else if (IsOneOf(c, CodePoint.Solidus))
                    {
                        state = State.Authority;
                    }
                    // 3. Otherwise, set url’s username to base’s username, url’s password to base’s password,
                    //    url’s host to base’s host, url’s port to base’s port, state to path state,
                    //    and then, decrease pointer by 1.
                    else if (baseUrl is not null)
                    {
                        url.Username = baseUrl.Username;
                        url.Password = baseUrl.Password;
                        url.Host = baseUrl.Host;
                        url.Port = baseUrl.Port;
                        state = State.Path;
                        pointer--;
                    }

                    break;
                }
                case State.SpecialAuthoritySlashes:
                {
                    // 1. If c is U+002F (/) and remaining starts with U+002F (/), then
                    //    set state to special authority ignore slashes state and increase pointer by 1.
                    if (IsOneOf(c, CodePoint.Solidus) && remaining.StartsWith("/"))
                    {
                        state = State.SpecialAuthorityIgnoreSlashes;
                        pointer++;
                    }
                    // 2. Otherwise, special-scheme-missing-following-solidus validation error,
                    //    set state to special authority ignore slashes state and decrease pointer by 1.
                    else
                    {
                        hasValidationError = true;
                        state = State.SpecialAuthorityIgnoreSlashes;
                        pointer--;
                    }

                    break;
                }
                case State.SpecialAuthorityIgnoreSlashes:
                {
                    // 1. If c is neither U+002F (/) nor U+005C (\),
                    //    then set state to authority state and decrease pointer by 1.
                    if (!IsOneOf(c, CodePoint.Solidus, CodePoint.ReverseSolidus))
                    {
                        state = State.Authority;
                        pointer--;
                    }
                    // 2. Otherwise, special-scheme-missing-following-solidus validation error.
                    else
                    {
                        hasValidationError = true;
                    }

                    break;
                }
                case State.Authority:
                {
                    // Continue
                    break;
                }
                default:
                    throw new Exception("Unreachable.");
            }

            // TODO: Do we need this?
            pointer++;
        }

        return url;
    }

    // https://url.spec.whatwg.org/#shorten-a-urls-path
    public static void ShortenURLPath(URL url)
    {
        // 1. Assert: url does not have an opaque path.
        Debug.Assert(!url.HasOpaquePath());

        // 2. Let path be url’s path.
        var path = url.Path;

        // 3. If url’s scheme is "file", path’s size is 1, and path[0] is a normalized Windows drive letter, then return.
        if (url.HasFileScheme() && path.Count == 1 && IsNormalizedWindowsDriveLetter(path[0]))
        {
            return;
        }

        // 4. Remove path’s last item, if any.
        if (path.Count > 0)
        {
            path.RemoveAt(path.Count - 1);
        }
    }

    private static bool IsSpace(char c)
    {
        return c == (int)CodePoint.Space;
    }

    private static bool IsC0Control(char c)
    {
        return c >= (int)CodePoint.NullCharacter && c <= (int)CodePoint.UnitSeparator;
    }

    private static bool IsC0ControlOrSpace(char c)
    {
        return IsC0Control(c) || IsSpace(c);
    }

    private static bool IsControl(char c)
    {
        return IsC0Control(c) || (c >= (int)CodePoint.Delete && c <= (int)CodePoint.ApplicationProgramCommand);
    }

    private static bool IsASCIITab(char c)
    {
        return c == (int)CodePoint.Tab;
    }

    private static bool IsASCIINewLine(char c)
    {
        return c == (int)CodePoint.LineFeed || c == (int)CodePoint.CarriageReturn;
    }

    private static bool IsASCIITabOrNewLine(char c)
    {
        return IsASCIITab(c) || IsASCIINewLine(c);
    }


    public static bool IsASCIIDigit(char c)
    {
        return c >= (int)CodePoint.Zero && c <= (int)CodePoint.Nine; 
    }

    private static bool IsASCIIUpperAlpha(char c)
    {
        return c >= (int)CodePoint.UppercaseA && c <= (int)CodePoint.UppercaseZ;
    }

    private static bool IsASCIILowerAlpha(char c)
    {
        return c >= (int)CodePoint.LowercaseA && c <= (int)CodePoint.LowercaseZ;
    }

    private static bool IsASCIIAlpha(char c)
    {
        return IsASCIIUpperAlpha(c) || IsASCIILowerAlpha(c);
    }

    public static bool IsASCIIAlphaNumeric(char c)
    {
        return IsASCIIDigit(c) || IsASCIIAlpha(c);
    }

    public static bool IsOneOf(char c, params CodePoint[] codePoints)
    {
        return codePoints.Any(codePoint => (int)codePoint == c);
    }

    public static bool IsWindowsDriveLetter(string s)
    {
        return s.Length == 2 && IsASCIIAlpha(s[0]) && IsOneOf(s[1], CodePoint.Colon, CodePoint.VerticalLine);
    }
    
    public static bool IsNormalizedWindowsDriveLetter(string s)
    {
        return IsWindowsDriveLetter(s) && IsOneOf(s[1], CodePoint.Colon);
    }
}