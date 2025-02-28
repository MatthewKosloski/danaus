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

    // A reference to the last start tag token that was emitted.
    private TagToken? LastStartTagToken = null;

    private string? CurrentAttributeName = null;

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

    private void ConsumeNextInputCharacter()
    {
        if (!ShouldReconsume)
        {
            // TODO: https://html.spec.whatwg.org/#preprocessing-the-input-stream
            var next = Input.Read();
            CurrentCharacter = next == -1
                ? END_OF_FILE
                : (uint)next;
        }
    }    
}