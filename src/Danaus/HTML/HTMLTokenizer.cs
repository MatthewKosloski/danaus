using System.Diagnostics;
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
    ScriptDataDoubleEscapedLessThanSign,
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

    private Queue<int> Buffer = [];

    // A reference to the last start tag token that was emitted.
    private TagToken? LastStartTagToken = null;

    private string? CurrentAttributeName = null;

    private uint CharacterReferenceCode = 0;

    public HTMLToken? NextToken()
    {
        while (true)
        {
            switch (State)
            {
                // https://html.spec.whatwg.org/multipage/parsing.html#data-state
                case State.Data:
                {
                    ConsumeNextInputCharacter();

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
                    ConsumeNextInputCharacter();

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
                    ConsumeNextInputCharacter();

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
                    ConsumeNextInputCharacter();

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
                    ConsumeNextInputCharacter();

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
                    ConsumeNextInputCharacter();

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
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIAlpha())
                    {
                        CreateNewEndTagToken();
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
                    ConsumeNextInputCharacter();

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
                        EmitCurrentTagToken();
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        AppendToCurrentTagTokenName(char.ToLower((char)CurrentCharacter));
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        AppendToCurrentTagTokenName(CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-tag parse error.
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        AppendToCurrentTagTokenName((char)CurrentCharacter);
                    }

                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#rcdata-less-than-sign-state
                case State.RCDATALessThanSign:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        TempBuffer.Clear();
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
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIAlpha())
                    {
                        CreateNewEndTagToken();
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
                // https://html.spec.whatwg.org/multipage/parsing.html#rcdata-end-tag-name-state
                case State.RCDATAEndTagName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.BeforeAttributeName);
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.RCDATA);   
                        }
                    }
                    else if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.SelfClosingStartTag);
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.RCDATA);   
                        }
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.Data);
                            EmitCurrentTagToken();
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.RCDATA);   
                        }
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        AppendToCurrentTagTokenName(char.ToLower((char)CurrentCharacter));
                        TempBuffer.Append(CurrentCharacter);
                    }
                    else if (CurrentCharacter.IsASCIILowerAlpha())
                    {
                        AppendToCurrentTagTokenName((char)CurrentCharacter);
                        TempBuffer.Append(CurrentCharacter); 
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        EmitTempBufferTokens();
                        ReconsumeIn(State.RCDATA);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#rawtext-less-than-sign-state
                case State.RAWTEXTLessThanSign:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        TempBuffer.Clear();
                        SwitchTo(State.EndTagOpen);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        ReconsumeIn(State.RAWTEXT);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#rawtext-end-tag-open-state
                case State.RAWTEXTEndTagOpen:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIAlpha())
                    {
                        CreateNewEndTagToken();
                        ReconsumeIn(State.RAWTEXTEndTagName);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        ReconsumeIn(State.RAWTEXT);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#rawtext-end-tag-name-state
                case State.RAWTEXTEndTagName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.BeforeAttributeName);
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.RAWTEXT);  
                        }
                    }
                    else if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.SelfClosingStartTag);
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.RAWTEXT);   
                        }
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.Data);
                            EmitCurrentTagToken();
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.RAWTEXT);   
                        }
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        AppendToCurrentTagTokenName(char.ToLower((char)CurrentCharacter));
                        TempBuffer.Append(CurrentCharacter);
                    }
                    else if (CurrentCharacter.IsASCIILowerAlpha())
                    {
                        AppendToCurrentTagTokenName((char)CurrentCharacter);
                        TempBuffer.Append(CurrentCharacter); 
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        EmitTempBufferTokens();
                        ReconsumeIn(State.RAWTEXT);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-less-than-sign-state
                case State.ScriptDataLessThanSign:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        TempBuffer.Clear();
                        SwitchTo(State.ScriptDataEndTagOpen);
                    }
                    else if (CurrentCharacter.Is(CodePoint.ExclamationMark))
                    {
                        SwitchTo(State.ScriptDataEscapeStart);
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.ExclamationMark);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        ReconsumeIn(State.ScriptData);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-end-tag-open-state
                case State.ScriptDataEndTagOpen:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIAlpha())
                    {
                        CreateNewEndTagToken();
                        ReconsumeIn(State.ScriptDataEndTagName);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        ReconsumeIn(State.ScriptData);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-end-tag-name-state
                case State.ScriptDataEndTagName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.BeforeAttributeName);
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.ScriptData);  
                        }
                    }
                    else if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.SelfClosingStartTag);
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.ScriptData);   
                        }
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.Data);
                            EmitCurrentTagToken();
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.ScriptData);   
                        }
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        AppendToCurrentTagTokenName(char.ToLower((char)CurrentCharacter));
                        TempBuffer.Append(CurrentCharacter);
                    }
                    else if (CurrentCharacter.IsASCIILowerAlpha())
                    {
                        AppendToCurrentTagTokenName((char)CurrentCharacter);
                        TempBuffer.Append(CurrentCharacter); 
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        EmitTempBufferTokens();
                        ReconsumeIn(State.ScriptData);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escape-start-state
                case State.ScriptDataEscapeStart:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.ScriptDataEscapeStart);
                        EmitCharacterToken(CodePoint.HyphenMinus);
                    }
                    else
                    {
                        ReconsumeIn(State.ScriptData);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escape-start-dash-state
                case State.ScriptDataEscapeStartDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.ScriptDataEscapedDashDash);
                        EmitCharacterToken(CodePoint.HyphenMinus);
                    }
                    else
                    {
                        ReconsumeIn(State.ScriptData);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escaped-state
                case State.ScriptDataEscaped:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.ScriptDataEscapedDash);
                        EmitCharacterToken(CodePoint.HyphenMinus);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.ScriptDataEscapedLessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        EmitCharacterToken(CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-script-html-comment-like-text parse error.
                        EmitEndOfFileToken();   
                    }
                    else
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escaped-dash-state
                case State.ScriptDataEscapedDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.ScriptDataEscapedDashDash);
                        EmitCharacterToken(CodePoint.HyphenMinus);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.ScriptDataEscapedLessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        SwitchTo(State.ScriptDataEscaped);
                        EmitCharacterToken(CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-script-html-comment-like-text parse error.
                        EmitEndOfFileToken();   
                    }
                    else
                    {
                        SwitchTo(State.ScriptDataEscaped);
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escaped-dash-dash-state
                case State.ScriptDataEscapedDashDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        EmitCharacterToken(CodePoint.HyphenMinus);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.ScriptDataEscapedLessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.ScriptData);
                        EmitCharacterToken(CodePoint.GreaterThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        SwitchTo(State.ScriptDataEscaped);
                        EmitCharacterToken(CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-script-html-comment-like-text parse error.
                        EmitEndOfFileToken();   
                    }
                    else
                    {
                        SwitchTo(State.ScriptDataEscaped);
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escaped-less-than-sign-state
                case State.ScriptDataEscapedLessThanSign:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        TempBuffer.Clear();
                        SwitchTo(State.ScriptDataEscapedEndTagOpen);
                    }
                    else if (CurrentCharacter.IsASCIIAlpha())
                    {
                        TempBuffer.Clear();
                        EmitCharacterToken(CodePoint.LessThanSign);
                        ReconsumeIn(State.ScriptDataDoubleEscapeStart);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        ReconsumeIn(State.ScriptDataEscaped);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escaped-end-tag-open-state
                case State.ScriptDataEscapedEndTagOpen:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIAlpha())
                    {
                        CreateNewEndTagToken();
                        ReconsumeIn(State.ScriptDataEscapedEndTagName);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        ReconsumeIn(State.ScriptDataEscaped);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escaped-end-tag-name-state
                case State.ScriptDataEscapedEndTagName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.BeforeAttributeName);
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.ScriptDataEscaped);  
                        }
                    }
                    else if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.SelfClosingStartTag);
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.ScriptDataEscaped);
                        }
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        if (IsCurrentTokenAnAppropriateEndTagToken())
                        {
                            SwitchTo(State.Data);
                            EmitCurrentTagToken();
                        }
                        else
                        {
                            EmitCharacterToken(CodePoint.LessThanSign);
                            EmitCharacterToken(CodePoint.Solidus);
                            EmitTempBufferTokens();
                            ReconsumeIn(State.ScriptDataEscaped);
                        }
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        AppendToCurrentTagTokenName(char.ToLower((char)CurrentCharacter));
                        TempBuffer.Append(CurrentCharacter);
                    }
                    else if (CurrentCharacter.IsASCIILowerAlpha())
                    {
                        AppendToCurrentTagTokenName((char)CurrentCharacter);
                        TempBuffer.Append(CurrentCharacter); 
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.LessThanSign);
                        EmitCharacterToken(CodePoint.Solidus);
                        EmitTempBufferTokens();
                        ReconsumeIn(State.ScriptDataEscaped);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-double-escape-start-state
                case State.ScriptDataDoubleEscapeStart:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        if (TempBuffer.Equals("script"))
                        {
                            SwitchTo(State.ScriptDataDoubleEscaped);
                        }
                        else
                        {
                            SwitchTo(State.ScriptDataEscaped);
                        }
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        TempBuffer.Append(char.ToLower((char)CurrentCharacter));
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    else if (CurrentCharacter.IsASCIILowerAlpha())
                    {
                        TempBuffer.Append(CurrentCharacter);
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    else
                    {
                        ReconsumeIn(State.ScriptDataEscaped);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-double-escaped-state
                case State.ScriptDataDoubleEscaped:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.ScriptDataDoubleEscapedDash);
                        EmitCharacterToken(CodePoint.HyphenMinus);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.ScriptDataDoubleEscapedLessThanSign);
                        EmitCharacterToken(CodePoint.LessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        EmitCharacterToken(CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-script-html-comment-like-text parse error.
                        EmitEndOfFileToken();   
                    }
                    else
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-double-escaped-dash-state
                case State.ScriptDataDoubleEscapedDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.ScriptDataDoubleEscapedDashDash);
                        EmitCharacterToken(CodePoint.HyphenMinus);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.ScriptDataDoubleEscapedLessThanSign);
                        EmitCharacterToken(CodePoint.LessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        SwitchTo(State.ScriptDataDoubleEscaped);
                        EmitCharacterToken(CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-script-html-comment-like-text parse error.
                        EmitEndOfFileToken();   
                    }
                    else
                    {
                        SwitchTo(State.ScriptDataDoubleEscaped);
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-double-escaped-dash-dash-state
                case State.ScriptDataDoubleEscapedDashDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        EmitCharacterToken(CodePoint.HyphenMinus);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        SwitchTo(State.ScriptDataDoubleEscapedLessThanSign);
                        EmitCharacterToken(CodePoint.LessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.ScriptData);
                        EmitCharacterToken(CodePoint.GreaterThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        SwitchTo(State.ScriptDataDoubleEscaped);
                        EmitCharacterToken(CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-script-html-comment-like-text parse error.
                        EmitEndOfFileToken();   
                    }
                    else
                    {
                        SwitchTo(State.ScriptDataDoubleEscaped);
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-double-escaped-less-than-sign-state
                case State.ScriptDataDoubleEscapedLessThanSign:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        TempBuffer.Clear();
                        SwitchTo(State.ScriptDataDoubleEscapeEnd);
                        EmitCharacterToken(CodePoint.Solidus);
                    }
                    else
                    {
                        ReconsumeIn(State.ScriptDataDoubleEscaped);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#script-data-double-escape-end-state
                case State.ScriptDataDoubleEscapeEnd:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        if (TempBuffer.Equals("script"))
                        {
                            SwitchTo(State.ScriptDataEscaped);
                        }
                        else
                        {
                            SwitchTo(State.ScriptDataDoubleEscaped);
                        }
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        TempBuffer.Append(char.ToLower((char)CurrentCharacter));
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    else if (CurrentCharacter.IsASCIILowerAlpha())
                    {
                        TempBuffer.Append(CurrentCharacter);
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    else
                    {
                        ReconsumeIn(State.ScriptDataDoubleEscaped);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-name-state
                case State.BeforeAttributeName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.IsOneOf(CodePoint.Solidus, CodePoint.GreaterThanSign) || IsEOF())
                    {
                        ReconsumeIn(State.AfterAttributeName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.EqualsSign))
                    {
                        // This is an unexpected-equals-sign-before-attribute-name parse error.
                        StartNewAttributeInCurrentTagToken(((char)CurrentCharacter).ToString(), string.Empty);
                        SwitchTo(State.AttributeName);
                    }
                    else
                    {
                        StartNewAttributeInCurrentTagToken();
                        ReconsumeIn(State.AttributeName);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
                case State.AttributeName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace() || CurrentCharacter.IsOneOf(CodePoint.Solidus, CodePoint.GreaterThanSign) || IsEOF())
                    {
                        ReconsumeIn(State.AfterAttributeName);
                    }
                    else if (CurrentCharacter.IsOneOf(CodePoint.EqualsSign))
                    {
                        SwitchTo(State.BeforeAttributeValue);
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        AppendCharacterToCurrentAttributeNameOrFail(char.ToLower((char)CurrentCharacter));
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error. 
                        AppendCharacterToCurrentAttributeNameOrFail((char)CodePoint.ReplacementCharacter);
                    }
                    else if (CurrentCharacter.IsOneOf(CodePoint.QuotationMark, CodePoint.Apostrophe, CodePoint.LessThanSign))
                    {
                        // This is an unexpected-character-in-attribute-name parse error.
                        AppendCharacterToCurrentAttributeNameOrFail((char)CurrentCharacter);
                    }
                    else
                    {
                        AppendCharacterToCurrentAttributeNameOrFail((char)CurrentCharacter);
                    }

                    /*
                    * TODO: When the user agent leaves the attribute name state (and before emitting the tag token,
                    * if appropriate), the complete attribute's name must be compared to the other attributes on the
                    * same token; if there is already an attribute on the token with the exact same name, then this
                    * is a duplicate-attribute parse error and the new attribute must be removed from the token.
                    */
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-name-state
                case State.AfterAttributeName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.Is(CodePoint.Solidus))
                    {
                        SwitchTo(State.SelfClosingStartTag);
                    }
                    else if (CurrentCharacter.Is(CodePoint.EqualsSign))
                    {
                        SwitchTo(State.BeforeAttributeValue);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentTagToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-tag parse error.
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        StartNewAttributeInCurrentTagToken();
                        ReconsumeIn(State.AttributeName);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-value-state
                case State.BeforeAttributeValue:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        SwitchTo(State.AttributeValueDoubleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        SwitchTo(State.AttributeValueSingleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is a missing-attribute-value parse error.
                        SwitchTo(State.Data);
                        EmitCurrentTagToken();
                    }
                    else
                    {
                        ReconsumeIn(State.AttributeValueUnquoted);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(double-quoted)-state
                case State.AttributeValueDoubleQuoted:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        SwitchTo(State.AfterAttributeValueQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Ampersand))
                    {
                        ReturnState = State.AttributeValueDoubleQuoted;
                        SwitchTo(State.CharacterReference);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        AppendCharacterToCurrentAttributeValueOrFail((char)CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-tag parse error.
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        AppendCharacterToCurrentAttributeValueOrFail((char)CurrentCharacter);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(single-quoted)-state
                case State.AttributeValueSingleQuoted:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        SwitchTo(State.AfterAttributeValueQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Ampersand))
                    {
                        ReturnState = State.AttributeValueSingleQuoted;
                        SwitchTo(State.CharacterReference);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        AppendCharacterToCurrentAttributeValueOrFail((char)CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-tag parse error.
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        AppendCharacterToCurrentAttributeValueOrFail((char)CurrentCharacter);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(unquoted)-state
                case State.AttributeValueUnquoted:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        SwitchTo(State.BeforeAttributeName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Ampersand))
                    {
                        ReturnState = State.AttributeValueUnquoted;
                        SwitchTo(State.CharacterReference);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentTagToken();
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        AppendCharacterToCurrentAttributeValueOrFail((char)CodePoint.ReplacementCharacter);
                    }
                    else if (CurrentCharacter.IsOneOf(
                        CodePoint.QuotationMark,
                        CodePoint.Apostrophe,
                        CodePoint.LessThanSign,
                        CodePoint.EqualsSign,
                        CodePoint.GraveAccent
                    ))
                    {
                        // This is an unexpected-character-in-unquoted-attribute-value parse error.
                        AppendCharacterToCurrentAttributeValueOrFail((char)CurrentCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-tag parse error.
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        AppendCharacterToCurrentAttributeValueOrFail((char)CurrentCharacter);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-value-(quoted)-state
                case State.AfterAttributeValueQuoted:
                {
                    ConsumeNextInputCharacter();

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
                        EmitCurrentTagToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-tag parse error.
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is a missing-whitespace-between-attributes parse error.
                        ReconsumeIn(State.BeforeAttributeName);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#self-closing-start-tag-state
                case State.SelfClosingStartTag:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        var currentTagToken = GetCurrentTagTokenOrFail();
                        currentTagToken.IsSelfClosing = true;
                        SwitchTo(State.Data);
                        EmitCurrentTagToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-tag parse error.
                        EmitEndOfFileToken();   
                    }
                    else
                    {
                        // This is an unexpected-solidus-in-tag parse error.
                        ReconsumeIn(State.BeforeAttributeName);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#bogus-comment-state
                case State.BogusComment:
                {
                    ConsumeNextInputCharacter();
                    
                    if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentCommentToken();
                    }
                    else if (IsEOF())
                    {
                        EmitCurrentCommentToken();
                        EmitEndOfFileToken();
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CodePoint.ReplacementCharacter); 
                    }
                    else
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CurrentCharacter); 
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#markup-declaration-open-state
                case State.MarkupDeclarationOpen:
                {
                    if (Match("--"))
                    {
                        CreateNewCommentToken();
                        SwitchTo(State.CommentStart);
                    }
                    else if (Match("DOCTYPE", false))
                    {
                        SwitchTo(State.DOCTYPE);
                    }
                    else if (Match("[CDATA["))
                    {
                        // TODO: Handle adjusted current node case.
                        // This is a cdata-in-html-content parse error.
                        CreateNewCommentToken("[CDATA[");
                        SwitchTo(State.BogusComment);
                    }
                    else
                    {
                        // This is an incorrectly-opened-comment parse error.
                        CreateNewCommentToken();
                        SwitchTo(State.BogusComment);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-state
                case State.CommentStart:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.CommentStartDash);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is an abrupt-closing-of-empty-comment parse error.
                        SwitchTo(State.Data);
                        EmitCurrentCommentToken();
                    }
                    else
                    {
                        ReconsumeIn(State.Comment);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-dash-state
                case State.CommentStartDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.CommentEnd);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is an abrupt-closing-of-empty-comment parse error.
                        SwitchTo(State.Data);
                        EmitCurrentCommentToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-comment parse error.
                        EmitCurrentCommentToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CodePoint.HyphenMinus);
                        ReconsumeIn(State.Comment);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-state
                case State.Comment:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CurrentCharacter);
                        SwitchTo(State.CommentLessThanSign);
                    }
                    else if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.CommentEndDash);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-comment parse error.
                        EmitCurrentCommentToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CurrentCharacter);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-state
                case State.CommentLessThanSign:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.ExclamationMark))
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CurrentCharacter);
                        SwitchTo(State.CommentLessThanSignBang);
                    }
                    else if (CurrentCharacter.Is(CodePoint.LessThanSign))
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CurrentCharacter);
                    }
                    else
                    {
                        ReconsumeIn(State.Comment);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-state
                case State.CommentLessThanSignBang:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.CommentLessThanSignBangDash);
                    }
                    else
                    {
                        ReconsumeIn(State.Comment);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-dash-state
                case State.CommentLessThanSignBangDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        SwitchTo(State.CommentLessThanSignBangDashDash);
                    }
                    else
                    {
                        ReconsumeIn(State.CommentEndDash);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-dash-dash-state
                case State.CommentLessThanSignBangDashDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.GreaterThanSign) || IsEOF())
                    {
                        ReconsumeIn(State.CommentEnd);
                    }
                    else
                    {
                        // This is a nested-comment parse error.
                        ReconsumeIn(State.CommentEnd);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-dash-state
                case State.CommentEndDash:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        ReconsumeIn(State.CommentEnd);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-comment parse error.
                        EmitCurrentCommentToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CodePoint.HyphenMinus);
                        ReconsumeIn(State.Comment);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-state
                case State.CommentEnd:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentCommentToken();
                    }
                    else if (CurrentCharacter.Is(CodePoint.ExclamationMark))
                    {
                        SwitchTo(State.CommentEndBang);
                    }
                    else if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CodePoint.HyphenMinus);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-comment parse error.
                        EmitCurrentCommentToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CodePoint.HyphenMinus);
                        ReconsumeIn(State.Comment);
                    }
                    break;  
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-bang-state
                case State.CommentEndBang:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.HyphenMinus))
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CodePoint.HyphenMinus);
                        currentCommentToken.AppendToData(CodePoint.HyphenMinus);
                        currentCommentToken.AppendToData(CodePoint.ExclamationMark);
                        SwitchTo(State.CommentEndDash);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is an incorrectly-closed-comment parse error.
                        SwitchTo(State.Data);
                        EmitCurrentCommentToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-comment parse error.
                        EmitCurrentCommentToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentCommentToken = GetCurrentCommentTokenOrFail();
                        currentCommentToken.AppendToData(CodePoint.HyphenMinus);
                        currentCommentToken.AppendToData(CodePoint.HyphenMinus);
                        currentCommentToken.AppendToData(CodePoint.ExclamationMark);
                        SwitchTo(State.CommentEndDash);
                        ReconsumeIn(State.Comment);
                    }
                    break;      
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#doctype-state
                case State.DOCTYPE:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        SwitchTo(State.BeforeDOCTYPEName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        ReconsumeIn(State.BeforeDOCTYPEName);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        CreateNewDocTypeToken(null, true);
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is a missing-whitespace-before-doctype-name parse error.
                        ReconsumeIn(State.BeforeDOCTYPEName);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-name-state
                case State.BeforeDOCTYPEName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        CreateNewDocTypeToken(char.ToString(char.ToLower((char)CurrentCharacter)));
                        SwitchTo(State.DOCTYPEName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        CreateNewDocTypeToken(char.ToString((char)CodePoint.ReplacementCharacter));
                        SwitchTo(State.DOCTYPEName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is a missing-doctype-name parse error.
                        CreateNewDocTypeToken(null, true);
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        CreateNewDocTypeToken(null, true);
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        CreateNewDocTypeToken(char.ToString((char)CurrentCharacter));
                        SwitchTo(State.DOCTYPEName);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#doctype-name-state
                case State.DOCTYPEName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        SwitchTo(State.AfterDOCTYPEName);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (CurrentCharacter.IsASCIIUpperAlpha())
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.Name += char.ToLower((char)CurrentCharacter);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.Name += char.ToLower((char)CodePoint.ReplacementCharacter);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.Name += (char)CurrentCharacter;
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-name-state
                case State.AfterDOCTYPEName:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else if (Match("PUBLIC", false))
                    {
                        SwitchTo(State.AfterDOCTYPEPublicKeyword);
                    }
                    else if (Match("SYSTEM", false))
                    {
                        SwitchTo(State.AfterDOCTYPESystemKeyword);
                    }
                    else
                    {
                        // This is an invalid-character-sequence-after-doctype-name parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        ReconsumeIn(State.BogusDOCTYPE);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-public-keyword-state
                case State.AfterDOCTYPEPublicKeyword:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        SwitchTo(State.BeforeDOCTYPEPublicIdentifier);
                    }
                    else if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        // This is a missing-whitespace-after-doctype-public-keyword parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.PublicIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPEPublicIdentifierDoubleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        // This is a missing-whitespace-after-doctype-public-keyword parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.PublicIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPEPublicIdentifierSingleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is a missing-doctype-public-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is a missing-quote-before-doctype-public-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        ReconsumeIn(State.BogusDOCTYPE);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-public-identifier-state
                case State.BeforeDOCTYPEPublicIdentifier:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.PublicIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPEPublicIdentifierDoubleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.PublicIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPEPublicIdentifierSingleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is a missing-doctype-public-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is a missing-quote-before-doctype-public-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        ReconsumeIn(State.BogusDOCTYPE);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#doctype-public-identifier-(double-quoted)-state
                case State.DOCTYPEPublicIdentifierDoubleQuoted:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        SwitchTo(State.AfterDOCTYPEPublicIdentifier);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.PublicIdentifier += (char)CodePoint.ReplacementCharacter;
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is an abrupt-doctype-public-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.PublicIdentifier += (char)CurrentCharacter;
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#doctype-public-identifier-(single-quoted)-state
                case State.DOCTYPEPublicIdentifierSingleQuoted:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        SwitchTo(State.AfterDOCTYPEPublicIdentifier);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.PublicIdentifier += (char)CodePoint.ReplacementCharacter;
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is an abrupt-doctype-public-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.PublicIdentifier += (char)CurrentCharacter;
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-public-identifier-state
                case State.AfterDOCTYPEPublicIdentifier:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        SwitchTo(State.BetweenDOCTYPEPublicAndSystemIdentifiers);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        // This is a missing-whitespace-between-doctype-public-and-system-identifiers parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPESystemIdentifierDoubleQuoted);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is a missing-quote-before-doctype-system-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        ReconsumeIn(State.BogusDOCTYPE);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#between-doctype-public-and-system-identifiers-state
                case State.BetweenDOCTYPEPublicAndSystemIdentifiers:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPESystemIdentifierDoubleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPESystemIdentifierSingleQuoted);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is a missing-quote-before-doctype-system-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        ReconsumeIn(State.BogusDOCTYPE);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-system-keyword-state
                case State.AfterDOCTYPESystemKeyword:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        SwitchTo(State.BeforeDOCTYPESystemIdentifier);
                    }
                    else if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        // This is a missing-whitespace-after-doctype-system-keyword parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPESystemIdentifierDoubleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        // This is a missing-whitespace-after-doctype-system-keyword parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPESystemIdentifierSingleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is a missing-doctype-system-identifier parse error. 
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is a missing-quote-before-doctype-system-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        ReconsumeIn(State.BogusDOCTYPE);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-system-identifier-state
                case State.BeforeDOCTYPESystemIdentifier:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPESystemIdentifierDoubleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier = string.Empty;
                        SwitchTo(State.DOCTYPESystemIdentifierSingleQuoted);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is a missing-doctype-system-identifier parse error. 
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is a missing-quote-before-doctype-system-identifier parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        ReconsumeIn(State.BogusDOCTYPE);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#doctype-system-identifier-(double-quoted)-state
                case State.DOCTYPESystemIdentifierDoubleQuoted:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.QuotationMark))
                    {
                        SwitchTo(State.AfterDOCTYPESystemIdentifier);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier += (char)CodePoint.ReplacementCharacter;
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is an abrupt-doctype-system-identifier parse error
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier += (char)CurrentCharacter;
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#doctype-system-identifier-(single-quoted)-state
                case State.DOCTYPESystemIdentifierSingleQuoted:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.Apostrophe))
                    {
                        SwitchTo(State.AfterDOCTYPESystemIdentifier);
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier += (char)CodePoint.ReplacementCharacter;
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        // This is an abrupt-doctype-system-identifier parse error
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.SystemIdentifier += (char)CurrentCharacter;
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-system-identifier-state
                case State.AfterDOCTYPESystemIdentifier:
                {
                    ConsumeNextInputCharacter();

                    if (IsWhiteSpace())
                    {
                        // Ignore the character.
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-doctype parse error.
                        var currentDoctypeToken = GetCurrentDocTypeTokenOrFail();
                        currentDoctypeToken.ForceQuirks = true;
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // This is an unexpected-character-after-doctype-system-identifier parse error.
                        ReconsumeIn(State.BogusDOCTYPE);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#bogus-doctype-state
                case State.BogusDOCTYPE:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                        EmitCurrentDocTypeToken();
                    }
                    else if (CurrentCharacter.Is(CodePoint.NullCharacter))
                    {
                        // This is an unexpected-null-character parse error.
                        // Ignore the character.
                    }
                    else if (IsEOF())
                    {
                        EmitCurrentDocTypeToken();
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        // Ignore the character.
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#cdata-section-state
                case State.CDATASection:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.RightSquareBracket))
                    {
                        SwitchTo(State.CDATASectionBracket);
                    }
                    else if (IsEOF())
                    {
                        // This is an eof-in-cdata parse error.
                        EmitEndOfFileToken();
                    }
                    else
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#cdata-section-bracket-state
                case State.CDATASectionBracket:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.RightSquareBracket))
                    {
                        SwitchTo(State.CDATASectionEnd);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.RightSquareBracket);
                        ReconsumeIn(State.CDATASection);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#cdata-section-end-state
                case State.CDATASectionEnd:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.Is(CodePoint.RightSquareBracket))
                    {
                        EmitCharacterToken(CodePoint.RightSquareBracket);
                    }
                    else if (CurrentCharacter.Is(CodePoint.GreaterThanSign))
                    {
                        SwitchTo(State.Data);
                    }
                    else
                    {
                        EmitCharacterToken(CodePoint.RightSquareBracket);
                        EmitCharacterToken(CodePoint.RightSquareBracket);
                        ReconsumeIn(State.CDATASection);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#character-reference-state
                case State.CharacterReference:
                {
                    TempBuffer.Clear();
                    TempBuffer.Append((char)CodePoint.Ampersand);

                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIAlphaNumeric())
                    {
                        ReconsumeIn(State.NamedCharacterReference);
                    }
                    else if (CurrentCharacter.Is(CodePoint.Number))
                    {
                        TempBuffer.Append((char)CurrentCharacter);
                        SwitchTo(State.NumericCharacterReference);
                    }
                    else
                    {
                        FlushCodePointsConsumedAsACharacterReference();
                        ReconsumeInReturnState();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#named-character-reference-state
                case State.NamedCharacterReference:
                {
                    // TODO
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#ambiguous-ampersand-state
                case State.AmbiguousAmpersand:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIAlphaNumeric() && IsCharRefConsumedAsPartOfAttribute())
                    {
                        AppendCharacterToCurrentAttributeValueOrFail((char)CurrentCharacter);
                    }
                    else if (CurrentCharacter.IsASCIIAlphaNumeric())
                    {
                        EmitCurrentCharacterAsCharacterToken();
                    }
                    else if (CurrentCharacter.Is(CodePoint.Semicolon))
                    {
                        // This is an unknown-named-character-reference parse error.
                        ReconsumeInReturnState();
                    }
                    else
                    {
                        ReconsumeInReturnState();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#numeric-character-reference-state
                case State.NumericCharacterReference:
                {
                    CharacterReferenceCode = 0;

                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsOneOf(CodePoint.LowercaseX, CodePoint.UppercaseX))
                    {
                        TempBuffer.Append((char)CurrentCharacter);
                        SwitchTo(State.HexadecimalCharacterReferenceStart);
                    }
                    else
                    {
                        ReconsumeIn(State.DecimalCharacterReferenceStart);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#hexadecimal-character-reference-start-state
                case State.HexadecimalCharacterReferenceStart:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIHexDigit())
                    {
                        ReconsumeIn(State.HexadecimalCharacterReference);
                    }
                    else
                    {
                        // This is an absence-of-digits-in-numeric-character-reference parse error.
                        FlushCodePointsConsumedAsACharacterReference();
                        ReconsumeInReturnState();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#decimal-character-reference-start-state
                case State.DecimalCharacterReferenceStart:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIDigit())
                    {
                        ReconsumeIn(State.DecimalCharacterReference);
                    }
                    else
                    {
                        // This is an absence-of-digits-in-numeric-character-reference parse error.
                        FlushCodePointsConsumedAsACharacterReference();
                        ReconsumeInReturnState();
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#hexadecimal-character-reference-state
                case State.HexadecimalCharacterReference:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIDigit() || CurrentCharacter.IsASCIIHexDigit())
                    {
                        CharacterReferenceCode *= 16;
                        CharacterReferenceCode += CurrentCharacter;
                    }
                    else if (CurrentCharacter.Is(CodePoint.Semicolon))
                    {
                        SwitchTo(State.NumericCharacterReferenceEnd);
                    }
                    else
                    {
                        // This is a missing-semicolon-after-character-reference parse error.
                        ReconsumeIn(State.NumericCharacterReferenceEnd);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#decimal-character-reference-state
                case State.DecimalCharacterReference:
                {
                    ConsumeNextInputCharacter();

                    if (CurrentCharacter.IsASCIIDigit())
                    {
                        CharacterReferenceCode *= 10;
                        CharacterReferenceCode += CurrentCharacter;
                    }
                    else if (CurrentCharacter.Is(CodePoint.Semicolon))
                    {
                        SwitchTo(State.NumericCharacterReferenceEnd);
                    }
                    else
                    {
                        // This is a missing-semicolon-after-character-reference parse error.
                        ReconsumeIn(State.NumericCharacterReferenceEnd);
                    }
                    break;
                }
                // https://html.spec.whatwg.org/multipage/parsing.html#numeric-character-reference-end-state
                case State.NumericCharacterReferenceEnd:
                {
                    if (CharacterReferenceCode == 0x00)
                    {
                        // This is a null-character-reference parse error.
                        CharacterReferenceCode = 0xFFFD;
                    }
                    else if (CharacterReferenceCode > 0x10FFFF)
                    {
                        // This is a character-reference-outside-unicode-range parse error.
                        CharacterReferenceCode = 0xFFFD;
                    }
                    else if (CharacterReferenceCode.IsSurrogate())
                    {
                        // This is a surrogate-character-reference parse error.
                        CharacterReferenceCode = 0xFFFD;
                    }
                    else if (CharacterReferenceCode.IsNonCharacter())
                    {
                        // This is a noncharacter-character-reference parse error.
                        CharacterReferenceCode = 0xFFFD;
                    }
                    else if (CharacterReferenceCode == 0x0D || (CharacterReferenceCode.IsControl() && !IsCharacterReferenceCodeWhiteSpace()))
                    {
                        // This is a control-character-reference parse error.
                        CharacterReferenceCode = 0xFFFD;

                        var conversionTable = new Dictionary<uint, CodePoint>
                        {
                            {0x80, CodePoint.EuroSign},
                            {0x82, CodePoint.SingleLow9QuotationMark},
                            {0x83, CodePoint.LatinSmallLetterFWithHook},
                            {0x84, CodePoint.DoubleLow9QuotationMark},
                            {0x85, CodePoint.HorizontalEllipsis},
                            {0x86, CodePoint.Dagger},
                            {0x87, CodePoint.DoubleDagger},
                            {0x88, CodePoint.ModifiedLetterCircumflexAccent},
                            {0x89, CodePoint.PerMilleSign},
                            {0x8A, CodePoint.LatinCapitalLetterSWithCaron},
                            {0x8B, CodePoint.SingleLeftPointingAngleQuotationMark},
                            {0x8C, CodePoint.LatinCapitalLigatureOE},
                            {0x8E, CodePoint.LatinCapitalLetterZWithCaron},
                            {0x91, CodePoint.LeftSingleQuotationMark},
                            {0x92, CodePoint.RightSingleQuotationMark},
                            {0x93, CodePoint.LeftDoubleQuotationMark},
                            {0x94, CodePoint.RightDoubleQuotationMark},
                            {0x95, CodePoint.Bullet},
                            {0x96, CodePoint.EnDash},
                            {0x97, CodePoint.EmDash},
                            {0x98, CodePoint.SmallTilde},
                            {0x99, CodePoint.TradeMarkSign},
                            {0x9A, CodePoint.LatinSmallLetterSWithCaron},
                            {0x9B, CodePoint.SingleRightPointingAngleQuotationMark},
                            {0x9C, CodePoint.LatinSmallLigatureOE},
                            {0x9E, CodePoint.LatinSmallLetterZWithCaron},
                            {0x9F, CodePoint.LatinCapitalLetterYWithDiaeresis},
                        };

                        foreach (KeyValuePair<uint, CodePoint> kvp in conversionTable)
                        {
                            if (CharacterReferenceCode == kvp.Key)
                            {
                                CharacterReferenceCode = (uint)kvp.Value;
                                break;
                            }
                        }
                    }

                    TempBuffer.Clear();
                    TempBuffer.Append(CharacterReferenceCode);

                    FlushCodePointsConsumedAsACharacterReference();

                    if (ReturnState is not null)
                    {
                        SwitchTo((State)ReturnState);
                    }
                    break;
                }
                default:
                {
                    throw new UnreachableException($"Unhandled tokenizer state {State}");
                }
            }
        }
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

    private void ReconsumeInReturnState()
    {
        if (ReturnState is not null)
        {
            ReconsumeIn((State)ReturnState);
        }
    }

    private void CreateNewToken(HTMLToken token)
    {
        CurrentToken = token;
    }

    private void CreateNewStartTagToken(string name)
    {
        CreateNewToken(new TagToken(TagTokenType.Start, name));
    }

    private void CreateNewEndTagToken(string name = "")
    {
        CreateNewToken(new TagToken(TagTokenType.End, name));
    }

    private void CreateNewCommentToken(string data = "")
    {
        CreateNewToken(new CommentToken(data));
    }

    private void CreateNewDocTypeToken(string? name = null, bool forceQuirks = false, string? publicIdentifier = null, string? systemIdentifier = null)
    {
        CreateNewToken(new DocTypeToken(name, forceQuirks, publicIdentifier, systemIdentifier));
    }

    private void EmitCurrentCharacterAsCharacterToken()
    {
        Tokens.Enqueue(new CharacterToken((char)CurrentCharacter));
    }

    private void EmitReplacementCharacterToken()
    {
        Tokens.Enqueue(new CharacterToken((char)CodePoint.ReplacementCharacter));
    }

    private void EmitCurrentTagToken()
    {
        if (CurrentToken is not TagToken)
        {
            throw new InvalidOperationException("Current token is not a tag token.");
        }

        var currentTagToken = (TagToken)CurrentToken;

        if (currentTagToken.IsStart())
        {
            LastStartTagToken = currentTagToken;
        }

        Tokens.Enqueue(currentTagToken);
    }

    private void EmitCurrentCommentToken()
    {
        if (CurrentToken is not CommentToken)
        {
            throw new InvalidOperationException("Current token is not a comment token.");
        }

        var currentCommentToken = (CommentToken)CurrentToken;
        Tokens.Enqueue(currentCommentToken);
    }

    private void EmitCurrentDocTypeToken()
    {
        if (CurrentToken is not DocTypeToken)
        {
            throw new InvalidOperationException("Current token is not a doctype token.");
        }

        var currentDoctypeToken = (DocTypeToken)CurrentToken;
        Tokens.Enqueue(currentDoctypeToken);
    }

    private void EmitCharacterToken(CodePoint codePoint)
    {
        EmitCharacterToken((char)codePoint);
    }

    private void EmitCharacterToken(char c)
    {
        Tokens.Enqueue(new CharacterToken(c));
    }

    private void EmitTempBufferTokens()
    {
        var str = TempBuffer.ToString();
        foreach (char c in str)
        {
            EmitCharacterToken(c);
        }
    }

    private void EmitEndOfFileToken()
    {
        Tokens.Enqueue(new EndOfFileToken());
    }

    private TagToken GetCurrentTagTokenOrFail()
    {
        if (CurrentToken is not TagToken)
        {
            throw new InvalidOperationException("Current token is not a tag token.");
        }

        return (TagToken)CurrentToken;
    }

    private CommentToken GetCurrentCommentTokenOrFail()
    {
        if (CurrentToken is not CommentToken)
        {
            throw new InvalidOperationException("Current token is not a comment token.");
        }

        return (CommentToken)CurrentToken;
    }

    private DocTypeToken GetCurrentDocTypeTokenOrFail()
    {
        if (CurrentToken is not DocTypeToken)
        {
            throw new InvalidOperationException("Current token is not a doctype token.");
        }

        return (DocTypeToken)CurrentToken;
    }

    private void AppendToCurrentTagTokenName(char c)
    {
        GetCurrentTagTokenOrFail().AppendToName(c);
    }

    private void AppendToCurrentTagTokenName(CodePoint codePoint)
    {
        AppendToCurrentTagTokenName((char)codePoint);
    }

    private void StartNewAttributeInCurrentTagToken(string name = "", string value = "")
    {
        var currentTagToken = GetCurrentTagTokenOrFail();
        currentTagToken.Attributes.Add(name, value);

        CurrentAttributeName = name;
    }

    private void AppendCharacterToCurrentAttributeNameOrFail(char c)
    {
        if (CurrentAttributeName is null)
        {
            throw new InvalidOperationException("Cannot append character because there is no current attribute name.");
        }

        var currentTagToken = GetCurrentTagTokenOrFail();
        currentTagToken.AppendToAttributeName(CurrentAttributeName, c);
        CurrentAttributeName += c;
    }

    private void AppendCharacterToCurrentAttributeValueOrFail(char c)
    {
        if (CurrentAttributeName is null)
        {
            throw new InvalidOperationException("Cannot append character because there is no current attribute name.");
        }

        var currentTagToken = GetCurrentTagTokenOrFail();
        currentTagToken.AppendToAttributeValue(CurrentAttributeName, c); 
    }

    private bool IsCharRefConsumedAsPartOfAttribute()
    {
        return ReturnState == State.AttributeValueDoubleQuoted ||
            ReturnState == State.AttributeValueSingleQuoted ||
            ReturnState == State.AttributeValueUnquoted;
    }

    private void FlushCodePointsConsumedAsACharacterReference()
    {
        if (IsCharRefConsumedAsPartOfAttribute())
        {
            foreach (char c in TempBuffer.ToString())
            {
                AppendCharacterToCurrentAttributeValueOrFail(c);
            }
        }
    }

    private bool IsCurrentTokenAnAppropriateEndTagToken()
    {
        return CurrentToken is TagToken currentTagToken
            && LastStartTagToken is not null
            && currentTagToken.IsEnd()
            && currentTagToken.Matches(LastStartTagToken);
    }

    private bool IsEOF()
    {
        return CurrentCharacter == END_OF_FILE;
    }

    private bool IsWhiteSpace()
    {
        return CurrentCharacter.IsOneOf(
            CodePoint.Tab,
            CodePoint.LineFeed,
            CodePoint.FormFeed,
            CodePoint.Space);
    }

    private bool IsCharacterReferenceCodeWhiteSpace()
    {
        return CharacterReferenceCode.IsOneOf(
            CodePoint.Tab,
            CodePoint.LineFeed,
            CodePoint.FormFeed,
            CodePoint.Space);    
    }

    private bool Match(string str, bool isCaseSensitive = true)
    {
        // Fill up the buffer with as many character from str as possible.
        int cursor = 0;
        if (isCaseSensitive)
        {
            while (cursor < str.Length && str[cursor] == Input.Peek())
            {
                Buffer.Enqueue(Input.Read());
                cursor++;
            }
        }
        else
        {
            while (cursor < str.Length && char.ToLower(str[cursor]) == char.ToLower((char)Input.Peek()))
            {
                Buffer.Enqueue(Input.Read());
                cursor++;
            }
        }

        /*
        * If the input string str of length n matches the
        * next n characters in the stream, then we have a
        * match, so consume all of the characters.
        * Otherwise, return false.
        */
        var isMatch = cursor == str.Length;
        if (isMatch)
        {
            ConsumeNextInputCharacters(str.Length);
        }

        return isMatch;
    }

    private void ConsumeNextInputCharacter()
    {
        // TODO: https://html.spec.whatwg.org/#preprocessing-the-input-stream

        if (!ShouldReconsume)
        {
            // Read from our internal buffer before checking the stream.
            var next = Buffer.Count > 0
                ? Buffer.Dequeue()
                : Input.Read();

            CurrentCharacter = next == -1
                ? END_OF_FILE
                : (uint)next;
        }
    }

    private void ConsumeNextInputCharacters(int numCharacters)
    {
        for (int i = 0; i < numCharacters; i++)
        {
            ConsumeNextInputCharacter();
        }
    }
}