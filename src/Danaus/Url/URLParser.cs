using System.Diagnostics;
using System.Text;

namespace Danaus.Url;

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

public class URLParser
{
    private const uint END_OF_FILE = 0xFFFFFFFF;

    // https://url.spec.whatwg.org/#url-parsing
    public static ParseResult Parse(string input, URL? baseUrl = null, Encoding encoding = Encoding.UTF8)
    {
        // 1. Let result be the result of running the basic URL parser on input with base and encoding.
        var result = BasicParse(input, baseUrl, encoding) ?? throw new Exception("Failed to parse");

        // 3. If url's scheme is not "blob", return url.
        if (!result.Url.HasBlobScheme())
        {
            return result;
        }

        // 4. Set url's blob URL entry to the result of resolving the blob URL url, 
        //    if that did not return failure, and null otherwise.
        // TODO

        // 5. Return url.
        // TODO
        return result;
    }

    // https://url.spec.whatwg.org/#concept-basic-url-parser
    private static ParseResult? BasicParse(string input, URL? baseUrl = null, Encoding encoding = Encoding.UTF8, URL? url = null, State? stateOverride = null)
    {
        var processedInput = input;
        ParseResult result;

        // 1. If url is not given:
        if (url == null)
        {
            // 1. Set url to a new URL.
            url = new URL();
            result = new ParseResult(url);
            
            // 2. If input contains any leading or trailing C0 control or space, invalid-URL-unit validation error.
            var startIndex = 0;
            var endIndex = input.Length - 1;

            for (; startIndex < input.Length; startIndex++)
            {
                if (IsC0ControlOrSpace(input[startIndex]))
                {
                    result.AddError(ValidationError.InvalidURLUnit);
                    continue;
                }

                break;
            }

            for (; endIndex > startIndex; endIndex--)
            {
                if (IsC0ControlOrSpace(input[endIndex]))
                {
                    result.AddError(ValidationError.InvalidURLUnit);
                    continue;
                }

                break;
            }

            // 3. Remove any leading and trailing C0 control or space from input.
            processedInput = input.Substring(startIndex, endIndex + 1);
        }
        else
        {
            result = new ParseResult(url);
        }

        // 2. If input contains any ASCII tab or newline, invalid-URL-unit validation error.
        // 3. Remove all ASCII tab or newline from input.
        foreach (char ch in processedInput)
        {
            if (IsASCIITabOrNewLine(ch))
            {
                result.AddError(ValidationError.InvalidURLUnit);
                processedInput = processedInput.Replace((char)CodePoint.Tab, (char)CodePoint.NullCharacter)
                    .Replace((char)CodePoint.LineFeed, (char)CodePoint.NullCharacter)
                    .Replace((char)CodePoint.CarriageReturn, (char)CodePoint.NullCharacter);
                break;
            }
        }
        
        // 4. Let state be state override if given, or scheme start state otherwise.
        // 6. Let buffer be the empty string.
        // 7. Let atSignSeen, insideBrackets, and passwordTokenSeen be false.
        // 8. Let pointer be a pointer for input.
        var context = new BasicParsingContext(processedInput, result, encoding, stateOverride, baseUrl);
        context.GoToNextCodePoint();

        // 9. Keep running the following state machine by switching on state. 
        while (true)
        {
            ParseResult? res = null;
            
            switch (context.State)
            {
                case State.SchemeStart: res = HandleSchemeStartState(context); break;
                case State.Scheme: res = HandleSchemeState(context); break;
                case State.NoScheme: res = HandleNoSchemeState(context); break;
                case State.SpecialRelativeOrAuthority: res = HandleSpecialRelativeOrAuthorityState(context); break;
                case State.PathOrAuthority: res = HandlePathOrAuthorityState(context); break;
                case State.Relative: res = HandleRelativeState(context); break;
                case State.RelativeSlash: res = HandleRelativeSlashState(context); break;
                case State.SpecialAuthoritySlashes: res = HandleSpecialAuthoritySlashesState(context); break;
                case State.SpecialAuthorityIgnoreSlashes: res = HandleSpecialAuthorityIgnoreSlashesState(context); break;
                case State.Authority: res = HandleAuthorityState(context); break;
                case State.Host:
                case State.Hostname: res = HandleHostState(context); break;
                case State.Port: res = HandlePortState(context); break;
                case State.File: res = HandleFileState(context); break;
                case State.FileSlash: res = HandleFileSlashState(context); break;
                case State.FileHost: res = HandleFileHostState(context); break;
                case State.PathStart: res = HandlePathStartState(context); break;
                case State.Path: res = HandlePathState(context); break;
                case State.OpaquePath: res = HandleOpaquePathState(context); break;
                case State.Query: res = HandleQueryState(context); break;
                case State.Fragment: res = HandleFragmentState(context); break;
                default:
                    throw new Exception("Unreachable.");
            }

            if (res is not null)
            {
                return res;
            }

            // If, after a run, pointer points to the EOF code point, go to the next step.
            if (context.IsEOF())
            {
                break;
            }

            // Otherwise, increase pointer by 1 and continue with the state machine.
            context.GoToNextCodePoint();
        }

        return context.Result;
    }

    private static ParseResult? HandleSchemeStartState(BasicParsingContext context)
    {
        // 1. If c is an ASCII alpha, append c, lowercased, to buffer, and set state to scheme state.
        if (IsASCIIAlpha(context.CodePoint))
        {
            context.AppendLoweredCodePointToBuffer();
            context.SetState(State.Scheme);
        }
        // 2. Otherwise, if state override is not given, set state to no scheme state and decrease pointer by 1.
        else if (!context.HasStateOverride)
        {
            context.SetState(State.NoScheme);
            context.GoToPreviousCodePoint();
        }
        // 3. Otherwise, return failure.
        else
        {
            throw new URLParseFailureException();
        }

        return null;
    }

