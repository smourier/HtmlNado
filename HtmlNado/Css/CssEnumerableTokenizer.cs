namespace HtmlNado.Css;

public class CssEnumerableTokenizer : CssTokenizer
{
    private readonly IEnumerator<CssComponentValue> _enumerator;

    public CssEnumerableTokenizer(IEnumerable<CssComponentValue> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        _enumerator = enumerable.GetEnumerator();
    }

    public override CssComponentValue Consume()
    {
        if (!_enumerator.MoveNext())
            return null;

        return _enumerator.Current;
    }
}
