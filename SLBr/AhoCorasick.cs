using System.Collections;

namespace SLBr
{
    public class Trie : Trie<string>, IEnumerable<string>
    {
        public void Add(string s)
        {
            Add(s, s);
        }
        public void Add(IEnumerable<string> strings)
        {
            foreach (string s in strings)
                Add(s);
        }
        public IEnumerator<string> GetEnumerator() => Enumerable.Empty<string>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class Trie<TValue> : Trie<char, TValue>
    {
    }

    public class Trie<T, TValue>
    {
        public static Trie FromList(IEnumerable<string> patterns)
        {
            Trie trie = new Trie { patterns };
            trie.Build();
            return trie;
        }

        private readonly Node<T, TValue> root = new Node<T, TValue>();

        public void Add(IEnumerable<T> word, TValue value)
        {
            var node = root;
            foreach (T c in word)
            {
                var child = node[c];
                if (child == null)
                    child = node[c] = new Node<T, TValue>(c, node);
                node = child;
            }
            node.Values.Add(value);
        }
        public void Build()
        {
            var queue = new Queue<Node<T, TValue>>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var child in node)
                    queue.Enqueue(child);
                if (node == root)
                {
                    root.Fail = root;
                    continue;
                }
                var fail = node.Parent.Fail;
                while (fail[node.Word] == null && fail != root)
                    fail = fail.Fail;
                node.Fail = fail[node.Word] ?? root;
                if (node.Fail == node)
                    node.Fail = root;
            }
        }

        public IEnumerable<TValue> Find(IEnumerable<T> text)
        {
            var node = root;
            foreach (T c in text)
            {
                while (node[c] == null && node != root)
                    node = node.Fail;
                node = node[c] ?? root;
                for (var t = node; t != root; t = t.Fail)
                {
                    foreach (TValue value in t.Values)
                        yield return value;
                }
            }
        }

        private class Node<TNode, TNodeValue> : IEnumerable<Node<TNode, TNodeValue>>
        {
            private readonly TNode word;
            private readonly Node<TNode, TNodeValue> parent;
            private readonly Dictionary<TNode, Node<TNode, TNodeValue>> children = new Dictionary<TNode, Node<TNode, TNodeValue>>();
            private readonly List<TNodeValue> values = new List<TNodeValue>();

            public Node()
            {
            }

            public Node(TNode word, Node<TNode, TNodeValue> parent)
            {
                this.word = word;
                this.parent = parent;
            }
            public TNode Word
            {
                get { return word; }
            }
            public Node<TNode, TNodeValue> Parent
            {
                get { return parent; }
            }
            public Node<TNode, TNodeValue> Fail
            {
                get;
                set;
            }
            public Node<TNode, TNodeValue> this[TNode c]
            {
                get { return children.ContainsKey(c) ? children[c] : null; }
                set { children[c] = value; }
            }
            public List<TNodeValue> Values
            {
                get { return values; }
            }

            public IEnumerator<Node<TNode, TNodeValue>> GetEnumerator()
            {
                return children.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public override string ToString()
            {
                return Word.ToString();
            }
        }
    }
}

public class DomainList : IEnumerable<string>
{
    private readonly TrieNode Root = new();
    public readonly HashSet<string> AllDomains = new();

    public void Add(string Domain)
    {
        if (!AllDomains.Add(Domain)) return;

        bool WildCard = Domain.StartsWith("*.", StringComparison.Ordinal);
        var Parts = Domain.Trim().TrimStart('*', '.').TrimEnd('.').Split('.').Reverse();
        var _Node = Root;

        foreach (var part in Parts)
        {
            if (!_Node.Children.TryGetValue(part, out var Child))
            {
                Child = new TrieNode();
                _Node.Children[part] = Child;
            }
            _Node = Child;
        }

        _Node.IsEnd = true;
        _Node.Wildcard = WildCard;
    }

    public bool Has(string Host)
    {
        var Parts = Host.Trim().TrimEnd('.').Split('.').Reverse();
        var _Node = Root;

        foreach (var part in Parts)
        {
            if (_Node.IsEnd && _Node.Wildcard)
                return true;
            if (!_Node.Children.TryGetValue(part, out _Node))
                return false;
        }

        return _Node.IsEnd;
    }

    public void Remove(string Domain)
    {
        if (!AllDomains.Remove(Domain)) return;

        RemoveRecursive(Root, Domain.Trim().TrimStart('*', '.').TrimEnd('.').Split('.').Reverse().ToList(), 0, Domain.StartsWith("*.", StringComparison.Ordinal));
    }

    private bool RemoveRecursive(TrieNode _Node, List<string> Parts, int Index, bool Wildcard)
    {
        if (Index == Parts.Count)
        {
            if (!_Node.IsEnd || _Node.Wildcard != Wildcard)
                return false;
            _Node.IsEnd = false;
            _Node.Wildcard = false;
            return _Node.Children.Count == 0;
        }
        string Part = Parts[Index];
        if (!_Node.Children.TryGetValue(Part, out TrieNode child))
            return false;
        if (RemoveRecursive(child, Parts, Index + 1, Wildcard))
            _Node.Children.Remove(Part);
        return !_Node.IsEnd && _Node.Children.Count == 0;
    }

    class TrieNode
    {
        public Dictionary<string, TrieNode> Children = new();
        public bool IsEnd = false;
        public bool Wildcard = false;
    }

    public IEnumerator<string> GetEnumerator() => throw new NotSupportedException();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}