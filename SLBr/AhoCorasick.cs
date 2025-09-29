using System.Collections;

namespace SLBr
{
    public class Trie : Trie<string>, IEnumerable<string>
    {
        public void Add(string s) =>
            Add(s, s);
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
            Trie _Trie = new Trie { patterns };
            _Trie.Build();
            return _Trie;
        }

        private readonly Node<T, TValue> _Root = new Node<T, TValue>();

        public void Add(IEnumerable<T> _Word, TValue _Value)
        {
            var _Node = _Root;
            foreach (T c in _Word)
            {
                var Child = _Node[c];
                if (Child == null)
                    Child = _Node[c] = new Node<T, TValue>(c, _Node);
                _Node = Child;
            }
            _Node.Values.Add(_Value);
        }
        public void Build()
        {
            var Queue = new Queue<Node<T, TValue>>();
            Queue.Enqueue(_Root);
            while (Queue.Count > 0)
            {
                var _Node = Queue.Dequeue();
                foreach (var Child in _Node)
                    Queue.Enqueue(Child);
                if (_Node == _Root)
                {
                    _Root.Fail = _Root;
                    continue;
                }
                var _Fail = _Node.Parent.Fail;
                while (_Fail[_Node.Word] == null && _Fail != _Root)
                    _Fail = _Fail.Fail;
                _Node.Fail = _Fail[_Node.Word] ?? _Root;
                if (_Node.Fail == _Node)
                    _Node.Fail = _Root;
            }
        }

        public IEnumerable<TValue> Find(IEnumerable<T> Text)
        {
            var _Node = _Root;
            foreach (T c in Text)
            {
                while (_Node[c] == null && _Node != _Root)
                    _Node = _Node.Fail;
                _Node = _Node[c] ?? _Root;
                for (var t = _Node; t != _Root; t = t.Fail)
                {
                    foreach (TValue _Value in t.Values)
                        yield return _Value;
                }
            }
        }

        private class Node<TNode, TNodeValue> : IEnumerable<Node<TNode, TNodeValue>>
        {
            private readonly TNode _Word;
            private readonly Node<TNode, TNodeValue> _Parent;
            private readonly Dictionary<TNode, Node<TNode, TNodeValue>> _Children = new Dictionary<TNode, Node<TNode, TNodeValue>>();
            private readonly List<TNodeValue> _Values = new List<TNodeValue>();

            public Node()
            {
            }

            public Node(TNode word, Node<TNode, TNodeValue> parent)
            {
                _Word = word;
                _Parent = parent;
            }
            public TNode Word
            {
                get { return _Word; }
            }
            public Node<TNode, TNodeValue> Parent
            {
                get { return _Parent; }
            }
            public Node<TNode, TNodeValue> Fail
            {
                get;
                set;
            }
            public Node<TNode, TNodeValue> this[TNode c]
            {
                get { return _Children.ContainsKey(c) ? _Children[c] : null; }
                set { _Children[c] = value; }
            }
            public List<TNodeValue> Values
            {
                get { return _Values; }
            }

            public IEnumerator<Node<TNode, TNodeValue>> GetEnumerator() =>
                _Children.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public override string ToString() =>
                Word.ToString();
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
        var Parts = Domain.Trim().TrimStart('*', '.').TrimEnd('.').Split('.').AsEnumerable().Reverse();
        var _Node = Root;

        foreach (var Part in Parts)
        {
            if (!_Node.Children.TryGetValue(Part, out var Child))
            {
                Child = new TrieNode();
                _Node.Children[Part] = Child;
            }
            _Node = Child;
        }

        _Node.IsEnd = true;
        _Node.Wildcard = WildCard;
    }

    public bool Has(string Host)
    {
        Host = Host.AsSpan().TrimEnd('.').ToString();
        var Span = Host.AsSpan();
        TrieNode Node = Root;

        int End = Span.Length;
        while (End > 0)
        {
            int Dot = Span.Slice(0, End).LastIndexOf(".", StringComparison.Ordinal);
            ReadOnlySpan<char> Label;
            if (Dot == -1)
            {
                Label = Span.Slice(0, End);
                End = 0;
            }
            else
            {
                Label = Span.Slice(Dot + 1, End - Dot - 1);
                End = Dot;
            }
            if (Node.IsEnd && Node.Wildcard)
                return true;
            if (!Node.Children.TryGetValue(Label.ToString(), out Node))
                return false;
        }

        return Node.IsEnd;

        /*var Parts = Host.Trim().TrimEnd('.').Split('.').Reverse();
        var _Node = Root;

        foreach (var part in Parts)
        {
            if (_Node.IsEnd && _Node.Wildcard)
                return true;
            if (!_Node.Children.TryGetValue(part, out _Node))
                return false;
        }

        return _Node.IsEnd;*/
    }

    public void Remove(string Domain)
    {
        if (!AllDomains.Remove(Domain)) return;
        RemoveRecursive(Root, Domain.Trim().TrimStart('*', '.').TrimEnd('.').Split('.').AsEnumerable().Reverse().ToList(), 0, Domain.StartsWith("*.", StringComparison.Ordinal));
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