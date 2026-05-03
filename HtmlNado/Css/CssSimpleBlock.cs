namespace HtmlNado.Css;

public class CssSimpleBlock : CssComponentValue
{
    private readonly List<CssComponentValue> _values;

    public CssSimpleBlock(LiteralToken token)
        : this(token, [])
    {
    }

    internal CssSimpleBlock(LiteralToken token, List<CssComponentValue> values)
        : base(token.Info)
    {
        ArgumentNullException.ThrowIfNull(token);

        Token = token;
        _values = values;
    }

    public LiteralToken Token { get; }
    public virtual IList<CssComponentValue> Values => _values;

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (Token.Value == '{')
        {
            switch (formatter.BraceStyle)
            {
                case CssFormatBraceStyle.C:
                    formatter.Write(Token.Value);
                    formatter.Write(Environment.NewLine);
                    formatter.IndentationLevel++;
                    break;

                case CssFormatBraceStyle.Pascal:
                    formatter.Write(Environment.NewLine);
                    formatter.Write(Token.Value);
                    formatter.IndentationLevel++;
                    break;

                default:
                    formatter.Write(Token.Value);
                    break;
            }
        }
        else
        {
            formatter.Write(Token.Value);
        }

        for (int i = 0; i < _values.Count; i++)
        {
            var cv = _values[i];
            cv.Write(formatter);
        }

        var endingToken = Token.EndingToken != null ? Token.EndingToken.Value : Token.Value;
        if (Token.Value == '{')
        {
            switch (formatter.BraceStyle)
            {
                case CssFormatBraceStyle.C:
                case CssFormatBraceStyle.Pascal:
                    formatter.IndentationLevel--;
                    formatter.Write(Environment.NewLine);
                    formatter.Write(endingToken);
                    break;

                default:
                    formatter.Write(endingToken);
                    break;
            }
        }
        else
        {
            formatter.Write(endingToken);
        }
    }
}
