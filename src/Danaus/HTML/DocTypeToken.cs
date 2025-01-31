namespace Danaus.HTML;

class DocTypeToken: HTMLToken 
{
    public string? Name { get; } = null;
    public string? PublicIdentifier { get; } = null;
    public string? SystemIdentifier { get; } = null;
    public bool ForceQuirks { get; } = false;
}