    private static ParseResult? HandleSchemeState(BasicParsingContext context)
    {
        var (c, result, url, baseUrl) = context;

        // 1. If c is an ASCII alphanumeric, U+002B (+), U+002D (-), or U+002E (.), append c, lowercased, to buffer.
        if (IsASCIIAlphaNumeric(c) || IsOneOf(c, CodePoint.Plus, CodePoint.HyphenMinus, CodePoint.Period))
        {
            context.AppendLoweredCodePointToBuffer();
        }
        // 2. Otherwise, if c is U+003A (:), then:
        else if (c == (uint)CodePoint.Colon)
        {
            // 1. If state override is given, then:
            if (context.HasStateOverride)
            {
                // 1. If url’s scheme is a special scheme and buffer is not a special scheme, then return.
                // 2. If url’s scheme is not a special scheme and buffer is a special scheme, then return.
                if (url.IsSpecial() && !SpecialScheme.IsSpecialScheme(context.Buffer)
                    || !url.IsSpecial() && SpecialScheme.IsSpecialScheme(context.Buffer))
                {
                    return result;
                }

                // 3. If url includes credentials or has a non-null port, and buffer is "file", then return.
                if ((url.IncludesCredentials() || url.HasPort()) && context.Buffer == "file")
                {
                    return result;
                }

                // 4. If url’s scheme is "file" and its host is an empty host, then return.
                if (url.HasFileScheme() && url.HasEmptyHost())
                {
                    return result;
                }
            }

            // 2. Set url’s scheme to buffer.
            url.Scheme = context.Buffer;

            // 3. If state override is given, then:
            if (context.HasStateOverride)
            {
                // 1. If url’s port is url’s scheme’s default port, then set url’s port to null.
                if (url.Port == url.SpecialScheme?.Port)
                {
                    url.Port = null;
                }

                // 2. Return.
                return result;
            }

            // 4. Set buffer to the empty string.
            context.ClearBuffer();

            // 5. If url’s scheme is "file", then:
            if (url.HasFileScheme())
            {
                // 1. If remaining does not start with "//", special-scheme-missing-following-solidus validation error.
                if(!context.Remaining.StartsWith("//"))
                {
                    result.AddError(ValidationError.SpecialSchemeMissingFollowingSolidus);
                }
                context.SetState(State.File);
            }
            // 6. Otherwise, if url is special, base is non-null, and base’s scheme is url’s scheme:
            else if (url.IsSpecial() && baseUrl?.Scheme == url.Scheme)
            {
                // 1. Assert: base is special (and therefore does not have an opaque path).
                Debug.Assert(baseUrl.IsSpecial());

                // 2. Set state to special relative or authority state.
                context.SetState(State.SpecialRelativeOrAuthority);
            }
            // 7. Otherwise, if url is special, set state to special authority slashes state.
            else if (url.IsSpecial())
            {
                context.SetState(State.SpecialAuthoritySlashes);
            }
            // 8. Otherwise, if remaining starts with an U+002F (/),
            //    set state to path or authority state and increase pointer by 1.
            else if (context.Remaining.StartsWith("/"))
            {
                context.SetState(State.PathOrAuthority);
                context.GoToNextCodePoint();
            }
            // 9. Otherwise, set url’s path to the empty string and set state to opaque path state.
            else
            {
                url.Path = [""];
                context.SetState(State.OpaquePath);
            }
        }
        // 3. Otherwise, if state override is not given,
        //    set buffer to the empty string, state to no scheme state,
        //    and start over (from the first code point in input).
        else if (!context.HasStateOverride)
        {
            context.ClearBuffer();
            context.SetState(State.NoScheme);
            context.ResetPointer();
        }
        // 4. Otherwise, return failure.
        else
        {
            throw new URLParseFailureException();
        }

        return null;
    }

    private static ParseResult? HandleNoSchemeState(BasicParsingContext context)
    {
        var (c, result, url, baseUrl) = context;

        // 1. If base is null, or base has an opaque path and c is not U+0023 (#),
        //    missing-scheme-non-relative-URL validation error, return failure.
        if (baseUrl is null || baseUrl is not null && baseUrl.HasOpaquePath() && !IsOneOf(c, CodePoint.Number))
        {
            result.AddError(ValidationError.MissingSchemeNonRelativeURL);
            return null;
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
            context.SetState(State.Fragment);
        }
        // 3. Otherwise, if base’s scheme is not "file", set state to relative state and decrease pointer by 1.
        else if (baseUrl is not null && !baseUrl.HasFileScheme())
        {
            context.SetState(State.Relative);
            context.GoToPreviousCodePoint();
        }
        // 4. Otherwise, set state to file state and decrease pointer by 1.
        else
        {
            context.SetState(State.File);
            context.GoToPreviousCodePoint();
        }

        return null;
    }

    private static ParseResult? HandleSpecialRelativeOrAuthorityState(BasicParsingContext context)
    {
        var (c, result, _, _) = context;

        // 1. If c is U+002F (/) and remaining starts with U+002F (/), then
        //    set state to special authority ignore slashes state and increase pointer by 1.
        if (IsOneOf(c, CodePoint.Solidus) && context.Remaining.StartsWith("/"))
        {
            context.SetState(State.SpecialAuthorityIgnoreSlashes);
            context.GoToNextCodePoint();
        }
        // 2. Otherwise, special-scheme-missing-following-solidus validation error,
        //    set state to relative state and decrease pointer by 1.
        else
        {
            result.AddError(ValidationError.SpecialSchemeMissingFollowingSolidus);
            context.SetState(State.Relative);
            context.GoToPreviousCodePoint();
        }

        return null;
    }

    private static ParseResult? HandlePathOrAuthorityState(BasicParsingContext context)
    {
        var (c, _, _, _) = context;

        // 1. If c is U+002F (/), then set state to authority state.
        if (IsOneOf(c, CodePoint.Solidus))
        {
            context.SetState(State.Authority);
        }
        // 2. Otherwise, set state to path state, and decrease pointer by 1.
        else
        {
            context.SetState(State.Path);
            context.GoToPreviousCodePoint();
        }
        
        return null;
    }

