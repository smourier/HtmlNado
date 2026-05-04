using HtmlNado.Utilities;

namespace HtmlNado;

public sealed class HtmlNodeList : IList<HtmlNode>, INotifyCollectionChanged
{
    private readonly List<HtmlNode> _list = [];
    private readonly HtmlNode _parent;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    internal HtmlNodeList(HtmlNode parent)
    {
        _parent = parent;
    }

    public int Count => _list.Count;
    public HtmlNode? this[string? name] => _list.Find(n => n.Name.EqualsOrdinalIgnoreCase(name));
    public HtmlNode? this[string? localName, string? namespaceURI] => _list.Find(a => localName.EqualsOrdinalIgnoreCase(a.LocalName) && a.NamespaceURI != null && string.Equals(namespaceURI, a.NamespaceURI, StringComparison.Ordinal));
    public HtmlNode this[int index]
    {
        get => _list[index];
        set
        {
            if (value == _list[index])
                return;

            var oldItem = _list[index];
            _list[index] = value;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem));
        }
    }

    public void Replace(HtmlNode newChild, HtmlNode oldChild)
    {
        ArgumentNullException.ThrowIfNull(newChild);
        ArgumentNullException.ThrowIfNull(oldChild);

        if (newChild.ParentNode != null)
            throw new ArgumentException(null, nameof(newChild));

        var index = _list.IndexOf(oldChild);
        if (index >= 0)
        {
            if (oldChild.ParentNode != _parent)
                throw new ArgumentException(null, nameof(oldChild));

            _list.RemoveAt(index);
            oldChild.OwnerDocument?.ClearId(oldChild);
            HtmlDocument.RemoveIntrinsicElement(oldChild.OwnerDocument, oldChild as HtmlElement);
            oldChild.ParentNode = null;
        }
        else
            throw new ArgumentException(null, nameof(oldChild));

        _list.Insert(index, newChild);
        newChild.OwnerDocument?.SetNodeById(newChild);
        newChild.ParentNode = _parent;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newChild, oldChild));
    }

    public void RemoveAll()
    {
        foreach (var node in _list)
        {
            HtmlDocument.RemoveIntrinsicElement(node.OwnerDocument, node as HtmlElement);
            if (node.ParentNode != _parent)
                throw new InvalidOperationException();

            node.ParentNode = null;
        }
        RemoveAllNoCheck();
    }

    public void Insert(int index, HtmlNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.ParentNode != null)
            throw new ArgumentException(null, nameof(node));

        HtmlDocument.RemoveIntrinsicElement(node.OwnerDocument, node as HtmlElement);
        _list.Insert(index, node);
        node.OwnerDocument?.SetNodeById(node);
        node.ParentNode = _parent;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, index));
    }

    public void AddRange(IEnumerable<HtmlNode> nodes)
    {
        if (nodes == null)
            return;

        foreach (var node in nodes)
        {
            Add(node);
        }
    }

    internal void AddNoCheck(HtmlNode node)
    {
        _list.Add(node);
        node.OwnerDocument?.SetNodeById(node);
        node.ParentNode = _parent;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node));
    }

    public void Add(HtmlNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.ParentNode != null)
            throw new ArgumentException(null, nameof(node));

        AddNoCheck(node);
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _list.Count)
            return false;

        var node = _list[index];
        if (node.ParentNode != _parent)
            throw new ArgumentException(null, nameof(index));

        _list.RemoveAt(index);
        node.OwnerDocument?.ClearId(node);
        HtmlDocument.RemoveIntrinsicElement(node.OwnerDocument, node as HtmlElement);
        node.ParentNode = null;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node, index));
        return true;
    }

    public bool Remove(HtmlNode? node)
    {
        if (node == null)
            return false;

        var index = _list.IndexOf(node);
        if (index < 0)
            return false;

        var existing = _list[index];
        if (existing.ParentNode != _parent)
            throw new ArgumentException(null, nameof(node));

        _list.RemoveAt(index);
        node?.OwnerDocument?.ClearId(node);
        HtmlDocument.RemoveIntrinsicElement(node?.OwnerDocument, node as HtmlElement);
        node?.ParentNode = null;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node));
        return true;
    }

    internal void RemoveAllNoCheck()
    {
        _parent.OwnerDocument?.ClearIds(_list);
        _list.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        _parent.ClearCaches();
        CollectionChanged?.Invoke(this, e);
    }

    bool ICollection<HtmlNode>.IsReadOnly => false;
    public int IndexOf(HtmlNode item) => _list.IndexOf(item);
    public bool Contains(HtmlNode item) => IndexOf(item) >= 0;
    public void CopyTo(HtmlNode[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
    public IEnumerator<HtmlNode> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    void IList<HtmlNode>.RemoveAt(int index) => RemoveAt(index);
    void ICollection<HtmlNode>.Clear() => RemoveAll();
}
