using System.Diagnostics;

namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#node
abstract class Node: EventTarget
{
    // https://dom.spec.whatwg.org/#concept-node-document
    public Document Document { get; set; }

    // https://dom.spec.whatwg.org/#dom-node-ownerdocument
    public Document? OwnerDocument => this is Document ? null : Document;

    // https://dom.spec.whatwg.org/#dom-node-ownerdocument
    public Node? ParentNode { get; set; }

    // https://dom.spec.whatwg.org/#dom-node-childnodes
    public NodeList ChildNodes { get; } = new();

    // https://dom.spec.whatwg.org/#dom-node-firstchild
    public Node? FirstChild => ChildNodes.First;

    // https://dom.spec.whatwg.org/#dom-node-lastchild
    public Node? LastChild => ChildNodes.Last;

    // https://dom.spec.whatwg.org/#dom-node-previoussibling
    public Node? PreviousSibling => ParentNode?.ChildNodes.Previous(this);

    // https://dom.spec.whatwg.org/#dom-node-nextsibling
    public Node? NextSibling => ParentNode?.ChildNodes.Next(this);

    // https://dom.spec.whatwg.org/#concept-node-length
    public int Length
    {
        get
        {
            if (this is DocumentType || this is Attr)
            {
                return 0;
            }
            else if (this is CharacterData data)
            {
                return data.Data.Length;
            }

            return (int)ChildNodes.Length;
        }
    }

    public Node()
    {

    }

    public Node(Document document, Node? parent = null)
    {
        Document = document;
        ParentNode = parent;
    }

    // https://dom.spec.whatwg.org/#concept-node-insert
    public void InsertBefore(Node node, Node? child = null, bool suppressObservers = false)
    {
        var parentNode = this;

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
            // 1. For each live range whose start node is parent and start offset is greater than child’s index,
            //    increase its start offset by count.
            // 2. For each live range whose end node is parent and end offset is greater than child’s index,
            //    increase its end offset by count.

        // 6. Let previousSibling be child’s previous sibling or parent’s last child if child is null.
        var previousSibling = child is not null
            ? child.PreviousSibling
            : parentNode.LastChild;

        // 7. For each node in nodes, in tree order:
        foreach (var nodeToInsert in nodes)
        {
            if (parentNode.Document is null)
            {
                break;
            }

            // 1. Adopt node into parent’s node document.
            parentNode.Document.AdoptNode(nodeToInsert);

            // 2. If child is null, then append node to parent’s children.
            if (child is null)
            {
                parentNode.ChildNodes.Append(node);
            }
            // 3. Otherwise, insert node into parent’s children before child’s index.
            else
            {
                parentNode.ChildNodes.InsertBefore(child, node);
            }

            // TODO - 4. If parent is a shadow host whose shadow root’s slot assignment 
            // is "named" and node is a slottable, then assign a slot for node.

            // TODO - 5. If parent’s root is a shadow root, and parent is a slot whose assigned nodes is the empty list,
            //           then run signal a slot change for parent.

            // TODO - 6. Run assign slottables for a tree with node’s root.

            // TODO - 7. For each shadow-including inclusive descendant inclusiveDescendant of node, in shadow-including tree order:
                // TODO - 1. Run the insertion steps with inclusiveDescendant.
                // TODO - 2. If inclusiveDescendant is connected:
                    // TODO - 1. If inclusiveDescendant is custom, then enqueue a custom element callback reaction
                    //           with inclusiveDescendant, callback name "connectedCallback", and « ».
                    // TODO - 2. Otherwise, try to upgrade inclusiveDescendant.

            // TODO - 8. If suppress observers flag is unset, then queue a tree mutation record for parent with
            //           nodes, « », previousSibling, and child.

            // 9. Run the children changed steps for parent.
            parentNode.ChildrenChanged();

            // 10. Let staticNodeList be a list of nodes, initially « ».
            var staticNodeList = new List<Node>();

            // TODO - 11. For each node of nodes, in tree order:
                // TODO - 1. For each shadow-including inclusive descendant inclusiveDescendant of node,
                //           in shadow-including tree order, append inclusiveDescendant to staticNodeList.
        
            // TODO - 12. For each node of staticNodeList, if node is connected, then run the post-connection steps with node.
        }
    }
    

