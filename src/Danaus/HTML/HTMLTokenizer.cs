using System.Text;
using Danaus.Core;

namespace Danaus.HTML;

enum State
{
    AfterAttributeName,
    AfterAttributeValueQuoted,
    AfterDOCTYPEName,
    AfterDOCTYPEPublicIdentifier,
    AfterDOCTYPEPublicKeyword,
    AfterDOCTYPESystemIdentifier,
    AfterDOCTYPESystemKeyword,
    AmbiguousAmpersand,
    AttributeName,
    AttributeValueDoubleQuoted,
    AttributeValueSingleQuoted,
    AttributeValueUnquoted,
    BeforeAttributeName,
    BeforeAttributeValue,
    BeforeDOCTYPEName,
    BeforeDOCTYPEPublicIdentifier,
    BeforeDOCTYPESystemIdentifier,
    BetweenDOCTYPEPublicAndSystemIdentifiers,
    BogusComment,
    BogusDOCTYPE,
    CDATASection,
    CDATASectionBracket,
    CDATASectionEnd,
    CharacterReference,
    Comment,
    CommentEnd,
    CommentEndBang,
    CommentEndDash,
    CommentLessThanSign,
    CommentLessThanSignBang,
    CommentLessThanSignBangDash,
    CommentLessThanSignBangDashDash,
    CommentStart,
    CommentStartDash,
    Data,
    DecimalCharacterReference,
    DecimalCharacterReferenceStart,
    DOCTYPE,
    DOCTYPEName,
    DOCTYPEPublicIdentifierDoubleQuoted,
    DOCTYPEPublicIdentifierSingleQuoted,
    DOCTYPESystemIdentifierDoubleQuoted,
    DOCTYPESystemIdentifierSingleQuoted,
    EndTagOpen,
    HexadecimalCharacterReference,
    HexadecimalCharacterReferenceStart,
    MarkupDeclarationOpen,
    NamedCharacterReference,
    NumericCharacterReference,
    NumericCharacterReferenceEnd,
    PLAINTEXT,
    RAWTEXT,
    RAWTEXTEndTagName,
    RAWTEXTEndTagOpen,
    RAWTEXTLessThanSign,
    RCDATA,
    RCDATAEndTagName,
    RCDATAEndTagOpen,
    RCDATALessThanSign,
    ScriptData,
    ScriptDataDoubleEscaped,
    ScriptDataDoubleEscapedDash,
    ScriptDataDoubleEscapedDashDash,
    ScriptDataDoubleEscapedLessThan,
    ScriptDataDoubleEscapeEnd,
    ScriptDataDoubleEscapeStart,
    ScriptDataEndTagName,
    ScriptDataEndTagOpen,
    ScriptDataEscaped,
    ScriptDataEscapedDash,
    ScriptDataEscapedDashDash,
    ScriptDataEscapedEndTagName,
    ScriptDataEscapedEndTagOpen,
    ScriptDataEscapedLessThanSign,
    ScriptDataEscapeStart,
    ScriptDataEscapeStartDash,
    ScriptDataLessThanSign,
    SelfClosingStartTag,
    TagName,
    TagOpen,
}

// https://html.spec.whatwg.org/#tokenization
class HTMLTokenizer(StreamReader input)
{
    private const uint END_OF_FILE = 0xFFFFFFFF;

    private State State = State.Data;
 
    private State? ReturnState = null;

    private Queue<HTMLToken> Tokens = new();

    private HTMLToken CurrentToken = null;

    private StreamReader Input = input;

    private uint CurrentCharacter = '\0';

    private bool ShouldReconsume = false;

    private StringBuilder TempBuffer = new();

