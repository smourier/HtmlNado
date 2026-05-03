namespace HtmlNado.Css;

public class CssDeclarationList : CssSimpleBlock, IList<CssDeclaration>
{
    private readonly List<CssDeclaration> _declarations = [];

    internal CssDeclarationList(LiteralToken token)
        : base(token)
    {
    }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        switch (formatter.BraceStyle)
        {
            case CssFormatBraceStyle.C:
                formatter.Write('{');
                formatter.Write(Environment.NewLine);
                formatter.IndentationLevel++;
                break;

            case CssFormatBraceStyle.Pascal:
                formatter.Write(Environment.NewLine);
                formatter.Write('{');
                formatter.IndentationLevel++;
                break;

            default:
                formatter.Write('{');
                break;
        }

        for (int i = 0; i < _declarations.Count; i++)
        {
            var cv = _declarations[i];
            cv.Write(formatter);
            if (i != (_declarations.Count - 1))
            {
                formatter.Write(Environment.NewLine);
            }
        }

        switch (formatter.BraceStyle)
        {
            case CssFormatBraceStyle.C:
            case CssFormatBraceStyle.Pascal:
                formatter.IndentationLevel--;
                formatter.Write(Environment.NewLine);
                formatter.Write('}');
                break;

            default:
                formatter.Write('}');
                break;
        }
    }

    public int IndexOf(CssDeclaration item) => _declarations.IndexOf(item);
    public void Insert(int index, CssDeclaration item) => _declarations.Insert(index, item);
    public void RemoveAt(int index) => _declarations.RemoveAt(index);
    public CssDeclaration this[int index] { get => _declarations[index]; set => _declarations[index] = value; }
    public void Add(CssDeclaration item) => _declarations.Add(item);
    public void Clear() => _declarations.Clear();
    public bool Contains(CssDeclaration item) => _declarations.Contains(item);
    public void CopyTo(CssDeclaration[] array, int arrayIndex) => _declarations.CopyTo(array, arrayIndex);
    public int Count => _declarations.Count;
    public bool Remove(CssDeclaration item) => _declarations.Remove(item);
    public IEnumerator<CssDeclaration> GetEnumerator() => _declarations.GetEnumerator();

    bool ICollection<CssDeclaration>.IsReadOnly => ((IList<CssDeclaration>)_declarations).IsReadOnly;
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
