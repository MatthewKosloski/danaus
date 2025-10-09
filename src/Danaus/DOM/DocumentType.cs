namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#documenttype
class DocumentType(string name, Document document): Node(document)
{
    public string Name { get; set; } = name;
    public string PublicID { get; set; } = string.Empty;
    public string SystemID { get; set; } = string.Empty;

    // https://dom.spec.whatwg.org/#concept-node-equals
    public override bool Equals(object? other)
    {
        if (other == null || other is not DocumentType)
        {
            return false;
        }
        else
        {
            var otherDocumentType = (DocumentType)other;

            return Name == otherDocumentType.Name &&
                   PublicID == otherDocumentType.PublicID &&
                   SystemID == otherDocumentType.SystemID;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name.GetHashCode(),
            PublicID.GetHashCode(),
            SystemID.GetHashCode());
    }
}
