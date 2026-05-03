namespace HtmlNado.Css;

public class CssFunction : CssComponentValue
{
    private readonly List<CssComponentValue> _values = [];
    private readonly string _name;

    public CssFunction(string name)
        : base(null)
    {
        ArgumentNullException.ThrowIfNull(name);

        _name = name;
    }

    public CssFunction(FunctionToken token)
        : base(token.Info)
    {
        Token = token;
    }

    public FunctionToken Token { get; }
    public virtual string Name => Token != null ? Token.Name : _name;

    public virtual IList<CssComponentValue> Values => _values;

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(Name);
        formatter.Write('(');

        // TODO: change this
        formatter.Write(ToString(_values));
        formatter.Write(')');
    }
}
