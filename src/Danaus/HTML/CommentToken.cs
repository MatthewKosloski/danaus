using Danaus.Core;

namespace Danaus.HTML;

class CommentToken(string data): HTMLToken 
{
    public string Data { get; private set; } = data;

    public void AppendToData(CodePoint c)
    {
        Data += (char)c;
    }

    public void AppendToData(uint c)
    {
        Data += (char)c;
    }
}