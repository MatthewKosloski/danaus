using Danaus.Core;

namespace Danaus.HTML;

enum State
{
    Data,
    RCDATA,
    RAWTEXT,
    ScriptData,
    PLAINTEXT,
    TagOpen,
    EndTagOpen,
    CharacterReference,
    MarkupDeclarationOpen,
    TagName,
    BogusComment,
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

    private void CreateNewCommentToken(string data)
    {
        CreateNewToken(new CommentToken(data));
    }

    private void EmitCurrentCharacterAsCharacterToken()
    {
        Tokens.Enqueue(new CharacterToken((char)CurrentCharacter));
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

    private bool IsEOF()
    {
        return CurrentCharacter == END_OF_FILE;
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