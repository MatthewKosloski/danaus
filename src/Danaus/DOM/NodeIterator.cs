namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#nodeiterator
sealed class NodeIterator(Node root, Node referenceNode)
{
    public Node Root { get; } = root;
    public Node ReferenceNode { get; } = referenceNode;
    public bool PointerBeforeReferenceNode { get; set; }
    public ulong WhatToShow { get; set; }
    public NodeFilter? Filter { get; set; }

}