namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#documenttype
class DocumentType(string name, Document document): Node(document)
{
    public string Name { get; set; } = name;
    public string PublicID { get; set; } = string.Empty;
    public string SystemID { get; set; } = string.Empty;
}