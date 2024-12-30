using System.Diagnostics;

namespace Danaus.Url;

public enum Encoding
{
    UTF8
}

public enum PercentEncodeSet
{
    C0Control,
    Fragment,
    Query,
    SpecialQuery,
    Path,
    Userinfo,
    Component,
    ApplicationXWWWFormUrlEncoded
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
    Apostrophe = 0x0027,
    ApplicationProgramCommand = 0x009F,
    CarriageReturn = 0x000D,
    Colon = 0x003A,
    Comma = 0x002C,
    CommercialAt = 0x0040,
    Delete = 0x007F,
    EqualsSign = 0x003D,
    ExclamationMark = 0x0021,
    GraveAccent = 0x0060,
    GreaterThanSign = 0x003E,
    HyphenMinus = 0x002D,
    LeftCurlyBracket = 0x007B,
    LessThanSign = 0x003C,
    LineFeed = 0x000A,
    LowercaseA = 0x0061,
    LowercaseZ = 0x007A,
    Nine = 0x0039,
    NullCharacter = 0x0000,
    Number = 0x0023,
    Period = 0x002E,
    Plus = 0x002B,
    QuestionMark = 0x003F,
    QuotationMark = 0x022,
    ReverseSolidus = 0x005C,
    RightCurlyBracket = 0x007D,
    Semicolon = 0x003B,
    Solidus = 0x002F,
    Space = 0x0020,
    Tab = 0x0009,
    Tilde = 0x007E,
    UnitSeparator = 0x001F,
    UppercaseA = 0x0041,
    UppercaseZ = 0x005A,
    VerticalLine = 0x007C,
    Zero = 0x0030,
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
                    else if (c == (uint)CodePoint.Colon)
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
                    // 1. If c is U+0040 (@), then:
                    if (IsOneOf(c, CodePoint.CommercialAt))
                    {
                        // 1. Invalid-credentials validation error.
                        hasValidationError = true;

                        // 2. If atSignSeen is true, then prepend "%40" to buffer.
                        if (atSignSeen)
                        {
                            buffer += "%40";
                        }

                        // 3. Set atSignSeen to true.
                        atSignSeen = true;

                        // 4. For each codePoint in buffer:
                        foreach (uint codePoint in buffer)
                        {
                            // 1. If codePoint is U+003A (:) and passwordTokenSeen is false,
                            //    then set passwordTokenSeen to true and continue.
                            if (IsOneOf(codePoint, CodePoint.Colon) && !passwordTokenSeen)
                            {
                                passwordTokenSeen = true;
                                continue;
                            }

                            // 2. Let encodedCodePoints be the result of running UTF-8 percent-encode
                            //    codePoint using the userinfo percent-encode set.
                            var encodedCodePoints = UTF8PercentEncode(codePoint, PercentEncodeSet.Userinfo);

                            // 3. If passwordTokenSeen is true, then append encodedCodePoints to url’s password.
                            // 4. Otherwise, append encodedCodePoints to url’s username.
                            if (passwordTokenSeen)
                            {
                                url.Password += encodedCodePoints;
                            }
                            else
                            {
                                url.Username += encodedCodePoints;
                            }
                        }

                        // 5. Set buffer to the empty string.
                        buffer = string.Empty;
                    }
                    // 2. Otherwise, if one of the following is true:
                    //      - c is the EOF code point, U+002F (/), U+003F (?), or U+0023 (#)
                    //      - url is special and c is U+005C (\)
                    else if (IsOneOf(c, CodePoint.Solidus, CodePoint.QuestionMark, CodePoint.Number) || (url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus)))
                    {
                        // 1. If atSignSeen is true and buffer is the empty string, host-missing validation error, return failure.
                        if (atSignSeen && buffer == string.Empty)
                        {
                            hasValidationError = true;
                        }

                        // 2. Decrease pointer by buffer’s code point length + 1, set buffer to the empty string,
                        // and set state to host state.
                        pointer -= buffer[buffer.Length + 1];
                        buffer = string.Empty;
                        state = State.Host;
                    }
                    // 3. Otherwise, append c to buffer.
                    else
                    {
                        buffer += c;
                    }
                    break;
                }
                case State.Host:
                case State.Hostname:
                {
                    // Continue.
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
    private static void ShortenURLPath(URL url)
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

    // https://url.spec.whatwg.org/#userinfo-percent-encode-set
    private static bool IsCodePointInPercentEncodeSet(uint codePoint, PercentEncodeSet set)
    {
        switch (set)
        {
            case PercentEncodeSet.C0Control:
                // The C0 control percent-encode set are the C0 controls and all code points greater than U+007E (~).
                var isC0Control = IsC0Control((char)codePoint);
                return isC0Control || codePoint > 0x007E;
            case PercentEncodeSet.Fragment:
                // The fragment percent-encode set is the C0 control percent-encode set
                // and U+0020 SPACE, U+0022 ("), U+003C (<), U+003E (>), and U+0060 (`).  
                var isInC0ControlPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.C0Control);
                return isInC0ControlPercentEncodeSet || IsOneOf(codePoint, CodePoint.Space, CodePoint.QuotationMark, CodePoint.LessThanSign, CodePoint.GreaterThanSign, CodePoint.GraveAccent);
            case PercentEncodeSet.Query:
                // The query percent-encode set is the C0 control percent-encode set
                // and U+0020 SPACE, U+0022 ("), U+0023 (#), U+003C (<), and U+003E (>).
                isInC0ControlPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.C0Control);
                return isInC0ControlPercentEncodeSet || IsOneOf(codePoint, CodePoint.Space, CodePoint.QuotationMark, CodePoint.Number, CodePoint.LessThanSign, CodePoint.GreaterThanSign);
            case PercentEncodeSet.SpecialQuery:
                // The special-query percent-encode set is the query percent-encode set and U+0027 (').
                var isInQueryPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Query);
                return isInQueryPercentEncodeSet || IsOneOf(codePoint, CodePoint.Apostrophe);
            case PercentEncodeSet.Path:
                // The path percent-encode set is the query percent-encode set and U+003F (?), U+0060 (`), U+007B ({), and U+007D (}).
                isInQueryPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Query);
                return isInQueryPercentEncodeSet || IsOneOf(codePoint, CodePoint.QuestionMark, CodePoint.GraveAccent, CodePoint.LeftCurlyBracket, CodePoint.RightCurlyBracket);
            case PercentEncodeSet.Userinfo:
                // The userinfo percent-encode set is the path percent-encode set
                // and U+002F (/), U+003A (:), U+003B (;), U+003D (=), U+0040 (@), U+005B ([) to U+005E (^), inclusive, and U+007C (|).
                var isInPathPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Path);
                return isInPathPercentEncodeSet || IsOneOf(codePoint, CodePoint.Solidus, CodePoint.Colon, CodePoint.Semicolon, CodePoint.EqualsSign, CodePoint.CommercialAt, CodePoint.VerticalLine) || (codePoint >= 0x005B && codePoint <= 0x005E);
            case PercentEncodeSet.Component:
                // The component percent-encode set is the userinfo percent-encode set
                // and U+0024 ($) to U+0026 (&), inclusive, U+002B (+), and U+002C (,).
                var isInUserinfoPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Userinfo);
                return isInUserinfoPercentEncodeSet || IsOneOf(codePoint, CodePoint.Plus, CodePoint.Comma) || (codePoint >= 0x0024 && codePoint <= 0x0026);
            case PercentEncodeSet.ApplicationXWWWFormUrlEncoded:
                // The application/x-www-form-urlencoded percent-encode set is the component percent-encode set
                // and U+0021 (!), U+0027 (') to U+0029 RIGHT PARENTHESIS, inclusive, and U+007E (~).
                var isInComponentPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Component);
                return isInComponentPercentEncodeSet || IsOneOf(codePoint, CodePoint.ExclamationMark, CodePoint.Tilde) || (codePoint >= 0x0027 && codePoint <= 0x0029);
            default:
                throw new Exception();
        }
    }

    private static string UTF8PercentEncode(uint codePoint, PercentEncodeSet set)
    {
        if (IsCodePointInPercentEncodeSet(codePoint, set))
        {
            uint firstByte, secondByte, thirdByte, fourthByte;
        
            if (codePoint >= 0x0000 && codePoint <= 0x007F)
            {
                // Encode as one byte, formatted as two hexadecimal digits padded with leading zeros.
                firstByte = codePoint;

                return string.Format("%{0:X2}", firstByte);
            }
            else if (codePoint >= 0x0080 && codePoint <= 0x07FF)
            {
                // Base-2 representation of codepoint is in the format of xxxyyyyzzzz.

                // The format of firstByte is 110xxxyy, where xxxyy are the
                // five leftmost bits in the codepoint. To get these bits,
                // we shift the bits over to the right six places, thereby
                // discarding the rightmost 6 bits, which are yyzzzz.
                // Then, to discard any remaining bits to the left of xxxyy, we apply
                // bitwise AND with 0x1f, which is 11111 in base-2.
                var xxxyy = (codePoint >> 6) & 0x1f;

                // To make the three leftmost bits in firstByte 110, we
                // apply bitwise OR with 0xc0, which is 1100 0000 in base-2.
                firstByte = xxxyy | 0xc0;

                // The format of secondByte is 10yyzzzz, where yyzzzz are the
                // six rightmost bits in the codepoint. To get these bits,
                // we apply bitwise AND with 0x3f, which is 0111 111 in base-2.
                // This effectively discards the five leftmost bits in the
                // codepoint, which are xxxyy.
                var yyzzzz = codePoint & 0x3f;
                
                // To make the two leftmost bits in secondByte 10, we
                // apply bitwise OR with 0x80, which is 1000 0000 in base-2.
                secondByte = yyzzzz | 0x80;

                // Encode as two bytes, each byte formatted as two hexadecimal digits padded with leading zeros.
                return string.Format("%{0:X2}%{1:X2}", firstByte, secondByte);
            }
            else if (codePoint >= 0x0800 && codePoint <= 0xFFFF)
            {
                // Base-2 representation of codepoint is in the format of wwwwxxxxyyyyzzzz.

                // The format of firstByte is 1110wwww, where wwww are the
                // four leftmost bits in the codepoint. To get these bits,
                // we shift the bits over to the right twelve places, thereby
                // discarding the rightmost 12 bits, which are xxxxyyyyzzzz.
                // Then, to discard any remaining bits to the left of wwww, we apply
                // bitwise AND with 0x0f, which is 1111 in base-2.
                var wwww = (codePoint >> 12) & 0x0f;

                // To make the four leftmost bits in firstByte 1110, we
                // apply bitwise OR with 0xe0, which is 1110 0000 in base-2.
                firstByte = wwww | 0xe0;

                // The format of secondByte is 10xxxxyy, where xxxxyy are the
                // middle bits in the codepoint. To get these bits, we first 
                // shift the bits over to the right six places, thereby
                // discarding the rightmost 6 bits, which are yyzzzz. Then,
                // to discard the four leftmost bits, we apply bitwise AND
                // with 0x3f, which is 0011 1111 in base-2.
                var xxxxyy = (codePoint >> 6) & 0x3f;
                
                // To make the two leftmost bits in secondByte 10, we
                // apply bitwise OR with 0x80, which is 1000 0000 in base-2.
                secondByte = xxxxyy | 0x80;
                
                // The format of thirdByte is 10yyzzzz, where yyzzzz are the
                // six rightmost bits in the codepoint. To get these bits, we
                // apply bitwise AND with 0x3f, wich is 0011 1111 in base-2.
                // This effectively discards the 10 leftmost bits,
                // which are wwwwxxxxyy.
                thirdByte = codePoint & 0x3f;

                // Encode as three bytes, each byte formatted as two hexadecimal digits padded with leading zeros.
                return string.Format("%{0:X2}%{1:X2}%{2:X2}", firstByte, secondByte, thirdByte);
            }
            else if (codePoint >= 0x010000 && codePoint <= 0x10FFFF)
            {
                // Base-2 representation of codepoint is in the form uvvvvwwwwxxxxyyyyzzzz.

                // The format of firstByte is 11110uvv, where uvv are the
                // three leftmost bits in the codepoint. To get these bits,
                // we shift the bits over to the right eighteen places, thereby
                // discarding the rightmost 18 bits, which are vvwwwwxxxxyyyyzzzz.
                // Then, to discard any remaining bits to the left of uvv, we apply
                // bitwise AND with 0x007, which is 0111 in base-2.
                var uvv = (codePoint >> 18) & 0x07;

                // To make the five leftmost bits in firstByte 11110, we
                // apply bitwise OR with 0xf0, which is 1111 0000 in base-2.
                firstByte = uvv | 0xf0;

                // The format of secondByte is 10vvwwww, where vvwwww are the
                // the middle bits in the codepoint. To get these bits, we first 
                // shift the bits over to the right twelve places, thereby
                // discarding the rightmost 12 bits, which are xxxxyyyyzzzz. Then,
                // to discard the three leftmost bits uvv, we apply bitwise AND
                // with 0x3f, which is 0011 1111 in base-2.
                var vvwwww = (codePoint >> 12) & 0x3f;

                // To make the two leftmost bits in secondByte 10, we
                // apply bitwise OR with 0x80, which is 1000 0000 in base-2.
                secondByte = vvwwww | 0x80;

                // The format of thirdByte is 10xxxxyy, where xxxxyy are the
                // the middle bits in the codepoint. To get these bits, we first 
                // shift the bits over to the right six places, thereby
                // discarding the rightmost 6 bits, which are yyzzzz. Then,
                // to discard the nine leftmost bits uvvvvwwww, we apply bitwise AND
                // with 0x3f, which is 0011 1111 in base-2.
                var xxxxyy = (codePoint >> 6) & 0x3f;

                // To make the two leftmost bits in thirdByte 10, we
                // apply bitwise OR with 0x80, which is 1000 0000 in base-2.
                thirdByte = xxxxyy | 0x80;

                // The format of fourthByte is 10yyzzzz, where yyzzzz are the
                // rightmost six bits in the codepoint. To get these bits, we
                // apply bitwise AND with 0x3f, which is 0011 1111 in base-2.
                // This effectively discards the leftmost 15 bits.
                var yyzzzz = codePoint & 0x3f;

                // To make the two leftmost bits in fourthByte 10, we
                // apply bitwise OR with 0x80, which is 1000 0000 in base-2.
                fourthByte = yyzzzz | 0x80; 

                // Encode as four bytes, each byte formatted as two hexadecimal digits padded with leading zeros.
                return string.Format("%{0:X2}%{1:X2}%{2:X2}%{3:X2}", firstByte, secondByte, thirdByte, fourthByte);
            }
            else
            {
                throw new UnreachableException("Unexpected range of codepoint");
            }
        }
        else
        {
            return string.Format("{0}", codePoint);
        }
    }

    // https://url.spec.whatwg.org/#host-parsing
    private static string ParseHost(string input, bool isOpaque = false)
    {
        // 1. If input starts with U+005B ([), then:
        if (input.StartsWith('['))
        {
            // 1. If input does not end with U+005D (]), IPv6-unclosed validation error, return failure.
            if (!input.EndsWith(']'))
            {
                
            }

            // 2. Return the result of IPv6 parsing input with its leading U+005B ([) and trailing U+005D (]) removed.
            return ParseIPv6Address(input);
        }

        // 2. If isOpaque is true, then return the result of opaque-host parsing input.
        if (isOpaque)
        {
            return ParseOpaqueHost(input);
        }

        // 3. Assert: input is not the empty string.
        Debug.Assert(input != string.Empty);

        // 4. Let domain be the result of running UTF-8 decode without BOM on the percent-decoding of input.
    }

    // https://url.spec.whatwg.org/#concept-opaque-host-parser
    private static string ParseOpaqueHost(string input)
    {
        return string.Empty;
    }

    private static string ParseIPv4Address(string input)
    {
        return string.Empty;
    }

    private static string ParseIPv6Address(string input)
    {
        throw new NotImplementedException("IPv6 address parsing is unsupported at the moment.");
    }

    private static bool IsSpace(char c)
    {
        return c == (uint)CodePoint.Space;
    }

    private static bool IsC0Control(char c)
    {
        return c >= (uint)CodePoint.NullCharacter && c <= (uint)CodePoint.UnitSeparator;
    }

    private static bool IsC0ControlOrSpace(char c)
    {
        return IsC0Control(c) || IsSpace(c);
    }

    private static bool IsControl(char c)
    {
        return IsC0Control(c) || (c >= (uint)CodePoint.Delete && c <= (uint)CodePoint.ApplicationProgramCommand);
    }

    private static bool IsASCIITab(char c)
    {
        return c == (uint)CodePoint.Tab;
    }

    private static bool IsASCIINewLine(char c)
    {
        return c == (uint)CodePoint.LineFeed || c == (uint)CodePoint.CarriageReturn;
    }

    private static bool IsASCIITabOrNewLine(char c)
    {
        return IsASCIITab(c) || IsASCIINewLine(c);
    }

    private static bool IsASCIIDigit(char c)
    {
        return c >= (uint)CodePoint.Zero && c <= (uint)CodePoint.Nine; 
    }

    private static bool IsASCIIUpperAlpha(char c)
    {
        return c >= (uint)CodePoint.UppercaseA && c <= (uint)CodePoint.UppercaseZ;
    }

    private static bool IsASCIILowerAlpha(char c)
    {
        return c >= (uint)CodePoint.LowercaseA && c <= (uint)CodePoint.LowercaseZ;
    }

    private static bool IsASCIIAlpha(char c)
    {
        return IsASCIIUpperAlpha(c) || IsASCIILowerAlpha(c);
    }

    private static bool IsASCIIAlphaNumeric(char c)
    {
        return IsASCIIDigit(c) || IsASCIIAlpha(c);
    }

    private static bool IsOneOf(char c, params CodePoint[] codePoints)
    {
        return IsOneOf((uint)c, codePoints);
    }

    private static bool IsOneOf(uint c, params CodePoint[] codePoints)
    {
        return codePoints.Any(codePoint => (uint)codePoint == c);
    }

    private static bool IsWindowsDriveLetter(string s)
    {
        return s.Length == 2 && IsASCIIAlpha(s[0]) && IsOneOf(s[1], CodePoint.Colon, CodePoint.VerticalLine);
    }
    
    private static bool IsNormalizedWindowsDriveLetter(string s)
    {
        return IsWindowsDriveLetter(s) && IsOneOf(s[1], CodePoint.Colon);
    }
}