    private static ParseResult? HandleRelativeState(BasicParsingContext context)
    {
        var (c, result, url, baseUrl) = context;

        // 1. Assert: base’s scheme is not "file".
        Debug.Assert(baseUrl is not null && baseUrl.Scheme != "file");

        // 2. Set url’s scheme to base’s scheme.
        url.Scheme = baseUrl.Scheme;

        // 3. If c is U+002F (/), then set state to relative slash state.
        if (IsOneOf(c, CodePoint.Solidus))
        {
            context.SetState(State.RelativeSlash);
        }
        // 4. Otherwise, if url is special and c is U+005C (\),
        //    invalid-reverse-solidus validation error, set state to relative slash state.
        else if (url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus))
        {
            result.AddError(ValidationError.InvalidReverseSolidus);
            context.SetState(State.RelativeSlash);
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
                context.SetState(State.Query);
            }
            // 3. Otherwise, if c is U+0023 (#), set url’s fragment to the empty string and state to fragment state.
            else if (IsOneOf(c, CodePoint.Number))
            {
                url.Fragment = string.Empty;
                context.SetState(State.Fragment);
            }
            // 4. Otherwise, if c is not the EOF code point:
            else if (!context.IsEOF())
            {
                // 1. Set url’s query to null.
                url.Query = null;

                // 2. Shorten url’s path.
                ShortenURLPath(url);

                // 3. Set state to path state and decrease pointer by 1.
                context.SetState(State.Path);
                context.GoToPreviousCodePoint();
            }
        }

        return null;
    }

    private static ParseResult? HandleRelativeSlashState(BasicParsingContext context)
    {
        var (c, result, url, baseUrl) = context;

        // 1. If url is special and c is U+002F (/) or U+005C (\), then:
        if (url.IsSpecial() && IsOneOf(c, CodePoint.Solidus, CodePoint.ReverseSolidus))
        {
            // 1. If c is U+005C (\), invalid-reverse-solidus validation error.
            if (IsOneOf(c, CodePoint.ReverseSolidus))
            {
                result.AddError(ValidationError.InvalidReverseSolidus);
            }

            // 2. Set state to special authority ignore slashes state.
            context.SetState(State.SpecialAuthorityIgnoreSlashes);
        }
        // 2. Otherwise, if c is U+002F (/), then set state to authority state.
        else if (IsOneOf(c, CodePoint.Solidus))
        {
            context.SetState(State.Authority);
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
            context.SetState(State.Path);
            context.GoToPreviousCodePoint();
        }

        return null;
    }

    private static ParseResult? HandleSpecialAuthoritySlashesState(BasicParsingContext context)
    {
        var (c, result, _, _) = context;

        // 1. If c is U+002F (/) and remaining starts with U+002F (/), then
        //    set state to special authority ignore slashes state and increase pointer by 1.
        if (IsOneOf(c, CodePoint.Solidus) && context.Remaining.StartsWith("/"))
        {
            context.SetState(State.SpecialAuthorityIgnoreSlashes);
            context.GoToNextCodePoint();
        }
        // 2. Otherwise, special-scheme-missing-following-solidus validation error,
        //    set state to special authority ignore slashes state and decrease pointer by 1.
        else
        {
            result.AddError(ValidationError.SpecialSchemeMissingFollowingSolidus);
            context.SetState(State.SpecialAuthorityIgnoreSlashes);
            context.GoToPreviousCodePoint();
        }

        return null;
    }

    private static ParseResult? HandleSpecialAuthorityIgnoreSlashesState(BasicParsingContext context)
    {
        var (c, result, _, _) = context;

        // 1. If c is neither U+002F (/) nor U+005C (\),
        //    then set state to authority state and decrease pointer by 1.
        if (!IsOneOf(c, CodePoint.Solidus, CodePoint.ReverseSolidus))
        {
            context.SetState(State.Authority);
            context.GoToPreviousCodePoint();
        }
        // 2. Otherwise, special-scheme-missing-following-solidus validation error.
        else
        {
            result.AddError(ValidationError.SpecialSchemeMissingFollowingSolidus);
        }

        return null;
    }

    private static ParseResult? HandleAuthorityState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;

        // 1. If c is U+0040 (@), then:
        if (IsOneOf(c, CodePoint.CommercialAt))
        {
            // 1. Invalid-credentials validation error.
            result.AddError(ValidationError.InvalidCredentials);

            // 2. If atSignSeen is true, then prepend "%40" to buffer.
            if (context.AtSignSeen)
            {
                // TODO
                // context.Buffer += "%40";
            }

            // 3. Set atSignSeen to true.
            context.SetAtSignSeen();

            // 4. For each codePoint in buffer:
            foreach (uint codePoint in context.Buffer)
            {
                // 1. If codePoint is U+003A (:) and passwordTokenSeen is false,
                //    then set passwordTokenSeen to true and continue.
                if (IsOneOf(codePoint, CodePoint.Colon) && !context.PasswordTokenSeen)
                {
                    context.SetPasswordTokenSeen();
                    continue;
                }

                // 2. Let encodedCodePoints be the result of running UTF-8 percent-encode
                //    codePoint using the userinfo percent-encode set.
                var encodedCodePoints = UTF8PercentEncode(codePoint, PercentEncodeSet.Userinfo);

                // 3. If passwordTokenSeen is true, then append encodedCodePoints to url’s password.
                // 4. Otherwise, append encodedCodePoints to url’s username.
                if (context.PasswordTokenSeen)
                {
                    url.Password += encodedCodePoints;
                }
                else
                {
                    url.Username += encodedCodePoints;
                }
            }

            // 5. Set buffer to the empty string.
            context.ClearBuffer();
        }
        // 2. Otherwise, if one of the following is true:
        //      - c is the EOF code point, U+002F (/), U+003F (?), or U+0023 (#)
        //      - url is special and c is U+005C (\)
        else if (context.IsEOF() || IsOneOf(c, CodePoint.Solidus, CodePoint.QuestionMark, CodePoint.Number)
            || (url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus)))
        {
            // 1. If atSignSeen is true and buffer is the empty string, host-missing validation error, return failure.
            if (context.AtSignSeen && context.Buffer == string.Empty)
            {
                result.AddError(ValidationError.HostMissing);
                throw new URLParseFailureException();
            }

            // 2. Decrease pointer by buffer’s code point length + 1, set buffer to the empty string,
            // and set state to host state.
            context.DecrementBufferBy(context.Buffer.Length + 1);
            context.ClearBuffer();
            context.SetState(State.Host);
        }
        // 3. Otherwise, append c to buffer.
        else
        {
            context.AppendCodePointToBuffer();
        }
        
        return null;
    }

    private static ParseResult? HandleHostState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;
        
        // 1. If state override is given and url’s scheme is "file", then decrease pointer by 1 and set state to file host state.
        if (context.HasStateOverride && url.HasFileScheme())
        {
            context.GoToPreviousCodePoint();
            context.SetState(State.FileHost);
        }
        // 2. Otherwise, if c is U+003A (:) and insideBrackets is false, then:
        else if (IsOneOf(c, CodePoint.Colon) && !context.InsideBrackets)
        {
            // 1. If buffer is the empty string, host-missing validation error, return failure.
            if (context.Buffer == string.Empty)
            {
                result.AddError(ValidationError.HostMissing);
                throw new URLParseFailureException();
            }
            
            // 2. If state override is given and state override is hostname state, then return.
            if (context.HasStateOverride && context.StateOverride == State.Hostname)
            {
                return result;
            }
            
            // 3. Let host be the result of host parsing buffer with url is not special.
            var host = ParseHost(context.Buffer, !url.IsSpecial());
            
            // FIXME: 4. If host is failure, then return failure.
            if (host == string.Empty)
            {
                throw new URLParseFailureException();
            } 

            // 5. Set url’s host to host, buffer to the empty string, and state to port state.
            url.Host = host;
            context.ClearBuffer();
            context.SetState(State.Port);
        }
        // 3. Otherwise, if one of the following is true:
        //      - c is the EOF code point, U+002F (/), U+003F (?), or U+0023 (#)
        //      - url is special and c is U+005C (\)
        else if (context.IsEOF()
            || IsOneOf(c, CodePoint.Solidus, CodePoint.QuestionMark, CodePoint.Number)
            || (url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus)))
        {
            // then decrease pointer by 1, and then:
            context.GoToPreviousCodePoint();

            // 1. If url is special and buffer is the empty string,
            //    host-missing validation error, return failure.
            if (url.IsSpecial() && context.Buffer == string.Empty)
            {
                result.AddError(ValidationError.HostMissing);
                throw new URLParseFailureException();
            }
            // 2. Otherwise, if state override is given, buffer is the empty string, 
            //    and either url includes credentials or url’s port is non-null, return.
            else if (context.HasStateOverride && context.Buffer == string.Empty && (url.IncludesCredentials() || url.Port is not null))
            {
                return result;
            }

            // 3. Let host be the result of host parsing buffer with url is not special.
            var host = ParseHost(context.Buffer, !url.IsSpecial());

            // FIXME: 4. If host is failure, then return failure.
            if (host == string.Empty)
            {
                throw new URLParseFailureException();
            } 

            // 5. Set url’s host to host, buffer to the empty string, and state to path start state.
            url.Host = host;
            context.ClearBuffer();
            context.SetState(State.PathStart);

            // 6. If state override is given, then return.
            if (context.HasStateOverride)
            {
                return result;
            }
        }
        // Otherwise:
        else
        {
            // 1. If c is U+005B ([), then set insideBrackets to true.
            if (IsOneOf(c, CodePoint.LeftSquareBracket))
            {
                context.SetInsideBrackets(true);
            }
            
            // 2. If c is U+005D (]), then set insideBrackets to false.
            if (IsOneOf(c, CodePoint.RightSquareBracket))
            {
                context.SetInsideBrackets(false);
            }
            
            // 3. Append c to buffer.
            context.AppendCodePointToBuffer();
        }

        return null;
    }

    private static ParseResult? HandlePortState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;

        // 1. If c is an ASCII digit, append c to buffer.
        if (IsASCIIDigit(c))
        {
            context.AppendCodePointToBuffer();
        }
        // 2. Otherwise, if one of the following is true:
        //      - c is the EOF code point, U+002F (/), U+003F (?), or U+0023 (#)
        //      - url is special and c is U+005C (\)
        //      - state override is given
        else if (context.IsEOF()
            || IsOneOf(c, CodePoint.Solidus, CodePoint.QuestionMark, CodePoint.Number)
            || (url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus))
            || context.HasStateOverride)
        {
            // 1. If buffer is not the empty string, then:
            if (context.Buffer != string.Empty)
            {
                // 1. Let port be the mathematical integer value that is represented by buffer
                //    in radix-10 using ASCII digits for digits with values 0 through 9.
                var port = ParseASCIIInt(context.Buffer);

                // 2. If port is not a 16-bit unsigned integer, port-out-of-range validation error, return failure.
                var isU16 = port >= ushort.MinValue && port <= ushort.MaxValue;
                if (!isU16)
                {
                    result.AddError(ValidationError.PortOutOfRange);
                    throw new URLParseFailureException();
                }

                // 3. Set url’s port to null, if port is url’s scheme’s default port;
                //    otherwise to port.
                url.Port = port != url.SpecialScheme?.Port
                    ? (ushort)port
                    : null;

                // 4. Set buffer to the empty string.
                context.ClearBuffer();
            }

            // 2. If state override is given, then return.
            if (context.HasStateOverride)
            {
                return result;
            }

            // 3. Set state to path start state and decrease pointer by 1.
            context.SetState(State.PathStart);
            context.GoToPreviousCodePoint();
        }
        // 3. Otherwise, port-invalid validation error, return failure.
        else
        {
            result.AddError(ValidationError.PortInvalid);
            throw new URLParseFailureException();
        }

        return null;
    }

    private static ParseResult? HandleFileState(BasicParsingContext context)
    {
        var (c, result, url, baseUrl) = context;

        // 1. Set url’s scheme to "file".
        // 2. Set url’s host to the empty string.
        url.Scheme = "file";
        url.Host = string.Empty;

        // 3. If c is U+002F (/) or U+005C (\), then:
        if (IsOneOf(c, CodePoint.Solidus, CodePoint.ReverseSolidus))
        {
            // 1. If c is U+005C (\), invalid-reverse-solidus validation error.
            if (IsOneOf(c, CodePoint.ReverseSolidus))
            {
                result.AddError(ValidationError.InvalidReverseSolidus);
            }

            // 2. Set state to file slash state.
            context.SetState(State.FileSlash);
        }
        // 4. Otherwise, if base is non-null and base’s scheme is "file":
        else if (baseUrl is not null && baseUrl.HasFileScheme())
        {
            // 1. Set url’s host to base’s host, url’s path to a clone of base’s path,
            //    and url’s query to base’s query.
            url.Host = baseUrl.Host;
            url.Path = [.. baseUrl.Path];
            url.Query = baseUrl.Query;

            // 2. If c is U+003F (?), then set url’s query to the empty string and state to query state.
            if (IsOneOf(c, CodePoint.QuestionMark))
            {
                url.Query = string.Empty;
                context.SetState(State.Query);
            }
            // 3. Otherwise, if c is U+0023 (#), set url’s fragment to the empty string and state to fragment state.
            else if (IsOneOf(c, CodePoint.Number))
            {
                url.Fragment = string.Empty;
                context.SetState(State.Fragment);
            }
            // 4. Otherwise:
            else
            {
                // 1. Set url’s query to null.
                url.Query = null;

                // 2. If the code point substring from pointer to the end of input
                //    does not start with a Windows drive letter, then shorten url’s path.
                if (!IsWindowsDriveLetter(context.Input[context.Pointer..][0] + ""))
                {
                    ShortenURLPath(url);
                }
                // 3. Otherwise:
                else
                {
                    // 1. File-invalid-Windows-drive-letter validation error.
                    result.AddError(ValidationError.FileInvalidWindowsDriveLetter);
                    url.Path = [" "];
                }

                // 4. Set state to path state and decrease pointer by 1.
                context.SetState(State.Path);
                context.GoToPreviousCodePoint();
            }
        }
        // 5. Otherwise, set state to path state, and decrease pointer by 1.
        else
        {
            context.SetState(State.Path);
            context.GoToPreviousCodePoint();
        }

        return null;
    }

    private static ParseResult? HandleFileSlashState(BasicParsingContext context)
    {
        var (c, result, url, baseUrl) = context;
        
        // 1. If c is U+002F (/) or U+005C (\), then:
        if (IsOneOf(c, CodePoint.Solidus, CodePoint.ReverseSolidus))
        {
            // 1. If c is U+005C (\), invalid-reverse-solidus validation error.
            if (IsOneOf(c, CodePoint.ReverseSolidus))
            {
                result.AddError(ValidationError.InvalidReverseSolidus);
            }

            // 2. Set state to file host state.
            context.SetState(State.FileHost);
        }
        // 2. Otherwise:
        else
        {
            // 1. If base is non-null and base’s scheme is "file", then:
            if (baseUrl is not null && baseUrl.HasFileScheme())
            {
                // 1. Set url’s host to base’s host.
                url.Host = baseUrl.Host;

                // 2. If the code point substring from pointer to the end of input
                //    does not start with a Windows drive letter and
                //    base’s path[0] is a normalized Windows drive letter, then
                //    append base’s path[0] to url’s path.
                if (!IsWindowsDriveLetter(context.Input[context.Pointer..][0] + "") && IsNormalizedWindowsDriveLetter(baseUrl.Path[0]))
                {
                    url.Path.Add(baseUrl.Path[0]);
                }
            }

            // 2. Set state to path state, and decrease pointer by 1.
            context.SetState(State.Path);
            context.GoToPreviousCodePoint();
        }

        return null;
    }

    private static ParseResult? HandleFileHostState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;

        // 1. If c is the EOF code point, U+002F (/), U+005C (\), U+003F (?),
        //    or U+0023 (#), then decrease pointer by 1 and then:
        if (context.IsEOF() || IsOneOf(c, CodePoint.Solidus, CodePoint.ReverseSolidus, CodePoint.QuestionMark, CodePoint.Number))
        {
            context.GoToPreviousCodePoint();

            // 1. If state override is not given and buffer is a Windows drive letter,
            //    file-invalid-Windows-drive-letter-host validation error, set state to path state.
            if (!context.HasStateOverride && IsWindowsDriveLetter(context.Buffer))
            {
                result.AddError(ValidationError.FileInvalidWindowsDriveLetterHost);
                context.SetState(State.Path);
            }
            // 2. Otherwise, if buffer is the empty string, then:
            else if (context.Buffer == string.Empty)
            {
                // 1. Set url’s host to the empty string.
                url.Host = string.Empty;

                // 2. If state override is given, then return.
                if (context.HasStateOverride)
                {
                    return result;
                }

                // 3. Set state to path start state.
                context.SetState(State.Path);
            }
            // 3. Otherwise, run these steps:
            else
            {
                // 1. Let host be the result of host parsing buffer with url is not special.
                var host = ParseHost(context.Buffer, !url.IsSpecial());

                // FIXME
                // 2. If host is failure, then return failure.
                if (host == string.Empty)
                {
                    throw new URLParseFailureException();
                }

                // 3. If host is "localhost", then set host to the empty string.
                if (host == "localhost")
                {
                    host = string.Empty;
                }

                // 4. Set url’s host to host.
                url.Host = host;

                // 5. If state override is given, then return.
                if (context.HasStateOverride)
                {
                    return result;
                }

                // 6. Set buffer to the empty string and state to path start state.
                context.ClearBuffer();
                context.SetState(State.PathStart);
            }
        }
        // 2. Otherwise, append c to buffer.
        else
        {
            context.AppendCodePointToBuffer();
        }

        return null;
    }

    private static ParseResult? HandlePathStartState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;

        // 1. If url is special, then:
        if (url.IsSpecial())
        {
            // 1. If c is U+005C (\), invalid-reverse-solidus validation error.
            if (IsOneOf(c, CodePoint.ReverseSolidus))
            {
                result.AddError(ValidationError.InvalidReverseSolidus);
            }

            // 2. Set state to path state.
            context.SetState(State.Path);

            // 3. If c is neither U+002F (/) nor U+005C (\), then decrease pointer by 1.
            if (!IsOneOf(c, CodePoint.Solidus, CodePoint.ReverseSolidus))
            {
                context.GoToPreviousCodePoint();
            }
        }
        // 2. Otherwise, if state override is not given and c is U+003F (?),
        //    set url’s query to the empty string and state to query state.
        else if (!context.HasStateOverride && IsOneOf(c, CodePoint.QuestionMark))
        {
            url.Query = string.Empty;
            context.SetState(State.Query);
        }
        // 3. Otherwise, if state override is not given and c is U+0023 (#),
        //    set url’s fragment to the empty string and state to fragment state.
        else if (!context.HasStateOverride && IsOneOf(c, CodePoint.Number))
        {
            url.Fragment = string.Empty;
            context.SetState(State.Fragment);
        }
        // 5. Otherwise, if state override is given and url’s host is null,
        //    append the empty string to url’s path.
        else if (context.HasStateOverride && url.Host is null)
        {
            url.Path.Add(string.Empty);
        }
        // 4. Otherwise, if c is not the EOF code point:
        else if (!context.IsEOF())
        {
            // 1. Set state to path state.
            context.SetState(State.Path);

            // 2. If c is not U+002F (/), then decrease pointer by 1.
            if (!IsOneOf(c, CodePoint.Solidus))
            {
                context.GoToPreviousCodePoint();
            }
        }

        return null;
    }

    private static ParseResult? HandlePathState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;

        // 1. If one of the following is true:
        //      - c is the EOF code point or U+002F (/)
        //      - url is special and c is U+005C (\)
        //      - state override is not given and c is U+003F (?) or U+0023 (#)
        if (context.IsEOF()
            || IsOneOf(c, CodePoint.Solidus)
            || (url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus))
            || (!context.HasStateOverride && IsOneOf(c, CodePoint.QuestionMark, CodePoint.Number)))
        {
            // 1. If url is special and c is U+005C (\), invalid-reverse-solidus validation error.
            if (url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus))
            {
                result.AddError(ValidationError.InvalidReverseSolidus);
            }

            // 2. If buffer is a double-dot URL path segment, then:
            if (IsDoubleDotURLPathSegment(context.Buffer))
            {
                // 1. Shorten url’s path.
                ShortenURLPath(url);
                // 2. If neither c is U+002F (/), nor url is special and c is U+005C (\),
                //    append the empty string to url’s path.
                if (!IsOneOf(c, CodePoint.Solidus) || !(url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus)))
                {
                    url.Path.Add(string.Empty);
                }
            }
            // 3. Otherwise, if buffer is a single-dot URL path segment
            //    and if neither c is U+002F (/), nor url is special and c is U+005C (\),
            //    append the empty string to url’s path.
            else if (IsSingleDotURLPathSegment(context.Buffer) && (!IsOneOf(c, CodePoint.Solidus) || !(url.IsSpecial() && IsOneOf(c, CodePoint.ReverseSolidus))))
            {
                url.Path.Add(string.Empty);
            }
            // 4. Otherwise, if buffer is not a single-dot URL path segment, then:
            else if (!IsSingleDotURLPathSegment(context.Buffer))
            {
                // 1. If url’s scheme is "file", url’s path is empty, and buffer is a Windows drive letter,
                //    then replace the second code point in buffer with U+003A (:).
                if (url.HasFileScheme() && url.HasEmptyPath() && IsWindowsDriveLetter(context.Buffer))
                {
                    context.SetBuffer(context.Buffer[..1] + ":" + context.Buffer[2..]);
                }

                // 2. Append buffer to url’s path.
                url.Path.Add(context.Buffer);
            }

            // 5. Set buffer to the empty string.
            context.ClearBuffer();

            // 6. If c is U+003F (?), then set url’s query to the empty string and state to query state.
            if (IsOneOf(c, CodePoint.QuestionMark))
            {
                url.Query = string.Empty;
                context.SetState(State.Query);
            }

            // 7. If c is U+0023 (#), then set url’s fragment to the empty string and state to fragment state.
            if (IsOneOf(c, CodePoint.Number))
            {
                url.Fragment = string.Empty;
                context.SetState(State.Fragment);
            }
        }
        // 2. Otherwise, run these steps:
        else
        {
            // 1. If c is not a URL code point and not U+0025 (%), invalid-URL-unit validation error.
            if (!IsURLCodePoint(c) && !IsOneOf(c, CodePoint.PercentSign))
            {
                result.AddError(ValidationError.InvalidURLUnit);
            }

            // 2. If c is U+0025 (%) and remaining does not start with two ASCII hex digits,
            //    invalid-URL-unit validation error.
            if (IsOneOf(c, CodePoint.PercentSign) && !StartsWithTwoASCIIHexDigits(context.Remaining))
            {
                // FIXME
                result.AddError(ValidationError.InvalidURLUnit);
            }
            
            // 3. UTF-8 percent-encode c using the path percent-encode set and append the result to buffer.
            context.AppendToBuffer(UTF8PercentEncode(c, PercentEncodeSet.Path));
        }
        
        return null;
    }

    private static ParseResult? HandleOpaquePathState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;

        // 1. If c is U+003F (?), then set url’s query to the empty string and state to query state.
        if (IsOneOf(c, CodePoint.QuestionMark))
        {
            url.Query = string.Empty;
            context.SetState(State.Query);
        }
        // 2. Otherwise, if c is U+0023 (#), then set url’s fragment to the empty string and state to fragment state.
        else if (IsOneOf(c, CodePoint.Number))
        {
            url.Fragment = string.Empty;
            context.SetState(State.Fragment);
        }
        // 3. Otherwise:
        else
        {
            // 1. If c is not the EOF code point, not a URL code point,
            //    and not U+0025 (%), invalid-URL-unit validation error.
            if (!context.IsEOF() && !IsURLCodePoint(c) && !IsOneOf(c, CodePoint.PercentSign))
            {
                result.AddError(ValidationError.InvalidURLUnit);
            }

            // 2. If c is U+0025 (%) and remaining does not start with two ASCII hex digits,
            //    invalid-URL-unit validation error.
            if (IsOneOf(c, CodePoint.PercentSign) && !StartsWithTwoASCIIHexDigits(context.Remaining))
            {
                result.AddError(ValidationError.InvalidURLUnit);
            }

            // 3. If c is not the EOF code point, UTF-8 percent-encode c
            //    using the C0 control percent-encode set and append the result to url’s path.
            if (!context.IsEOF())
            {
                url.Path.Add(UTF8PercentEncode(c, PercentEncodeSet.C0Control));
            }
        }

        return null;
    }

    private static ParseResult? HandleQueryState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;

        // 1. If encoding is not UTF-8 and one of the following is true:
        //      - url is not special
        //      - url’s scheme is "ws" or "wss"
        if (context.Encoding != Encoding.UTF8 && (!url.IsSpecial() || url.HasWsScheme() || url.HasWssScheme()))
        {
            // then set encoding to UTF-8.
            context.SetEncoding(Encoding.UTF8);
        }

        // 2. If one of the following is true:
        //      - state override is not given and c is U+0023 (#)
        //      - c is the EOF code point
        if ((!context.HasStateOverride && IsOneOf(c, CodePoint.Number)) || context.IsEOF())
        {
            // 1. Let queryPercentEncodeSet be the special-query percent-encode set
            //    if url is special; otherwise the query percent-encode set.
            var queryPercentEncodeSet = url.IsSpecial()
                ? PercentEncodeSet.SpecialQuery
                : PercentEncodeSet.Query;

            // 2. Percent-encode after encoding, with encoding, buffer, and queryPercentEncodeSet,
            //    and append the result to url’s query.
            url.Query += PercentEncodeAfterEncoding(context.Encoding, context.Buffer, queryPercentEncodeSet);

            // 3. Set buffer to the empty string.
            context.ClearBuffer();

            // 4. If c is U+0023 (#), then set url’s fragment to the empty string and state to fragment state.
            if (IsOneOf(c, CodePoint.Number))
            {
                url.Fragment = string.Empty;
                context.SetState(State.Fragment);
            }
        }
        // 3. Otherwise, if c is not the EOF code point:
        else if (!context.IsEOF())
        {
            // 1. If c is not a URL code point and not U+0025 (%), invalid-URL-unit validation error.
            if (!IsURLCodePoint(c) && !IsOneOf(c, CodePoint.PercentSign))
            {
                result.AddError(ValidationError.InvalidURLUnit);
            }

            // 2. If c is U+0025 (%) and remaining does not start with two ASCII hex digits,
            //    invalid-URL-unit validation error.
            if (IsOneOf(c, CodePoint.PercentSign) && !StartsWithTwoASCIIHexDigits(context.Remaining))
            {
                result.AddError(ValidationError.InvalidURLUnit);
            }

            // 3. Append c to buffer.
            context.AppendCodePointToBuffer();
        }

        return null;
    }

    private static ParseResult? HandleFragmentState(BasicParsingContext context)
    {
        var (c, result, url, _) = context;

        if(!context.IsEOF())
        {
            // 1. If c is not a URL code point and not U+0025 (%), invalid-URL-unit validation error.
            if (!IsURLCodePoint(c) && !IsOneOf(c, CodePoint.PercentSign))
            {
                result.AddError(ValidationError.InvalidURLUnit);
            }

            // 2. If c is U+0025 (%) and remaining does not start with two ASCII hex digits,
            //    invalid-URL-unit validation error.
            if (IsOneOf(c, CodePoint.PercentSign) && !StartsWithTwoASCIIHexDigits(context.Remaining))
            {
                result.AddError(ValidationError.InvalidURLUnit);
            }   

            // 3. UTF-8 percent-encode c using the fragment percent-encode set and append the result to url’s fragment.
            // NOTE: percent-encode is done on EOF on the entire buffer.
            context.AppendCodePointToBuffer();
        }
        else
        {
            url.Fragment = PercentEncodeAfterEncoding(Encoding.UTF8, context.Buffer, PercentEncodeSet.Fragment);
            context.ClearBuffer();
        }

        return null;
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
            return string.Format("{0}", (char)codePoint);
        }
    }

    private static string UTF8PercentDecode(string input)
    {
        // 1. Let output be an empty byte sequence.
        StringBuilder builder = new();

        // 2. For each byte byte in input:
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            var isPercentSign = c == (uint)CodePoint.PercentSign;

            // 1. If byte is not 0x25 (%), then append byte to output.
            if (!isPercentSign)
            {
                builder.Append(c);
            }
            // 2. Otherwise, if byte is 0x25 (%) and the next two bytes after byte in input are not
            //    in the ranges 0x30 (0) to 0x39 (9), 0x41 (A) to 0x46 (F), and 0x61 (a) to 0x66 (f),
            //    all inclusive, append byte to output.
            else if (isPercentSign && i + 2 < input.Length && !IsASCIIHexDigit(input[i + 1]) && !IsASCIIHexDigit(input[i + 2]))
            {
                builder.Append(c);
            }
            // 3. Otherwise:
            else
            {
                // 1. Let bytePoint be the two bytes after byte in input, decoded, and then interpreted as hexadecimal number.
                //    NOTE: We shift left to append the next byte.
                byte bytePoint = (byte)((ParseASCIIHexDigit(input[i + 1]) << 4) | ParseASCIIHexDigit(input[i + 1]));

                // 2. Append a byte whose value is bytePoint to output.
                builder.Append(bytePoint);

                // 3. Skip the next two bytes in input.
                i+=2;
            }
        }

        // 3. Return output.
        return builder.ToString();
    }

    // https://url.spec.whatwg.org/#string-percent-encode-after-encoding
    private static string PercentEncodeAfterEncoding(Encoding encoding, string input, PercentEncodeSet percentEncodeSet, bool spaceAsPlus = false)
    {
        var output = new StringBuilder();

        foreach (char c in input)
        {
            if (spaceAsPlus && c == (uint)CodePoint.Space)
            {
                output.Append(CodePoint.Plus);
                continue;
            }

            var isomorph = c;

            if (!IsCodePointInPercentEncodeSet(isomorph, percentEncodeSet))
            {
                output.Append(isomorph);
            }
            else
            {
                output.Append(UTF8PercentEncode(isomorph, percentEncodeSet));
            }
        }

        return output.ToString();
    }

    private static uint ParseASCIIHexDigit(char c)
    {
        if (IsASCIIHexDigit(c))
        {
            return c;
        }
        else
        {
            throw new Exception("Verify not reached.");
        }
    }

    private static int ParseASCIIInt(string input)
    {
        StringBuilder str = new();

        foreach (char c in input)
        {
            if (IsASCIIDigit(c))
            {
                str.Append(c);
            }
            else
            {
                throw new Exception("Verify not reached");
            }
        }

        if (!int.TryParse(str.ToString(), out int result))
        {
            throw new Exception("Verify not reached");
        }
        
        return result;
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
                // FIXME
                return string.Empty;
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
        var domain = UTF8PercentDecode(input);

        // 5. FIXME: Let asciiDomain be the result of running domain to ASCII with domain and false.

        // 6. FIXME: If asciiDomain is failure, then return failure.

        // 7. FIXME: If asciiDomain ends in a number, then return the result of IPv4 parsing asciiDomain.

        // 8. FIXME: Return asciiDomain.
        return domain;
    }

    // https://url.spec.whatwg.org/#concept-opaque-host-parser
    private static string ParseOpaqueHost(string input)
    {
        // 1. If input contains a forbidden host code point, host-invalid-code-point validation error, return failure.
        if (input.Any(IsForbiddenHostCodePoint))
        {
            // FIXME
            return string.Empty;
        }

        // 2. If input contains a code point that is not a URL code point and not U+0025 (%), invalid-URL-unit validation error.
        if (input.Any((c) => !(IsURLCodePoint(c) || IsOneOf(c, CodePoint.PercentSign))))
        {
            // FIXME
            return string.Empty;
        }

        // 3. If input contains a U+0025 (%) and the two code points following it are not ASCII hex digits, invalid-URL-unit validation error.
        for (int i = 0; i < input.Length; i++)
        {
            var isPercentSign = input[i] == (uint)CodePoint.PercentSign;
            var hasAtLeastTwoCharsOfLookahead = i + 2 < input.Length;
            var isInvalid = !(isPercentSign && hasAtLeastTwoCharsOfLookahead && IsASCIIHexDigit(input[i +1]) && IsASCIIDigit(input[i + 2]));
            
            if (isInvalid)
            {
                // FIXME
                return string.Empty;
            }
        }
        
        // 4. Return the result of running UTF-8 percent-encode on input using the C0 control percent-encode set.
        var result = new StringBuilder();
        foreach (char c in input)
        {
            result.Append(UTF8PercentEncode(c, PercentEncodeSet.C0Control));
        }

        return result.ToString();
    }

    private static string ParseIPv6Address(string input)
    {
        throw new NotImplementedException("IPv6 address parsing is unsupported at the moment.");
    }

    private static bool IsSpace(uint c)
    {
        return c == (uint)CodePoint.Space;
    }

    private static bool IsC0Control(uint c)
    {
        return c >= (uint)CodePoint.NullCharacter && c <= (uint)CodePoint.UnitSeparator;
    }

    private static bool IsC0ControlOrSpace(uint c)
    {
        return IsC0Control(c) || IsSpace(c);
    }

    private static bool IsASCIITab(uint c)
    {
        return c == (uint)CodePoint.Tab;
    }

    private static bool IsASCIINewLine(uint c)
    {
        return c == (uint)CodePoint.LineFeed || c == (uint)CodePoint.CarriageReturn;
    }

    private static bool IsASCIITabOrNewLine(uint c)
    {
        return IsASCIITab(c) || IsASCIINewLine(c);
    }

    private static bool IsASCIIDigit(uint c)
    {
        return c >= (uint)CodePoint.Zero && c <= (uint)CodePoint.Nine; 
    }

    private static bool IsASCIIUpperHexDigit(uint c)
    {
        return IsASCIIDigit(c) || (c >= (uint)CodePoint.UppercaseA && c <= (uint)CodePoint.UppercaseF); 
    }

    private static bool IsASCIILowerHexDigit(uint c)
    {
        return IsASCIIDigit(c) || (c >= (uint)CodePoint.LowercaseA && c <= (uint)CodePoint.LowercaseF); 
    }

    public static bool IsASCIIHexDigit(uint c)
    {
        return IsASCIIUpperHexDigit(c) || IsASCIILowerHexDigit(c);
    }

    private static bool IsASCIIUpperAlpha(uint c)
    {
        return c >= (uint)CodePoint.UppercaseA && c <= (uint)CodePoint.UppercaseZ;
    }

    private static bool IsASCIILowerAlpha(uint c)
    {
        return c >= (uint)CodePoint.LowercaseA && c <= (uint)CodePoint.LowercaseZ;
    }

    private static bool IsASCIIAlpha(uint c)
    {
        return IsASCIIUpperAlpha(c) || IsASCIILowerAlpha(c);
    }

    private static bool IsASCIIAlphaNumeric(uint c)
    {
        return IsASCIIDigit(c) || IsASCIIAlpha(c);
    }

    private static bool IsOneOf(uint c, params CodePoint[] codePoints)
    {
        return IsOneOf(c, codePoints.Select((codePoint) => (uint)codePoint));
    }

    private static bool IsOneOf(uint c, IEnumerable<uint> codePoints)
    {
        return codePoints.Any(codePoint => codePoint == c);
    }

    private static bool IsWindowsDriveLetter(string s)
    {
        return s.Length == 2 && IsASCIIAlpha(s[0]) && IsOneOf(s[1], CodePoint.Colon, CodePoint.VerticalLine);
    }
    
    private static bool IsNormalizedWindowsDriveLetter(string s)
    {
        return IsWindowsDriveLetter(s) && IsOneOf(s[1], CodePoint.Colon);
    }

    private static bool IsLeadingSurrogate(uint c)
    {
        return c >= 0xD800 && c <= 0xDBFF;
    }

    private static bool IsTrailingSurrogate(uint c)
    {
        return c >= 0xDC00 && c <= 0xDFFF;
    }

    private static bool IsSurrogate(uint c)
    {
        return IsLeadingSurrogate(c) || IsTrailingSurrogate(c);
    }

    private static bool IsNonCharacter(uint c)
    {
        return (c >= 0xFDD0 && c <= 0xFDEF) || IsOneOf(c, [
            0xFFFE, 0xFFFF, 0x1FFFE, 0x1FFFF, 0x2FFFE, 0x2FFFF, 0x3FFFE, 0x3FFFF, 0x4FFFE,
            0x4FFFF, 0x5FFFE, 0x5FFFF, 0x6FFFE, 0x6FFFF, 0x7FFFE, 0x7FFFF, 0x8FFFE, 0x8FFFF,
            0x9FFFE, 0x9FFFF, 0xAFFFE, 0xAFFFF, 0xBFFFE, 0xBFFFF, 0xCFFFE, 0xCFFFF, 0xDFFFE,
            0xDFFFF, 0xEFFFE, 0xEFFFF, 0xFFFFE, 0xFFFFF, 0x10FFFE, 0x10FFFF
        ]);
    }

    private static bool IsURLCodePoint(uint c)
    {
        return IsASCIIAlphaNumeric(c)
            || IsOneOf(c, CodePoint.ExclamationMark, CodePoint.DollarSign, CodePoint.Ampersand,
                CodePoint.Apostrophe, CodePoint.LeftParenthesis, CodePoint.RightParenthesis,
                CodePoint.Asterisk, CodePoint.Plus, CodePoint.Comma, CodePoint.HyphenMinus,
                CodePoint.Period, CodePoint.Solidus, CodePoint.Colon, CodePoint.Semicolon,
                CodePoint.EqualsSign, CodePoint.QuestionMark, CodePoint.CommercialAt, CodePoint.LowLine,
                CodePoint.Tilde)
            ||  (c >= 0x00A0 && c <= 0x10FFFD && !IsSurrogate(c) && !IsNonCharacter(c));
    }

    private static bool IsForbiddenHostCodePoint(char c)
    {
        return IsOneOf(c,
            CodePoint.NullCharacter, CodePoint.Tab, CodePoint.LineFeed, CodePoint.CarriageReturn,
            CodePoint.Space, CodePoint.Number, CodePoint.Solidus, CodePoint.Colon, CodePoint.LessThanSign,
            CodePoint.GreaterThanSign, CodePoint.QuestionMark, CodePoint.CommercialAt, CodePoint.LeftSquareBracket,
            CodePoint.ReverseSolidus, CodePoint.RightSquareBracket, CodePoint.CircumflexAccent, CodePoint.VerticalLine);
    }

    private static bool IsSingleDotURLPathSegment(string input)
    {
        return new List<string>([".", "%2e"]).Any((segment) =>
            string.Equals(segment, input, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDoubleDotURLPathSegment(string input)
    {
        return new List<string>(["..", ".%2e", "%2e.", "%2e%2e"]).Any((segment) =>
            string.Equals(segment, input, StringComparison.OrdinalIgnoreCase));
    }

    private static bool StartsWithTwoASCIIHexDigits(string input)
    {
        return input.Length >= 2 && IsASCIIHexDigit(input[0]) && IsASCIIHexDigit(input[1]);
    }
}