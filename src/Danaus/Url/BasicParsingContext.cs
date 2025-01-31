using System.Text;

namespace Danaus.Url;

class BasicParsingContext(string input, ParseResult result, Encoding? encoding, State? stateOverride, URL? baseUrl)
{
    private const uint END_OF_FILE = 0xFFFFFFFF;

    public string Buffer { get; private set; } = string.Empty;

    public string Input { get; } = input;

    public int Pointer { get; private set; } = -1;

    public State State { get; private set; } = stateOverride ?? State.SchemeStart;

    public readonly State? StateOverride = stateOverride;

    public ParseResult Result { get; } = result;

    public URL? BaseUrl { get; } = baseUrl;

    public bool AtSignSeen { get; private set; } = false;

    public bool InsideBrackets { get; private set; } = false;

    public bool PasswordTokenSeen { get; private set; } = false;

    public Encoding Encoding { get; private set; } = encoding ?? Encoding.UTF8;

    public uint CodePoint
    {
        get
        {
            return IsEOF()
                ? END_OF_FILE
                : Input[Pointer];
        } 
    }

    public string Remaining
    {
        get
        {
            var hasRemaining = Pointer + 1 < Input.Length;
            return hasRemaining
                ? Input[(Pointer + 1)..]
                : string.Empty;
        }
    }

    public bool HasStateOverride
    {
        get
        {
            return StateOverride is not null;
        }
    }

    public bool IsEOF()
    {
        return Pointer >= Input.Length;
    }

    public void SetAtSignSeen()
    {
        AtSignSeen = true;
    }

    public void SetInsideBrackets(bool insideBrackets = true)
    {
        InsideBrackets = insideBrackets;
    }

    public void SetPasswordTokenSeen()
    {
        PasswordTokenSeen = true;
    }

    public void SetEncoding(Encoding encoding)
    {
        Encoding = encoding;
    }

    public void AppendToBuffer(string s)
    {
        Buffer += s;
    }

    public void AppendCodePointToBuffer()
    {
        Buffer += (char)CodePoint;
    }

    public void AppendLoweredCodePointToBuffer()
    {
        Buffer += char.ToLower((char)CodePoint);
    }

    public void ClearBuffer()
    {
        Buffer = string.Empty;
    }

    public void SetBuffer(string s)
    {
        Buffer = s;
    }

    public void DecrementBufferBy(int amount)
    {
        Pointer -= amount;
    }

    public void GoToPreviousCodePoint()
    {
        Pointer--;
    }

    public void GoToNextCodePoint()
    {
        Pointer++;
    }

    public void ResetPointer()
    {
        Pointer = -1;
    }

    public void SetState(State nextState)
    {
        State = nextState;
    }

    public void Deconstruct(out uint codePoint, out ParseResult result, out URL url, out URL? baseUrl)
    {
        codePoint = CodePoint;
        result = Result;
        url = Result.Url;
        baseUrl = BaseUrl;
    }
}