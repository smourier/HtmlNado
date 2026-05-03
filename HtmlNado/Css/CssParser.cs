namespace HtmlNado.Css;

public class CssParser
{
    public event EventHandler<CssParseErrorEventArgs> ParseError;

    private CssComponentValue _reconsumed;

    public CssParser(string filePath, Encoding encoding)
        : this(new StreamReader(filePath, encoding))
    {
    }

    public CssParser(Stream stream, Encoding encoding)
        : this(new StreamReader(stream, encoding))
    {
    }

    public CssParser(TextReader reader)
        : this(new CssReaderTokenizer(reader))
    {
    }

    public CssParser(CssTokenizer tokenizer)
    {
        ArgumentNullException.ThrowIfNull(tokenizer);

        Tokenizer = tokenizer;
        Tokenizer.ParseError += OnTokenizerParseError;
    }

    public CssTokenizer Tokenizer { get; }

    public CssStyleSheet Parse()
    {
        var ss = new CssStyleSheet();
        foreach (var rule in ConsumeRuleList(true))
        {
            ss.Rules.Add(rule);
        }
        return ss;
    }

    protected virtual void OnTokenizerParseError(object sender, CssParseErrorEventArgs e) => OnParseError(sender, e);
    protected virtual void OnParseError(object sender, CssParseErrorEventArgs e) => ParseError?.Invoke(sender, e);
    protected virtual void OnParseError(CssParseError error) => OnParseError(this, new CssParseErrorEventArgs(error));

    private void Reconsume(CssComponentValue token) => _reconsumed = token;

    private CssComponentValue Consume()
    {
        if (_reconsumed != null)
        {
            var tk = _reconsumed;
            _reconsumed = null;
            return tk;
        }
        return Tokenizer.Consume();
    }

    private static CssDeclarationList ConsumeDeclarationList(LiteralToken token, IEnumerable<CssComponentValue> list, EventHandler<CssParseErrorEventArgs> parseError)
    {
        var tokenizer = new CssEnumerableTokenizer(list);
        var parser = new CssParser(tokenizer);
        if (parseError != null)
        {
            parser.ParseError += (sender, e) => parseError(sender, e);
        }
        return parser.ConsumeDeclarationList(token);
    }

    private CssDeclarationList ConsumeDeclarationList(LiteralToken token)
    {
        var decList = new CssDeclarationList(token);
        do
        {
            var cv = Consume();
            if (cv == null)
                break;

            if (cv is WhitespaceToken)
                continue;

            if (cv is LiteralToken lt && lt.Value == ';')
                continue;

            if (cv is AtKeywordToken atk)
            {
                Reconsume(cv);
                var rule = ConsumeAtRule(atk.Name);

                // REVIEW: I don' get the spec here... "Consume an at-rule. Append the returned rule to the list of declarations." ??
                var tokenizer = new CssEnumerableTokenizer(rule.Block.Values);
                var parser = new CssParser(tokenizer);
                parser.ParseError += (sender, e) => OnParseError(sender, e);
                var dec = parser.ConsumeDeclaration(atk.Name);
                if (dec != null)
                {
                    decList.Add(dec);
                }
                continue;
            }

            if (cv is IdentToken it)
            {
                var list = new List<CssComponentValue>();
                do
                {
                    cv = Consume();
                    if (cv == null)
                        break;

                    lt = cv as LiteralToken;
                    if (lt != null && lt.Value == ';')
                        break;

                    list.Add(cv);
                }
                while (true);

                if (list.Count > 0)
                {
                    var tokenizer = new CssEnumerableTokenizer(list);
                    var parser = new CssParser(tokenizer);
                    parser.ParseError += (sender, e) => OnParseError(sender, e);
                    var dec = parser.ConsumeDeclaration(it.Name);
                    if (dec != null)
                    {
                        decList.Add(dec);
                    }
                }
                continue;
            }

            OnParseError(new CssParseError(cv.Info) { Value = cv, Type = CssParseErrorType.ParserUnexpectedToken });
            do
            {
                cv = Consume();
                if (cv == null)
                    break;

                lt = cv as LiteralToken;
                if (lt != null && lt.Value == ';')
                    break;
            }
            while (true);
        }
        while (true);
        return decList;
    }

