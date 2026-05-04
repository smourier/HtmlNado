namespace HtmlNado;

public sealed class HtmlNodeList : ICollection<HtmlNode>, INotifyCollectionChanged, IList
{
    private readonly List<HtmlNode> _list = [];
    private readonly HtmlNode _parent;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    internal HtmlNodeList(HtmlNode parent)
    {
        _parent = parent;
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        _parent.ClearCaches();
        CollectionChanged?.Invoke(this, e);
    }

    public HtmlNode? this[string? name]
    {
        get
        {
            return _list.Find(n => n.Name.EqualsIgnoreCase(name));
        }
    }

    public HtmlNode? this[string? localName, string? namespaceURI]
    {
        get
        {
            return _list.Find(a => localName.EqualsIgnoreCase(a.LocalName) && a.NamespaceURI != null && string.Equals(namespaceURI, a.NamespaceURI, StringComparison.Ordinal));
        }
    }

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

    internal void RemoveAllNoCheck()
    {
        _parent.OwnerDocument?.ClearIds(_list);
        _list.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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

    public bool Remove(HtmlNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var index = _list.IndexOf(node);
        if (index < 0)
            return false;

        var existing = _list[index];
        if (existing.ParentNode != _parent)
            throw new ArgumentException(null, nameof(node));

        _list.RemoveAt(index);
        node?.OwnerDocument?.ClearId(node);
        HtmlDocument.RemoveIntrinsicElement(node.OwnerDocument, node as HtmlElement);
        node.ParentNode = null;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node));
        return true;
    }

    public int Count => _list.Count;
    public int IndexOf(HtmlNode item) => _list.IndexOf(item);
    public bool Contains(HtmlNode item) => IndexOf(item) >= 0;
    public void CopyTo(HtmlNode[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
    public IEnumerator<HtmlNode> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    bool ICollection<HtmlNode>.IsReadOnly => false;
    void ICollection<HtmlNode>.Clear() => RemoveAll();
    int IList.Add(object value)
    {
        int count = Count;
        Add((HtmlNode)value);
        return count;
    }

    void IList.Clear() => RemoveAll();
    bool IList.Contains(object value) => Contains((HtmlNode)value);
    int IList.IndexOf(object value) => IndexOf((HtmlNode)value);
    void IList.Insert(int index, object value) => Insert(index, (HtmlNode)value);
    bool IList.IsFixedSize => false;
    bool IList.IsReadOnly => false;
    void IList.Remove(object value) => Remove((HtmlNode)value);
    void IList.RemoveAt(int index) => RemoveAt(index);
    object IList.this[int index] { get => this[index]; set => this[index] = (HtmlNode)value; }
    void ICollection.CopyTo(Array array, int index) => ((ICollection)_list).CopyTo(array, index);
    bool ICollection.IsSynchronized => ((ICollection)_list).IsSynchronized;
    object ICollection.SyncRoot => ((ICollection)_list).SyncRoot;
}
