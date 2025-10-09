namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#nodelist
class NodeList
{
    private List<Node> Nodes { get; set; } = [];
    public ulong Length => (ulong)Nodes.Count;
    public Node? First => Nodes.FirstOrDefault();
    public Node? Last => Nodes.LastOrDefault();

    public Node? Item(ulong index)
    {
        return Nodes.ElementAtOrDefault((int)index);
    }

    public void Append(Node node)
    {
        if (!Nodes.Contains(node))
        {
            Nodes.Add(node);
        }
    }

    public void InsertBefore(Node node, Node nodeToInsert)
    {
        if (Nodes.Contains(node) && !Nodes.Contains(nodeToInsert))
        {
            var index = Nodes.IndexOf(node);

            if (index > 0)
            {
                Nodes.Insert(index - 1, nodeToInsert);
            }
            else if (index == 0)
            {
                Nodes = (List<Node>) Nodes.Prepend(node);
            }
        }
    }

    public void Remove(Node node)
    {
        Nodes.Remove(node);
    }

    public Node? Previous(Node node)
    {
        return Nodes.ElementAtOrDefault(Nodes.IndexOf(node) - 1);
    }

    public Node? Next(Node node)
    {
        return Nodes.ElementAtOrDefault(Nodes.IndexOf(node) + 1);
    }

    public List<Node> AsList()
    {
        var list = new List<Node>();

        foreach (var node in Nodes)
        {
            list.Add(node);
        }

        return list;
    }

    public override bool Equals(Object? other)
    {
        if (other == null || other is not NodeList)
        {
            return false;
        }
        else
        {
            var otherNodeList = (NodeList)other;

            var thisChildren = Nodes;
            var otherChildren = otherNodeList.Nodes;

            var hasSameNumberOfChildren = thisChildren.Count == otherChildren.Count;

            if (!hasSameNumberOfChildren)
            {
                return false;
            }
            
            var hasSameChildren = true;

            for (int i = 0; i < Nodes.Count; i++)
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
        return Nodes.GetHashCode();
    }
}