    private CssDeclaration ConsumeDeclaration(string name)
    {
        var dec = new CssDeclaration(name);
        var cv = Consume();
        while (cv is WhitespaceToken)
        {
            cv = Consume();
        }

        if (cv is not LiteralToken lt || lt.Value != ':')
        {
            OnParseError(new CssParseError(cv.Info) { Value = cv, Type = CssParseErrorType.ParserColonExpected });
            return null;
        }

        do
        {
            cv = Consume();
            if (cv == null)
                break;

            dec.Values.Add(cv);
        }
        while (true);

        IdentToken it = null;
        DelimToken dt = null;
        for (int i = dec.Values.Count - 1; i >= 0; i--)
        {
            if (dec.Values[i] is WhitespaceToken)
                continue;

            if (it == null)
            {
                it = dec.Values[i] as IdentToken;
                if (it == null || string.Compare(it.Name, "important", StringComparison.OrdinalIgnoreCase) != 0)
                {
                    it = null;
                    break;
                }
            }
            else if (dt == null)
            {
                dt = dec.Values[i] as DelimToken;
                if (dt == null || dt.Value != '!')
                {
                    dt = null;
                    break;
                }
            }
        }

        if (it != null && dt != null)
        {
            dec.Values.Remove(it);
            dec.Values.Remove(dt);
            dec.IsImportant = true;
        }

        return dec;
    }

    private List<CssRule> ConsumeRuleList(bool topLevel)
    {
        var rules = new List<CssRule>();
        do
        {
            var cv = Consume();
            if (cv == null)
                break;

            if (cv is WhitespaceToken)
                continue;

            CssRule rule;
            if (cv is CdoToken || cv is CdcToken)
            {
                if (topLevel)
                    continue;

                Reconsume(cv);
                rule = ConsumeQualifiedRule();
                if (rule != null)
                {
                    rules.Add(rule);
                }
                continue;
            }

            if (cv is AtKeywordToken atk)
            {
                Reconsume(cv);
                rule = ConsumeAtRule(atk.Name);
                rules.Add(rule);
                continue;
            }

            Reconsume(cv);
            rule = ConsumeQualifiedRule();
            if (rule != null)
            {
                rules.Add(rule);
            }
        }
        while (true);
        return rules;
    }

    private CssQualifiedRule ConsumeQualifiedRule()
    {
        var rule = new CssQualifiedRule();
        CssComponentValue prevCv = null;
        do
        {
            var cv = Consume();
            if (cv == null)
            {
                OnParseError(new CssParseError(prevCv?.Info) { Value = prevCv, Type = CssParseErrorType.ParserUnexpectedEof });
                return null;
            }

            if (cv is LiteralToken lt && lt.Value == '{')
            {
                var sb = ConsumeSimpleBlock(lt);
                rule.Declarations = ConsumeDeclarationList(lt, sb.Values, OnParseError);
                break;
            }

            if (cv is CssSimpleBlock block)
            {
                rule.Declarations = ConsumeDeclarationList(block.Token, block.Values, OnParseError);
                return rule;
            }

            Reconsume(cv);
            cv = ConsumeComponentValue();
            rule.Prelude.Add(cv);
            prevCv = cv;
        }
        while (true);
        return rule;
    }

    private CssAtRule ConsumeAtRule(string name)
    {
        var rule = new CssAtRule(name);
        do
        {
            var cv = Consume();
            if (cv == null)
                break;

            if (cv is LiteralToken lt)
            {
                if (lt.Value == ';')
                    break;

                if (lt.Value == '{')
                {
                    rule.Block = ConsumeSimpleBlock(lt);
                    break;
                }
            }

            if (cv is CssSimpleBlock block)
            {
                rule.Block = block;
                return rule;
            }

            Reconsume(cv);
            cv = ConsumeComponentValue();
            rule.Prelude.Add(cv);
        }
        while (true);

        return rule;
    }

    private CssComponentValue ConsumeComponentValue()
    {
        var cv = Consume();
        if (cv is LiteralToken lt)
        {
            if (lt.Value == '{' || lt.Value == '[' || lt.Value == '(')
                return ConsumeSimpleBlock(lt);
        }

        if (cv is FunctionToken ft)
            return ConsumeFunction(ft);

        return cv;
    }

    private CssSimpleBlock ConsumeSimpleBlock(LiteralToken lt)
    {
        var values = new List<CssComponentValue>();
        do
        {
            var cv = Consume();
            if (cv == null)
                break;

            if (cv is LiteralToken et && et.Value == lt.EndingToken.Value)
                break;

            Reconsume(cv);
            cv = ConsumeComponentValue();
            values.Add(cv);
        }
        while (true);

        return new CssSimpleBlock(lt, values);
    }

    private CssFunction ConsumeFunction(FunctionToken ft)
    {
        var fn = new CssFunction(ft);
        do
        {
            var cv = Consume();
            if (cv == null)
                break;

            if (cv is LiteralToken et && et.Value == ')')
                break;

            Reconsume(cv);
            cv = ConsumeComponentValue();
            fn.Values.Add(cv);
        }
        while (true);
        return fn;
    }
}