    // https://dom.spec.whatwg.org/#concept-node-remove
    public void Remove(bool suppressObservers = false)
    {
        // 2. Assert: parent is non-null.
        Debug.Assert(ParentNode is not null);

        // 1. Let parent be node’s parent.
        var parent = ParentNode;
        
        // 3. TODO - Run the live range pre-remove steps, given node.
        // 4. TODO - For each NodeIterator object iterator whose root’s node document is node’s node document,
        //    run the NodeIterator pre-remove steps given node and iterator.

        // 5. Let oldPreviousSibling be node’s previous sibling.
        var oldPreviousSibling = PreviousSibling;

        // 6. Let oldNextSibling be node’s next sibling.
        var oldNextSibling = NextSibling;

        // 7. Remove node from its parent’s children.
        parent.ChildNodes.Remove(this);

        // 8. TODO - If node is assigned, then run assign slottables for node’s assigned slot.
        // 9. TODO - If parent’s root is a shadow root, and parent is a slot whose assigned nodes
        //    is the empty list, then run signal a slot change for parent.

        // 10. TODO - If node has an inclusive descendant that is a slot:
            // 1. TODO - Run assign slottables for a tree with parent’s root.
            // 2. TODO - Run assign slottables for a tree with node.

        // 11. TODO - Run the removing steps with node and parent.

        // 12. TODO - Let isParentConnected be parent’s connected.
        var isParentConnected = false;

        // 13. TODO - If node is custom and isParentConnected is true, then enqueue a custom element callback reaction
        //     with node, callback name "disconnectedCallback", and « ».
        if (this is Element element && element.IsCustom && isParentConnected)
        {
            
        }

        // 14. TODO - For each shadow-including descendant descendant of node, in shadow-including tree order:
            //  1. TODO - Run the removing steps with descendant and null.
            //  2. TODO - If descendant is custom and isParentConnected is true, then 
            //     enqueue a custom element callback reaction with descendant, callback name "disconnectedCallback", and « ».

        // 15. TODO - For each inclusive ancestor inclusiveAncestor of parent,
        //     and then for each registered of inclusiveAncestor’s registered observer list, 
        //     if registered’s options["subtree"] is true, then append a new transient registered observer
        //     whose observer is registered’s observer, options is registered’s options,
        //     and source is registered to node’s registered observer list.

        // 16. TODO - If suppress observers flag is unset, then queue a tree mutation record for parent
        //     with « », « node », oldPreviousSibling, and oldNextSibling.

        // 17. Run the children changed steps for parent.
        parent.ChildrenChanged();
    }

    // https://dom.spec.whatwg.org/#concept-node-equals
    public override bool Equals(Object? other)
    {
        if (other == null || other is not Node || this.GetType() != other.GetType())
        {
            return false;
        }

        var otherNode = (Node)other;

        if ((this is DocumentType thisDoc && otherNode is DocumentType otherDoc && !thisDoc.Equals(otherDoc)) ||
            (this is Element thisElem && otherNode is Element otherElem && !thisElem.Equals(otherElem)) ||
            (this is Attr thisAttr && otherNode is Attr otherAttr && !thisAttr.Equals(otherAttr)) ||
            (this is Text thisText && otherNode is Text otherText && !thisText.Equals(otherText)) ||
            (this is Comment thisComment && otherNode is Comment otherComment && !thisComment.Equals(otherComment)))
        {
            return false;
        }

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

    // https://dom.spec.whatwg.org/#concept-node-children-changed-ext
    public void ChildrenChanged()
    {

    }

    // https://dom.spec.whatwg.org/#concept-node-post-connection-ext
    public void PostConnection()
    {

    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ChildNodes.GetHashCode());
    }

}