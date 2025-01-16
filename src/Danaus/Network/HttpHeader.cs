namespace Danaus.Network;

public abstract class HttpHeader(string name)
{
    public string Name { get; } = name;
}