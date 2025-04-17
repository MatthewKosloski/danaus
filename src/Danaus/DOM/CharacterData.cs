using Danaus.WebIDL;

namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#characterdata
abstract class CharacterData(Document document, string data = ""): Node(document)
{
    // https://dom.spec.whatwg.org/#concept-cd-data
    public string Data { get; private set; } = data;

    // https://dom.spec.whatwg.org/#dom-characterdata-appenddata
    public void AppendData(string data)
    {
        ReplaceDataImpl(Length, 0, data);
    }

    // https://dom.spec.whatwg.org/#dom-characterdata-insertdata
    public void InsertData(int offset, string data)
    {
        ReplaceDataImpl(offset, 0, data);
    }

    // https://dom.spec.whatwg.org/#dom-characterdata-deletedata
    public void DeleteData(int offset, int count)
    {
        ReplaceDataImpl(offset, count, string.Empty);
    }

    // https://dom.spec.whatwg.org/#dom-characterdata-replacedata
    public void ReplaceData(int offset, int count, string data)
    {
        ReplaceDataImpl(offset, count, data);
    }

    // https://dom.spec.whatwg.org/#concept-cd-replace
    private void ReplaceDataImpl(int offset, int count, string data)
    {
        // 1. Let length be node’s length.
        var length = Length;

        // 2. If offset is greater than length, then throw an "IndexSizeError" DOMException.
        if (offset > length)
        {
            throw new DOMException("Offset cannot be greater than length", "IndexSizeError");
        }

        // 3. If offset plus count is greater than length, then set count to length minus offset.
        if (offset + count > length)
        {
            count = length - offset;
        }

        // 4. TODO - Queue a mutation record of "characterData" for node with null, null, node’s data, « », « », null, and null.

        // 5. Insert data into node’s data after offset code units.
        Data = Data[..offset] + data + Data[offset..];

        // 6. Let delete offset be offset + data’s length.
        var deleteOffset = offset + data.Length;

        // 7. Starting from delete offset code units, remove count code units from node’s data.
        Data = Data[..deleteOffset] + Data[(deleteOffset + count)..];

        // 8. TODO - For each live range whose start node is node and start offset is
        //    greater than offset but less than or equal to offset plus count, set its start offset to offset.
        // 9. TODO - For each live range whose end node is node and end offset is
        //    greater than offset but less than or equal to offset plus count, set its end offset to offset.
        // 10. TODO - For each live range whose start node is node and start offset is
        //     greater than offset plus count, increase its start offset by data’s length and decrease it by count.
        // 11. TODO - For each live range whose end node is node and end offset
        //     is greater than offset plus count, increase its end offset by data’s length and decrease it by count.
        
        // 12. If node’s parent is non-null, then run the children changed steps for node’s parent.
        ParentNode?.ChildrenChanged();
    }
}