    public HTMLToken? NextToken()
    {
        while (true)
        {
            if (!ShouldReconsume)
            {
                ConsumeNextInputCharacter();
            }

            switch (State)
            {
                // https://html.spec.whatwg.org/multipage/parsing.html#data-state
                case State.Data:
                {
                    if (CurrentCharacter.Is(CodePoint.Ampersand))
                    {
                        ReturnState = State.Data;
                        SwitchTo(State.CharacterReference);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.TagOpen);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    else if (IsEOF())
                    {
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }

                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#rcdata-state
                case State.RCDATA:
                {
                    if (CurrentCharacter.Is(CodePoint.Ampersand))
                    {
                        ReturnState = State.RCDATA;
                        SwitchTo(State.CharacterReference);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.RCDATALessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        EmitReplacementCharacterToken();
                    }
                    else if (IsEOF())
                    {
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }

                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#rawtext-state
                case State.RAWTEXT:
                {
                    if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.RAWTEXTLessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        EmitReplacementCharacterToken();
                    }
                    else if (IsEOF())
                    {
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }

                    break; 
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-state
                case State.ScriptData:
                {
                    if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.ScriptDataLessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        EmitReplacementCharacterToken();
                    }
                    else if (IsEOF())
                    {
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }

                    break; 
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#plaintext-state
                case State.PLAINTEXT:
                {
                    if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        EmitReplacementCharacterToken();
                    }
                    else if (IsEOF())
                    {
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }

                    break; 
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#tag-open-state
                case State.TagOpen:
                {
                    if (CurrentCharacter.Is(CodePoint.ExclamationMark))
                    {
                        SwitchTo(State.MarkupDeclarationOpen);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        SwitchTo(State.EndTagOpen);
                    }
                    else if (CurrentCharacter.IsASCIIAlpha())
                    {
                        CreateNewStartTagToken(string.Empty);
                        ReconsumeIn(State.TagName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.QuestionMark))
                    {
                        // This is an unexpected-question-mark-instead-of-tag-name parse error.
                        CreateNewCommentToken(string.Empty);
                        ReconsumeIn(State.BogusComment);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-before-tag-name parse error. 
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is an invalid-first-character-of-tag-name parse error.
                        EmitCharacterToken(CodePoint.LessThanSign);
                        ReconsumeIn(State.Data);
                    }

                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#end-tag-open-state
                case State.EndTagOpen:
                {
                    if (CurrentCharacter.IsASCIIAlpha())
                    {
                        CreateNewEndTagToken(string.Empty);
                        ReconsumeIn(State.TagName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is a missing-end-tag-name parse error.
                        SwitchTo(State.Data);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-before-tag-name parse error.
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is an invalid-first-character-of-tag-name parse error.
                        CreateNewCommentToken(string.Empty);
                        ReconsumeIn(State.BogusComment);
                    }

                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#tag-name-state
                case State.TagName:
                {
                    if (IsWhiteSpace())
                    {
                        SwitchTo(State.BeforeAttributeName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        SwitchTo(State.SelfClosingStartTag);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentToken();
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        var token = GetCurrentTokenAsTagToken();
                        char c = (char)CurrentCharacter;
                        token.AppendToName(char.ToLower(c));
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        var token = GetCurrentTokenAsTagToken();
                        token.AppendToName((char)CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-tag parse error.
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var token = GetCurrentTokenAsTagToken();
                        token.AppendToName((char)CurrentCharacter);
                    }

                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#rcdata-less-than-sign-state
                case State.RCDATALessThanSign:
                {
                    if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        TempBuffer = new(string.Empty);
                        SwitchTo(State.RCDATAEndTagOpen);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        ReconsumeIn(State.RCDATA);
                    }

                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#rcdata-end-tag-open-state
                case State.RCDATAEndTagOpen:
                {
                    if (CurrentCharacter.IsASCIIAlpha())
                    {
                        CreateNewEndTagToken(string.Empty);
                        ReconsumeIn(State.RCDATAEndTagName);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        ReconsumeIn(State.RCDATA);
                    }

                    break;
                }
            }

        }

        return null;
    }

    private void SwitchTo(State state)
    {
        State = state;
        ShouldReconsume = false;
    }

    private void ReconsumeIn(State state)
    {
        ShouldReconsume = true;
        SwitchTo(state);
    }

    private void CreateNewToken(HTMLToken token)
    {
        CurrentToken = token;
    }

    private void CreateNewStartTagToken(string name)
    {
        CreateNewToken(new TagToken(TagTokenType.Start, name));
    }

    private void CreateNewEndTagToken(string name)
    {
        CreateNewToken(new TagToken(TagTokenType.End, name));
    }

    private void CreateNewCommentToken(string data)
    {
        CreateNewToken(new CommentToken(data));
    }

    private void EmitCurrentCharacterAsCharacterToken()
    {
        Tokens.Enqueue(new CharacterToken((char)CurrentCharacter));
    }

    private void EmitReplacementCharacterToken()
    {
        Tokens.Enqueue(new CharacterToken((char)CodePoint.ReplacementCharacter));
    }

    private void EmitCurrentToken()
    {
        Tokens.Enqueue(CurrentToken);
    }
    
    private void EmitCommentToken(string data)
    {
        Tokens.Enqueue(new CommentToken(data));
    }

    private void EmitCharacterToken(CodePoint codePoint)
    {
        Tokens.Enqueue(new CharacterToken((char)codePoint));
    }

    private void EmitEndOfFileToken()
    {
        Tokens.Enqueue(new EndOfFileToken());
    }

    private TagToken GetCurrentTokenAsTagToken()
    {
        if (CurrentToken is not TagToken)
        {
            throw new InvalidOperationException("Current token is not a tag token.");
        }
        return (TagToken)CurrentToken;
    }

    private bool IsEOF()
    {
        return CurrentCharacter == END_OF_FILE;
    }

    private bool IsWhiteSpace()
    {
        return CurrentCharacter.IsOneOf(CodePoint.Tab, CodePoint.LineFeed, CodePoint.FormFeed, CodePoint.Space);
    }

    private void ConsumeNextInputCharacter()
    {
        // TODO: https://html.spec.whatwg.org/#preprocessing-the-input-stream
        var next = Input.Read();
        CurrentCharacter = next == -1
            ? END_OF_FILE
            : (uint)next;
    }    
}