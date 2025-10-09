namespace Danaus.HTML;

class DocTypeToken(string? name = null, bool forceQuirks = false, string? publicIdentifier = null, string? systemIdentifier = null): HTMLToken 
{
    public string? Name { get; set; } = name;
    public string? PublicIdentifier { get; set; } = publicIdentifier;
    public string? SystemIdentifier { get; set; } = systemIdentifier;
    public bool ForceQuirks { get; set; } = forceQuirks;
}