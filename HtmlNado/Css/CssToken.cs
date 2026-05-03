namespace HtmlNado.Css;

public abstract class CssToken : CssComponentValue
{
    protected CssToken(CssTokenizerInfo info)
        : base(info)
    {
    }

    public virtual string Comment { get; set; }
}

public abstract class IdentLikeToken : CssToken
{
    protected IdentLikeToken(CssTokenizerInfo info, string name)
        : base(info)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    public string Name { get; internal set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(Name);
    }
}

public class IdentToken : IdentLikeToken
{
    public IdentToken(string name)
        : this(null, name)
    {
    }

    public IdentToken(CssTokenizerInfo info, string name)
        : base(info, name)
    {
    }
}

public class FunctionToken : IdentLikeToken
{
    public FunctionToken(string name)
        : this(null, name)
    {
    }

    public FunctionToken(CssTokenizerInfo info, string name)
        : base(info, name)
    {
    }
}

public class UrlToken : IdentLikeToken
{
    public UrlToken()
        : this(null)
    {
    }

    public UrlToken(CssTokenizerInfo info)
        : base(info, string.Empty)
    {
    }

    public virtual bool IsBad { get; set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        // TODO: escape )
        formatter.Write("url(");
        formatter.Write(Name);
        formatter.Write(')');
    }
}

public class CdcToken : CssToken
{
    public CdcToken()
        : this(null)
    {
    }

    public CdcToken(CssTokenizerInfo info)
        : base(info)
    {
    }
}

public class CdoToken : CssToken
{
    public CdoToken()
        : this(null)
    {
    }

    public CdoToken(CssTokenizerInfo info)
        : base(info)
    {
    }
}

public class HashToken : CssToken
{
    public HashToken()
        : this(null)
    {
    }

    public HashToken(CssTokenizerInfo info)
        : base(info)
    {
    }

    public virtual HashTypeFlag TypeFlag { get; set; }
    public virtual string Value { get; set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write('#');
        formatter.Write(Value);
    }
}

public enum HashTypeFlag
{
    Unrestricted,
    Id,
}

public class AtKeywordToken : CssToken
{
    public AtKeywordToken(string name)
        : this(null, name)
    {
    }

    public AtKeywordToken(CssTokenizerInfo info, string name)
        : base(info)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    public string Name { get; private set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write('@');
        formatter.Write(Name);
    }
}

public class DelimToken : LiteralToken
{
    public DelimToken(char value)
        : this(null, value)
    {
    }

    public DelimToken(CssTokenizerInfo info, char value)
        : base(info, value)
    {
    }
}

public class LiteralToken : CssToken
{
    private LiteralToken _endingToken;

    public LiteralToken(char value)
        : this(null, value)
    {
    }

    public LiteralToken(CssTokenizerInfo info, char value)
        : base(info)
    {
        Value = value;
    }

    public char Value { get; private set; }

    public virtual LiteralToken EndingToken
    {
        get
        {
            if (_endingToken == null)
            {
                switch (Value)
                {
                    case '[':
                        _endingToken = new LiteralToken(null, ']');
                        break;

                    case '(':
                        _endingToken = new LiteralToken(null, ')');
                        break;

                    case '{':
                        _endingToken = new LiteralToken(null, '}');
                        break;
                }
            }
            return _endingToken;
        }
    }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(Value);
    }
}

public class UnicodeRangeToken : CssToken
{
    public UnicodeRangeToken()
        : this(null)
    {
    }

    public UnicodeRangeToken(CssTokenizerInfo info)
        : base(info)
    {
    }

    public virtual int RangeStart { get; set; }
    public virtual int RangeEnd { get; set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (RangeEnd == RangeStart)
        {
            formatter.Write(string.Format("U+{0:X6}", RangeStart));
            return;
        }

        formatter.Write(string.Format("U+{0:X6}-U+{1:X6}", RangeStart, RangeEnd));
    }
}

public class ColumnToken : CssToken
{
    public ColumnToken()
        : this(null)
    {
    }

    public ColumnToken(CssTokenizerInfo info)
        : base(info)
    {
    }
}

public class IncludeMatchToken : CssToken
{
    public IncludeMatchToken()
        : this(null)
    {
    }

    public IncludeMatchToken(CssTokenizerInfo info)
        : base(info)
    {
    }
}

public class DashMatchToken : CssToken
{
    public DashMatchToken()
        : this(null)
    {
    }

    public DashMatchToken(CssTokenizerInfo info)
        : base(info)
    {
    }
}

public class SubstringMatchToken : CssToken
{
    public SubstringMatchToken()
        : this(null)
    {
    }

    public SubstringMatchToken(CssTokenizerInfo info)
        : base(info)
    {
    }
}

public class PrefixMatchToken : CssToken
{
    public PrefixMatchToken()
        : this(null)
    {
    }

    public PrefixMatchToken(CssTokenizerInfo info)
        : base(info)
    {
    }
}

public class SuffixMatchToken : CssToken
{
    public SuffixMatchToken()
        : this(null)
    {
    }

    public SuffixMatchToken(CssTokenizerInfo info)
        : base(info)
    {
    }
}

public class WhitespaceToken : CssToken
{
    public WhitespaceToken()
        : this(null)
    {
    }

    public WhitespaceToken(CssTokenizerInfo info)
        : base(info)
    {
    }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(' ');
    }
}

public enum NumberTypeFlag
{
    Integer,
    Number,
}

public abstract class NumericToken : CssToken
{
    protected NumericToken(CssTokenizerInfo info, CssNumber number)
        : base(info)
    {
        ArgumentNullException.ThrowIfNull(number);

        Number = number;
    }

    public CssNumber Number { get; private set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(Number.Representation);
    }
}

public class NumberToken : NumericToken
{
    public NumberToken(CssNumber number)
        : this(null, number)
    {
    }

    public NumberToken(CssTokenizerInfo info, CssNumber number)
        : base(info, number)
    {
    }
}

public class PercentageToken : NumericToken
{
    public PercentageToken(CssNumber number)
        : this(null, number)
    {
    }

    public PercentageToken(CssTokenizerInfo info, CssNumber number)
        : base(info, number)
    {
    }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(Number + "%");
    }
}

public class DimensionToken : NumericToken
{
    public DimensionToken(CssNumber number)
        : this(null, number)
    {
    }

    public DimensionToken(CssTokenizerInfo info, CssNumber number)
        : base(info, number)
    {
        Unit = string.Empty;
    }

    public virtual string Unit { get; set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(Number.Representation);
        if (Unit != null)
        {
            formatter.Write(Unit);
        }
    }
}

public class StringToken : CssToken
{
    public StringToken(char quote)
        : this(null, quote)
    {
    }

    public StringToken(CssTokenizerInfo info, char quote)
        : base(info)
    {
        Quote = quote;
    }

    public virtual bool IsBad { get; set; }
    public char Quote { get; private set; }
    public virtual string Value { get; set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(Quote);
        if (Value != null)
        {
            formatter.Write(Value);
        }

        formatter.Write(Quote);
    }
}
