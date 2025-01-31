namespace Danaus.HTML;

class CharacterToken(char data): HTMLToken 
{
    public char Data { get; } = data;
}