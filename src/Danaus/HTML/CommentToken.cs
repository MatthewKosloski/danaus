namespace Danaus.HTML;

class CommentToken(string data): HTMLToken 
{
    public string Data { get; } = data;
}