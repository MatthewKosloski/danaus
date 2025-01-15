using System.Diagnostics;
using System.Text;

namespace Danaus.Url;

public enum PercentEncodeSet
{
    ApplicationXWWWFormUrlEncoded,
    C0Control,
    Component,
    Fragment,
    Path,
    Query,
    SpecialQuery,
    Userinfo,
}

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
    public List<string> Path { get; set; } = path ?? [];
    public string? Query { get; set; } = query;
    public string? Fragment { get; set; } = fragment;

    public SpecialScheme? SpecialScheme
    {
        get
        {
          return SpecialScheme.GetFromScheme(Scheme);
        }
    }

    // https://url.spec.whatwg.org/#userinfo-percent-encode-set
    private static bool IsCodePointInPercentEncodeSet(uint codePoint, PercentEncodeSet set)
    {
        switch (set)
        {
            case PercentEncodeSet.C0Control:
                // The C0 control percent-encode set are the C0 controls and all code points greater than U+007E (~).
                var isC0Control = codePoint.IsC0Control();
                return isC0Control || codePoint > 0x007E;
            case PercentEncodeSet.Fragment:
                // The fragment percent-encode set is the C0 control percent-encode set
                // and U+0020 SPACE, U+0022 ("), U+003C (<), U+003E (>), and U+0060 (`).  
                var isInC0ControlPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.C0Control);
                return isInC0ControlPercentEncodeSet || codePoint.IsOneOf(CodePoint.Space, CodePoint.QuotationMark, CodePoint.LessThanSign, CodePoint.GreaterThanSign, CodePoint.GraveAccent);
            case PercentEncodeSet.Query:
                // The query percent-encode set is the C0 control percent-encode set
                // and U+0020 SPACE, U+0022 ("), U+0023 (#), U+003C (<), and U+003E (>).
                isInC0ControlPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.C0Control);
                return isInC0ControlPercentEncodeSet || codePoint.IsOneOf(CodePoint.Space, CodePoint.QuotationMark, CodePoint.Number, CodePoint.LessThanSign, CodePoint.GreaterThanSign);
            case PercentEncodeSet.SpecialQuery:
                // The special-query percent-encode set is the query percent-encode set and U+0027 (').
                var isInQueryPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Query);
                return isInQueryPercentEncodeSet || codePoint.IsOneOf(CodePoint.Apostrophe);
            case PercentEncodeSet.Path:
                // The path percent-encode set is the query percent-encode set and U+003F (?), U+0060 (`), U+007B ({), and U+007D (}).
                isInQueryPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Query);
                return isInQueryPercentEncodeSet || codePoint.IsOneOf(CodePoint.QuestionMark, CodePoint.GraveAccent, CodePoint.LeftCurlyBracket, CodePoint.RightCurlyBracket);
            case PercentEncodeSet.Userinfo:
                // The userinfo percent-encode set is the path percent-encode set
                // and U+002F (/), U+003A (:), U+003B (;), U+003D (=), U+0040 (@), U+005B ([) to U+005E (^), inclusive, and U+007C (|).
                var isInPathPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Path);
                return isInPathPercentEncodeSet
                    || codePoint.IsOneOf(CodePoint.Solidus, CodePoint.Colon, CodePoint.Semicolon, CodePoint.EqualsSign, CodePoint.CommercialAt, CodePoint.VerticalLine)
                    || (codePoint >= 0x005B && codePoint <= 0x005E);
            case PercentEncodeSet.Component:
                // The component percent-encode set is the userinfo percent-encode set
                // and U+0024 ($) to U+0026 (&), inclusive, U+002B (+), and U+002C (,).
                var isInUserinfoPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Userinfo);
                return isInUserinfoPercentEncodeSet
                    || codePoint.IsOneOf(CodePoint.Plus, CodePoint.Comma)
                    || (codePoint >= 0x0024 && codePoint <= 0x0026);
            case PercentEncodeSet.ApplicationXWWWFormUrlEncoded:
                // The application/x-www-form-urlencoded percent-encode set is the component percent-encode set
                // and U+0021 (!), U+0027 (') to U+0029 RIGHT PARENTHESIS, inclusive, and U+007E (~).
                var isInComponentPercentEncodeSet = IsCodePointInPercentEncodeSet(codePoint, PercentEncodeSet.Component);
                return isInComponentPercentEncodeSet
                    || codePoint.IsOneOf(CodePoint.ExclamationMark, CodePoint.Tilde)
                    || (codePoint >= 0x0027 && codePoint <= 0x0029);
            default:
                throw new Exception();
        }
    }

    public static string UTF8PercentEncode(uint codePoint, PercentEncodeSet set)
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

    public static string UTF8PercentDecode(string input)
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
            else if (isPercentSign && i + 2 < input.Length && !((uint)input[i + 1]).IsASCIIHexDigit() && !((uint)input[i + 2]).IsASCIIHexDigit())
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
    public static string PercentEncodeAfterEncoding(Encoding encoding, string input, PercentEncodeSet percentEncodeSet, bool spaceAsPlus = false)
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
        if (((uint)c).IsASCIIHexDigit())
        {
            return c;
        }
        else
        {
            throw new Exception("Verify not reached.");
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

    public override bool Equals(Object? other)
    {
        if (other == null || !(other is URL))
        {
            return false;
        }
        else
        {
            var otherURL = (URL)other;
            return otherURL.Scheme == Scheme
                && otherURL.Username == Username
                && otherURL.Password == Password
                && otherURL.Host == Host
                && otherURL.Port == Port
                && otherURL.Path.SequenceEqual(Path)
                && otherURL.Query == Query
                && otherURL.Fragment == Fragment;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Scheme.GetHashCode(), Username.GetHashCode(), Password.GetHashCode(),
            Host?.GetHashCode(), Port.GetHashCode(), Path.GetHashCode(), Query?.GetHashCode(), Fragment?.GetHashCode());
    }

}