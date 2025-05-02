using Danaus.Core;

namespace Danaus.HTML;

abstract class HTMLToken
{
    public bool IsCharacterToken()
    {
        return this is CharacterToken;
    }

    public bool IsCommentToken()
    {
        return this is CommentToken;
    }

    public bool IsDocTypeToken()
    {
        return this is DocTypeToken;
    }

    public bool IsEndOfFileToken()
    {
        return this is EndOfFileToken;
    }

    public bool IsTagToken()
    {
        return this is TagToken;
    }

    public bool IsStartTag()
    {
        return this is TagToken token && token.Type == TagTokenType.Start;
    }

    public bool IsEndTag()
    {
        return this is TagToken token && token.Type == TagTokenType.End;
    }

    public bool IsStartTag(Core.TagName name)
    {
        return IsStartTag() && name == ((TagToken)this).Name;
    }

    public bool IsEndTag(Core.TagName name)
    {
        return IsEndTag() && name == ((TagToken)this).Name;
    }

    public bool IsOneOfTags(params Core.TagName[] tagNames)
    {
        return this is TagToken token && tagNames.Any(t => t.Name == token.Name);
    }

    public bool IsOneOfStartTags(params Core.TagName[] tagNames)
    {
        return IsStartTag() && IsOneOfTags(tagNames);
    }

    public bool IsOneOfEndTags(params Core.TagName[] tagNames)
    {
        return IsEndTag() && IsOneOfTags(tagNames);
    }

    public bool IsWhiteSpaceCharacter()
    {
        if (this is CharacterToken tok)
        {
            uint data = tok.Data;
            return data.IsOneOf(
                CodePoint.Tab,
                CodePoint.LineFeed,
                CodePoint.FormFeed,
                CodePoint.Space
            );
        }

        return false;
    }

    public bool IsCodePoint(CodePoint codePoint)
    {
        if (this is CharacterToken tok)
        {
            uint data = tok.Data;
            return data.Is(codePoint);
        }

        return false;
    }
}