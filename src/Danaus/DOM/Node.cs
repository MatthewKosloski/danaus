namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#node
abstract class Node(Document? document, Node? parent = null): EventTarget
{
    public Document? Document { get; set; } = document;

    public Node? ParentNode { get; } = parent;

    public NodeList ChildNodes { get; } = new();

    public Node? FirstChild => ChildNodes.First;

    public Node? LastChild => ChildNodes.Last;

    public Node? PreviousSibling => ParentNode?.ChildNodes.Previous(this);

    public Node? NextSibling => ParentNode?.ChildNodes.Next(this);

    // https://dom.spec.whatwg.org/#concept-node-insert
    public void InsertBefore(Node node, Node? child, bool suppressObservers = false)
    {
        if (ParentNode is null)
        {
            return;
        }

        // 1. Let nodes be node’s children, if node is a DocumentFragment node; otherwise node.
        List<Node> nodes = node is DocumentFragment
            ? node.ChildNodes.AsList()
            : [node];

        // 2. Let count be nodes’s size.
        var count = nodes.Count;

        // 3. If count is 0, then return.
        if (count == 0)
        {
            return;
        }

        // TODO
        // 4. If node is a DocumentFragment node:
            // 1. Remove its children with the suppress observers flag set.
            // 2. Queue a tree mutation record for node with « », nodes, null, and null.

        // TODO
        // 5. If child is non-null:
            // 1. For each live range whose start node is parent and start offset is greater than child’s index, increase its start offset by count.
            // 2. For each live range whose end node is parent and end offset is greater than child’s index, increase its end offset by count.

        // 6. Let previousSibling be child’s previous sibling or parent’s last child if child is null.
        var previousSibling = child is not null
            ? child.PreviousSibling
            : ParentNode.LastChild;
    }

    // https://dom.spec.whatwg.org/#concept-node-equals
    public override bool Equals(Object? other)
    {
        if (other == null || other is not Node)
        {
            return false;
        }
        else
        {
            var otherNode = (Node)other;

            var thisChildren = ChildNodes.AsList();
            var otherChildren = otherNode.ChildNodes.AsList();

            var hasSameNumberOfChildren = thisChildren.Count == otherChildren.Count;

            if (!hasSameNumberOfChildren)
            {
                return false;
            }

            var hasSameChildren = true;

            for (int i = 0; i < (int)ChildNodes.Length; i++)
            {
                var thisChild = thisChildren.ElementAt(i);
                var otherChild = otherChildren.ElementAt(i);

                if (!thisChild.Equals(otherChild))
                {
                    hasSameChildren = false;
                    break;
                }
            }

            return hasSameNumberOfChildren && hasSameChildren;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ChildNodes.GetHashCode());
    }

}