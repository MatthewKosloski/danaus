namespace Danaus.Url;

public enum CodePoint
{
    Ampersand = 0x0026,
    Asterisk = 0x002A,
    Apostrophe = 0x0027,
    ApplicationProgramCommand = 0x009F,
    CarriageReturn = 0x000D,
    CircumflexAccent = 0x005E,
    Colon = 0x003A,
    Comma = 0x002C,
    CommercialAt = 0x0040,
    Delete = 0x007F,
    DollarSign = 0x0024,
    EqualsSign = 0x003D,
    ExclamationMark = 0x0021,
    GraveAccent = 0x0060,
    GreaterThanSign = 0x003E,
    HyphenMinus = 0x002D,
    LeftCurlyBracket = 0x007B,
    LeftParenthesis = 0x0028,
    LeftSquareBracket = 0x005B,
    LessThanSign = 0x003C,
    LineFeed = 0x000A,
    LowercaseA = 0x0061,
    LowercaseF = 0x0066,
    LowercaseZ = 0x007A,
    LowLine = 0x005F,
    Nine = 0x0039,
    NullCharacter = 0x0000,
    Number = 0x0023,
    PercentSign = 0x0025,
    Period = 0x002E,
    Plus = 0x002B,
    QuestionMark = 0x003F,
    QuotationMark = 0x022,
    ReverseSolidus = 0x005C,
    RightCurlyBracket = 0x007D,
    RightParenthesis = 0x0029,
    RightSquareBracket = 0x005D,
    Semicolon = 0x003B,
    Solidus = 0x002F,
    Space = 0x0020,
    Tab = 0x0009,
    Tilde = 0x007E,
    UnitSeparator = 0x001F,
    UppercaseA = 0x0041,
    UppercaseF = 0x0046,
    UppercaseZ = 0x005A,
    VerticalLine = 0x007C,
    Zero = 0x0030,
}

public static class CodePointExtension
{
    public static bool IsSpace(this uint codePoint)
    {
        return codePoint == (uint)CodePoint.Space;
    }

    public static bool IsC0Control(this uint codePoint)
    {
        return codePoint >= (uint)CodePoint.NullCharacter && codePoint <= (uint)CodePoint.UnitSeparator;
    }

    public static bool IsC0ControlOrSpace(this uint codePoint)
    {
        return IsC0Control(codePoint) || IsSpace(codePoint);
    }

    public static bool IsASCIITab(this uint codePoint)
    {
        return codePoint == (uint)CodePoint.Tab;
    }

    public static bool IsASCIINewLine(this uint codePoint)
    {
        return codePoint == (uint)CodePoint.LineFeed || codePoint == (uint)CodePoint.CarriageReturn;
    }

    public static bool IsASCIITabOrNewLine(this uint codePoint)
    {
        return IsASCIITab(codePoint) || IsASCIINewLine(codePoint);
    }

    public static bool IsASCIIDigit(this uint codePoint)
    {
        return codePoint >= (uint)CodePoint.Zero && codePoint <= (uint)CodePoint.Nine; 
    }

    public static bool IsASCIIUpperHexDigit(this uint codePoint)
    {
        return IsASCIIDigit(codePoint) || (codePoint >= (uint)CodePoint.UppercaseA && codePoint <= (uint)CodePoint.UppercaseF); 
    }

    public static bool IsASCIILowerHexDigit(this uint codePoint)
    {
        return IsASCIIDigit(codePoint) || (codePoint >= (uint)CodePoint.LowercaseA && codePoint <= (uint)CodePoint.LowercaseF); 
    }

    public static bool IsASCIIHexDigit(this uint codePoint)
    {
        return IsASCIIUpperHexDigit(codePoint) || IsASCIILowerHexDigit(codePoint);
    }

    public static bool IsASCIIUpperAlpha(this uint codePoint)
    {
        return codePoint >= (uint)CodePoint.UppercaseA && codePoint <= (uint)CodePoint.UppercaseZ;
    }

