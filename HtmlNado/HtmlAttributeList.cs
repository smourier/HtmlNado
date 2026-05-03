namespace HtmlNado;

public sealed class HtmlAttributeList : ICollection<HtmlAttribute>, INotifyCollectionChanged, IList
{
    private readonly List<HtmlAttribute> _list = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    internal HtmlAttributeList(HtmlNode parent)
    {
        Parent = parent;
    }

    public HtmlNode Parent { get; private set; }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        Parent.ClearCaches();
        CollectionChanged?.Invoke(this, e);
    }

    public HtmlAttribute Add(string prefix, string localName, string namespaceURI) => Add(prefix, localName, namespaceURI, null);
    public HtmlAttribute Add(string prefix, string localName, string namespaceURI, string value)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(localName);
        var owner = (Parent?.OwnerDocument) ?? throw new InvalidOperationException();

        if (string.IsNullOrWhiteSpace(prefix) && !string.IsNullOrWhiteSpace(namespaceURI))
        {
            prefix = owner.GetPrefixOfNamespace(namespaceURI);
        }

        var att = owner.CreateAttribute(prefix, localName, namespaceURI);
        att.Value = value;
        Add(att);
        return att;
    }

    public HtmlAttribute Add(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);

        var owner = (Parent?.OwnerDocument) ?? throw new InvalidOperationException();
        var att = owner.CreateAttribute(string.Empty, name, string.Empty);
        att.Value = value;
        Add(att);
        return att;
    }

    public void Add(HtmlAttribute attribute, bool replace = true)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        if (attribute.ParentNode != null)
            throw new ArgumentException(null, nameof(attribute));

        var att = this[attribute.LocalName, attribute.NamespaceURI];
        if (att != null)
        {
            if (!replace)
                throw new ArgumentException("The same attribute (" + att.NamespaceURI + ":" + att.LocalName + ") has has already been added.", nameof(attribute));

            Remove(att);
        }

        AddNoCheck(attribute);
    }

    internal void AddNoCheck(HtmlAttribute attribute)
    {
        _list.Add(attribute);
        attribute.ParentNode = Parent;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, attribute));
    }

    public string? GetNamespacePrefixIfDefined(string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(namespaceURI);

        foreach (var att in _list)
        {
            if ((string.Equals(att.Name, HtmlNode.XmlnsPrefix, StringComparison.Ordinal) ||
                string.Equals(att.Prefix, HtmlNode.XmlnsPrefix, StringComparison.Ordinal)) && string.Equals(att.Value, namespaceURI, StringComparison.Ordinal))
                return att.LocalName;
        }
        return null;
    }

    public void RemoveAll()
    {
        foreach (var att in _list)
        {
            if (att.ParentNode != Parent)
                throw new InvalidOperationException();

            att.ParentNode = null;
        }

        _list.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void Insert(int index, HtmlAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        if (attribute.ParentNode != null)
            throw new ArgumentException(null, nameof(attribute));

        _list.Insert(index, attribute);
        attribute.ParentNode = Parent;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, attribute, index));
    }

    public bool Contains(HtmlAttribute item) => IndexOf(item) >= 0;
    public void CopyTo(HtmlAttribute[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
    public int IndexOf(HtmlAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return _list.IndexOf(attribute);
    }

    public int IndexOf(string name) => _list.FindIndex(a => name.EqualsIgnoreCase(a.Name));
    public int IndexOf(string localName, string namespaceURI)
    {
        if (localName == null || namespaceURI == null)
            return -1;

        return _list.FindIndex(a => localName.EqualsIgnoreCase(a.LocalName) && a.NamespaceURI != null && string.Equals(namespaceURI, a.NamespaceURI, StringComparison.Ordinal));
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _list.Count)
            return false;

        var att = _list[index];
        if (att.ParentNode != Parent)
            throw new ArgumentException(null, nameof(index));

        _list.RemoveAt(index);
        att.ParentNode = null;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, att, index));
        return true;
    }

    public void RemoveRange(IEnumerable<HtmlAttribute> attributes)
    {
        if (attributes == null)
            return;

        foreach (var att in attributes)
        {
            Remove(att);
        }
    }

    public bool RemoveByPrefix(string prefix, string localName)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(localName);
        var att = _list.Find(a => localName.EqualsIgnoreCase(a.LocalName) && string.Equals(prefix, a.Prefix, StringComparison.Ordinal));
        if (att == null)
            return false;

        return Remove(att);
    }

    public bool Remove(string localName, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(namespaceURI);
        var att = this[localName, namespaceURI];
        if (att == null)
            return false;

        return Remove(att);
    }

    public bool Remove(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var att = this[name];
        if (att == null)
            return false;

        return Remove(att);
    }

    public bool Remove(HtmlAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        if (attribute.ParentNode != Parent)
            throw new ArgumentException(null, nameof(attribute));

        if (!_list.Remove(attribute))
            throw new ArgumentException(null, nameof(attribute));

        attribute.ParentNode = null;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, attribute));
        return true;
    }

    public HtmlAttribute? this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);
            return _list.Find(a => name.EqualsIgnoreCase(a.Name));
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.ParentNode != null)
                throw new ArgumentException(null, nameof(value));

            var index = IndexOf(name);
            if (index < 0)
            {
                AddNoCheck(value);
            }
            else
            {
                this[index] = value;
            }
        }
    }

    public HtmlAttribute? this[string localName, string namespaceURI]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(localName);
            ArgumentNullException.ThrowIfNull(namespaceURI);
            return _list.Find(a => localName.EqualsIgnoreCase(a.LocalName) && a.NamespaceURI != null && string.Equals(namespaceURI, a.NamespaceURI, StringComparison.Ordinal));
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.ParentNode != null)
                throw new ArgumentException(null, nameof(value));

            var index = IndexOf(localName, namespaceURI);
            if (index < 0)
            {
                AddNoCheck(value);
            }
            else
            {
                this[index] = value;
            }
        }
    }

    public HtmlAttribute this[int index]
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

    public int Count => _list.Count;

    public IEnumerator<HtmlAttribute> GetEnumerator() => _list.GetEnumerator();

    int IList.Add(object? value)
    {
        var count = Count;
        Add((HtmlAttribute)value);
        return count;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    bool ICollection<HtmlAttribute>.IsReadOnly => false;
    void ICollection<HtmlAttribute>.Clear() => RemoveAll();
    void IList.Clear() => RemoveAll();
    bool IList.Contains(object value) => Contains((HtmlAttribute)value);
    int IList.IndexOf(object value) => IndexOf((HtmlAttribute)value);
    void IList.Insert(int index, object value) => Insert(index, (HtmlAttribute)value);
    bool IList.IsFixedSize => false;
    bool IList.IsReadOnly => false;
    void IList.Remove(object value) => Remove((HtmlAttribute)value);
    void IList.RemoveAt(int index) => RemoveAt(index);
    object IList.this[int index] { get => this[index]; set => this[index] = (HtmlAttribute)value; }
    void ICollection.CopyTo(Array array, int index) => ((ICollection)_list).CopyTo(array, index);
    bool ICollection.IsSynchronized => ((ICollection)_list).IsSynchronized;
    object ICollection.SyncRoot => ((ICollection)_list).SyncRoot;
}