    public static bool IsASCIILowerAlpha(this uint codePoint)
    {
        return codePoint >= (uint)CodePoint.LowercaseA && codePoint <= (uint)CodePoint.LowercaseZ;
    }

    public static bool IsASCIIAlpha(this uint codePoint)
    {
        return IsASCIIUpperAlpha(codePoint) || IsASCIILowerAlpha(codePoint);
    }

    public static bool IsASCIIAlphaNumeric(this uint codePoint)
    {
        return IsASCIIDigit(codePoint) || IsASCIIAlpha(codePoint);
    }

    public static bool IsOneOf(this uint codePoint, params CodePoint[] codePoints)
    {
        return IsOneOf(codePoint, codePoints.Select((codePoint) => (uint)codePoint));
    }

    public static bool IsOneOf(this uint codePoint, IEnumerable<uint> codePoints)
    {
        return codePoints.Any(c => c == codePoint);
    }

    public static bool IsLeadingSurrogate(this uint codePoint)
    {
        return codePoint >= 0xD800 && codePoint <= 0xDBFF;
    }

    public static bool IsTrailingSurrogate(this uint codePoint)
    {
        return codePoint >= 0xDC00 && codePoint <= 0xDFFF;
    }

    public static bool IsSurrogate(this uint codePoint)
    {
        return IsLeadingSurrogate(codePoint) || IsTrailingSurrogate(codePoint);
    }

    public static bool IsNonCharacter(this uint codePoint)
    {
        return (codePoint >= 0xFDD0 && codePoint <= 0xFDEF) || IsOneOf(codePoint, [
            0xFFFE, 0xFFFF, 0x1FFFE, 0x1FFFF, 0x2FFFE, 0x2FFFF, 0x3FFFE, 0x3FFFF, 0x4FFFE,
            0x4FFFF, 0x5FFFE, 0x5FFFF, 0x6FFFE, 0x6FFFF, 0x7FFFE, 0x7FFFF, 0x8FFFE, 0x8FFFF,
            0x9FFFE, 0x9FFFF, 0xAFFFE, 0xAFFFF, 0xBFFFE, 0xBFFFF, 0xCFFFE, 0xCFFFF, 0xDFFFE,
            0xDFFFF, 0xEFFFE, 0xEFFFF, 0xFFFFE, 0xFFFFF, 0x10FFFE, 0x10FFFF
        ]);
    }

    public static bool IsURLCodePoint(this uint codePoint)
    {
        return codePoint.IsASCIIAlphaNumeric()
            || codePoint.IsOneOf(CodePoint.ExclamationMark, CodePoint.DollarSign, CodePoint.Ampersand,
                CodePoint.Apostrophe, CodePoint.LeftParenthesis, CodePoint.RightParenthesis,
                CodePoint.Asterisk, CodePoint.Plus, CodePoint.Comma, CodePoint.HyphenMinus,
                CodePoint.Period, CodePoint.Solidus, CodePoint.Colon, CodePoint.Semicolon,
                CodePoint.EqualsSign, CodePoint.QuestionMark, CodePoint.CommercialAt, CodePoint.LowLine,
                CodePoint.Tilde)
            ||  (codePoint >= 0x00A0 && codePoint <= 0x10FFFD && !codePoint.IsSurrogate() && !codePoint.IsNonCharacter());
    }

    public static bool IsForbiddenHostCodePoint(this uint codePoint)
    {
        return codePoint.IsOneOf(
            CodePoint.NullCharacter, CodePoint.Tab, CodePoint.LineFeed, CodePoint.CarriageReturn,
            CodePoint.Space, CodePoint.Number, CodePoint.Solidus, CodePoint.Colon, CodePoint.LessThanSign,
            CodePoint.GreaterThanSign, CodePoint.QuestionMark, CodePoint.CommercialAt, CodePoint.LeftSquareBracket,
            CodePoint.ReverseSolidus, CodePoint.RightSquareBracket, CodePoint.CircumflexAccent, CodePoint.VerticalLine);
    }
}