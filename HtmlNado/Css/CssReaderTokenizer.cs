namespace HtmlNado.Css;

public class CssReaderTokenizer : CssTokenizer
{
    public const char ReplacementCharacter = '\uFFFD';
    public const int MaximumCodePoint = 0x10FFFF;
    private const int _readAheadSize = 3;
    private const string _hexDigits = "0123456789ABCDEF";

    private readonly CircularArray<char> _buffer;

    public CssReaderTokenizer(string filePath, Encoding encoding)
        : this(new StreamReader(filePath, encoding))
    {
    }

    public CssReaderTokenizer(Stream stream, Encoding encoding)
        : this(new StreamReader(stream, encoding))
    {
    }

    public CssReaderTokenizer(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        Line = 1;
        Column = 1;
        BaseReader = reader;
        _buffer = new CircularArray<char>(_readAheadSize + 1);
    }

    private class CircularArray<T>
    {
        private readonly T[] _array;
        private int _index;

        public CircularArray(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException(null, nameof(capacity));

            _array = new T[capacity];
        }

        public int Count { get; private set; }

        public void AppendToEnd(T item)
        {
            if (Count == _array.Length)
                throw new InvalidOperationException();

            int index = (_index + Count) % _array.Length;
            _array[index] = item;
            Count++;
        }

        public void InsertAtBeginning(T item)
        {
            if (Count == _array.Length)
                throw new InvalidOperationException();

            _index = _index == 0 ? _array.Length - 1 : _index - 1;
            _array[_index] = item;
            Count++;
        }

        public void Read(T[] items, bool remove)
        {
            if (Count < items.Length)
                throw new InvalidOperationException();

            int index = _index;
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = _array[index++];
                if (index == _array.Length)
                {
                    index = 0;
                }
            }

            if (remove)
            {
                Count -= items.Length;
                _index = index;
            }
        }
    }

    public int Line { get; private set; }
    public int Column { get; private set; }
    public TextReader BaseReader { get; private set; }

    private static bool IsNewline(char cp) => cp == '\n'; // only this because look & consume next handles others cases

    private static bool IsNonPrintable(char cp)
    {
        if (cp >= '\0' && cp <= '\u0008')
            return true;

        if (cp == '\u000b')
            return true;

        if (cp >= '\u000e' && cp <= '\u001f')
            return true;

        if (cp == '\u007F')
            return true;

        return false;
    }

    private static bool IsAscii(char cp) => ((short)cp) < 0x80;
    private static bool IsNonAscii(char cp) => !IsAscii(cp);
    private static bool IsDigit(char cp) => cp >= '0' && cp <= '9';
    private static bool IsLetter(char cp) => (cp >= 'a' && cp <= 'z') || (cp >= 'A' && cp <= 'Z');
    private static bool IsNameStart(char cp) => IsLetter(cp) || IsNonAscii(cp) || cp == '_';
    private static bool IsName(char cp) => IsNameStart(cp) || IsDigit(cp) || cp == '-';
    private bool IsName() => TryLookNextCodePoint(out char cp) && IsName(cp);
    private static bool IsWhitespace(char cp) => IsNewline(cp) || cp == ' ' || cp == '\t';
    private static bool IsHexDigit(char cp) => IsHexDigit(cp, out _);

    private static bool IsHexDigit(char cp, out int hexValue)
    {
        int pos = _hexDigits.IndexOf(cp);
        hexValue = pos >= 0 ? pos : 0;
        return pos >= 0;
    }

    private static bool StartsValidEscape(char cp1, char cp2)
    {
        if (cp1 != '\\')
            return false;

        return cp2 != '\n';
    }

    private bool StartsValidEscape()
    {
        if (!TryLookNextCodePoint(out char cp1, out char cp2))
            return false;

        return StartsValidEscape(cp1, cp2);
    }

    private bool StartsIdentifier()
    {
        if (!TryLookNextCodePoint(out char cp1, out char cp2, out char cp3))
            return false;

        return StartsIdentifier(cp1, cp2, cp3);
    }

    private static bool StartsIdentifier(char cp1, char cp2, char cp3)
    {
        if (cp1 == '-')
            return IsNameStart(cp2) || StartsValidEscape(cp2, cp3);

        if (IsNameStart(cp1))
            return true;

        if (cp1 == '\\')
            return StartsValidEscape(cp1, cp2);

        return false;
    }

    private bool StartsNumber()
    {
        if (!TryLookNextCodePoint(out char cp1, out char cp2, out char cp3))
            return false;

        return StartsNumber(cp1, cp2, cp3);
    }

    private static bool StartsNumber(char cp1, char cp2, char cp3)
    {
        if (cp1 == '+' || cp1 == '-')
        {
            if (IsDigit(cp2))
                return true;

            if (cp2 == '.' && IsDigit(cp3))
                return true;

            return false;
        }

        if (cp1 == '.')
            return IsDigit(cp2);

        return IsDigit(cp1);
    }

    private string ConsumeEscapedCodePoint()
    {
        if (!TryConsumeNextCodePoint(out char cp))
            return ReplacementCharacter.ToString();

        if (!IsHexDigit(cp, out int hex))
            return cp.ToString();

        int value = hex;
        for (int i = 0; i < 5; i++)
        {
            if (!TryLookNextCodePoint(out cp))
                break;

            if (IsHexDigit(cp, out hex))
            {
                value = value * 16;
                value += hex;
                ConsumeNextCodePoint();
            }
            else
            {
                if (IsWhitespace(cp))
                {
                    ConsumeNextCodePoint();
                }
                break;
            }
        }
        return char.ConvertFromUtf32(value);
    }

    private CssNumber ConsumeNumber()
    {
        var number = new CssNumber();
        if (TryLookNextCodePoint(out char cp) && (cp == '+' || cp == '-'))
        {
            ConsumeNextCodePoint();
            number.Representation += cp;
        }

        do
        {
            if (!TryLookNextCodePoint(out cp) || !IsDigit(cp))
                break;

            cp = ConsumeNextCodePoint();
            number.Representation += cp;
        }
        while (true);

        if (TryLookNextCodePoint(out char nextCp1, out char nextCp2) && nextCp1 == '.' && IsDigit(nextCp2))
        {
            ConsumeNextCodePoint();
            ConsumeNextCodePoint();
            number.Representation += nextCp1.ToString() + nextCp2.ToString();
            number.TypeFlag = NumberTypeFlag.Number;

            do
            {
                if (!TryLookNextCodePoint(out cp) || !IsDigit(cp))
                    break;

                ConsumeNextCodePoint();
                number.Representation += cp;
            }
            while (true);
        }

        if (cp == 'e' || cp == 'E')
        {
            if (TryLookNextCodePoint(out nextCp1) && IsDigit(nextCp1))
            {
                ConsumeNextCodePoint();
                number.Representation += cp.ToString() + nextCp1.ToString();
                number.TypeFlag = NumberTypeFlag.Number;
                do
                {
                    cp = ConsumeNextCodePoint();
                    if (!IsDigit(cp))
                        break;

                    number.Representation += cp;
                }
                while (true);
            }
            else if (TryLookNextCodePoint(out nextCp1, out nextCp2) && (nextCp1 == '-' || nextCp1 == '+') && IsDigit(nextCp2))
            {
                ConsumeNextCodePoint();
                ConsumeNextCodePoint();
                number.Representation += cp.ToString() + nextCp1.ToString() + nextCp2.ToString();
                number.TypeFlag = NumberTypeFlag.Number;
                do
                {
                    cp = ConsumeNextCodePoint();
                    if (!IsDigit(cp))
                        break;

                    number.Representation += cp;
                }
                while (true);
            }
        }

        return number;
    }

    private void ConsumeWhitespaces()
    {
        while (IsWhitespace(LookNextCodePoint()))
        {
            _ = ConsumeNextCodePoint();
        }
    }

    private UrlToken ConsumeUrlToken()
    {
        var url = new UrlToken(CreateInfo());
        ConsumeWhitespaces();

        if (!TryLookNextCodePoint(out char cp))
            return url;

        if (cp == '"' || cp == '\'')
        {
            ConsumeNextCodePoint();
            var s = ConsumeStringToken(cp);
            if (s.IsBad)
            {
                url.IsBad = true;
                ConsumeRemnantsOfBadUrlToken();
                return url;
            }
            url.Name = s.Value;
            ConsumeWhitespaces();

            if (!TryLookNextCodePoint(out cp) || cp == ')')
            {
                ConsumeNextCodePoint();
                return url;
            }

            url.IsBad = true;
            ConsumeRemnantsOfBadUrlToken();
            return url;
        }

        do
        {
            if (!TryConsumeNextCodePoint(out cp) || cp == ')')
                return url;

            if (IsWhitespace(cp))
            {
                ConsumeWhitespaces();
                if (!TryLookNextCodePoint(out cp) || cp == ')')
                {
                    ConsumeNextCodePoint();
                    return url;
                }

                url.IsBad = true;
                ConsumeRemnantsOfBadUrlToken();
                return url;
            }

            if (cp == '"' || cp == '\'' || cp == '(' || IsNonPrintable(cp))
            {
                url.IsBad = true;
                OnParseError(url, CssParseErrorType.TokenizerBadUrl);
                ConsumeRemnantsOfBadUrlToken();
                return url;
            }

            if (cp == '\\')
            {
                if (StartsValidEscape())
                {
                    string ecp = ConsumeEscapedCodePoint();
                    url.Name += ecp;
                    continue;
                }
                else
                {
                    url.IsBad = true;
                    OnParseError(url, CssParseErrorType.TokenizerBadUrl);
                    ConsumeRemnantsOfBadUrlToken();
                    return url;
                }
            }

            url.Name += cp;
        }
        while (true);
    }

    private UnicodeRangeToken ConsumeUnicodeRangeToken()
    {
        var urt = new UnicodeRangeToken(CreateInfo());
        char cp1;
        int hex;
        int i;
        for (i = 0; i < 6; i++)
        {
            if (!TryLookNextCodePoint(out cp1))
                break;

            if (!IsHexDigit(cp1, out hex))
                break;

            urt.RangeStart *= 16;
            urt.RangeStart += hex;
            ConsumeNextCodePoint();
        }

        urt.RangeEnd = urt.RangeStart;
        bool anyWild = false;
        for (int j = i; j < 6; j++)
        {
            if (!TryLookNextCodePoint(out cp1))
                break;

            if (cp1 != '?')
                break;

            urt.RangeStart = urt.RangeStart * 16;
            urt.RangeEnd *= 16;
            urt.RangeEnd += 15;
            ConsumeNextCodePoint();
            anyWild = true;
        }
        if (anyWild)
            return urt;

        if (TryLookNextCodePoint(out cp1, out char cp2) && cp1 == '-' && IsHexDigit(cp2))
        {
            ConsumeNextCodePoint();
            i = 0;
            urt.RangeEnd = 0;
            for (i = 0; i < 6; i++)
            {
                if (!TryLookNextCodePoint(out cp1))
                    break;

                if (!IsHexDigit(cp1, out hex))
                    break;

                urt.RangeEnd *= 16;
                urt.RangeEnd += hex;
                ConsumeNextCodePoint();
            }
        }
        return urt;
    }

    private void ConsumeRemnantsOfBadUrlToken()
    {
        do
        {
            if (!TryConsumeNextCodePoint(out char cp) || cp == ')')
                break;

            if (StartsValidEscape())
            {
                ConsumeEscapedCodePoint();
            }
        }
        while (true);
    }

    private IdentLikeToken ConsumeIdentLikeToken()
    {
        string name = ConsumeName();
        if (string.Compare(name, "url", StringComparison.OrdinalIgnoreCase) == 0 && LookNextCodePoint() == '(')
        {
            ConsumeNextCodePoint();
            return ConsumeUrlToken();
        }

        if (LookNextCodePoint() == '(')
        {
            ConsumeNextCodePoint();
            return new FunctionToken(CreateInfo(), name);
        }

        return new IdentToken(CreateInfo(), name);
    }

    private NumericToken ConsumeNumericToken()
    {
        CssNumber number = ConsumeNumber();
        if (StartsIdentifier())
        {
            var dt = new DimensionToken(CreateInfo(), number);
            dt.Unit = ConsumeName();
            return dt;
        }

        if (LookNextCodePoint() == '%')
        {
            ConsumeNextCodePoint();
            return new PercentageToken(CreateInfo(), number);
        }

        return new NumberToken(CreateInfo(), number);
    }

    private StringToken ConsumeStringToken(char quote)
    {
        var s = new StringToken(CreateInfo(), quote);
        var sb = new StringBuilder();
        do
        {
            if (!TryConsumeNextCodePoint(out char cp) || cp == quote)
                break;

            if (cp == '\n')
            {
                Reconsume(cp);
                s.IsBad = true;
                OnParseError(s, CssParseErrorType.TokenizerBadString);
                break;
            }

            if (cp == '\\')
            {
                if (!TryConsumeNextCodePoint(out char nextCp))
                    continue;

                if (nextCp == '\n')
                {
                    ConsumeNextCodePoint();
                }
                else if (StartsValidEscape(cp, nextCp))
                {
                    string ecp = ConsumeEscapedCodePoint();
                    sb.Append(ecp);
                }
            }
            else
            {
                sb.Append(cp);
            }
        }
        while (true);

        s.Value = sb.ToString();
        return s;
    }

    private string ConsumeName()
    {
        var sb = new StringBuilder();
        do
        {
            if (!TryConsumeNextCodePoint(out char cp))
                break;

            if (IsName(cp))
            {
                sb.Append(cp);
            }
            else
            {
                if (TryLookNextCodePoint(out char nextCp) && StartsValidEscape(cp, nextCp))
                {
                    string ecp = ConsumeEscapedCodePoint();
                    sb.Append(ecp);
                }
                else
                {
                    Reconsume(cp);
                    break;
                }
            }
        }
        while (true);

        return sb.ToString();
    }

    private string ConsumeComment()
    {
        var sb = new StringBuilder();
        do
        {
            if (!TryConsumeNextCodePoint(out char cp))
                break;

            if (cp == '*' && LookNextCodePoint() == '/')
            {
                ConsumeNextCodePoint();
                break;
            }
        }
        while (true);

        return sb.ToString();
    }

    public override CssComponentValue Consume() => ConsumeToken();

    public CssToken ConsumeToken()
    {
        if (!TryConsumeNextCodePoint(out char cp))
            return null; // we don't use an EOF-token as such

        if (IsWhitespace(cp))
        {
            ConsumeWhitespaces();
            return new WhitespaceToken(CreateInfo());
        }

        if (cp == '"' || cp == '\'')
            return ConsumeStringToken(cp);

        if (cp == '(' || cp == ')' || cp == ',' || cp == ':' ||
            cp == ';' || cp == '[' || cp == ']' || cp == '{' ||
            cp == '}')
            return new LiteralToken(CreateInfo(), cp);

        if (cp == '#')
        {
            if (!IsName() && !StartsValidEscape())
                return new DelimToken(CreateInfo(), cp);

            var hash = new HashToken(CreateInfo());
            if (StartsIdentifier())
            {
                hash.TypeFlag = HashTypeFlag.Id;
            }
            hash.Value = ConsumeName();
            return hash;
        }

        if (cp == '$')
        {
            if (LookNextCodePoint() == '=')
            {
                ConsumeNextCodePoint();
                return new SuffixMatchToken(CreateInfo());
            }
            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '*')
        {
            if (LookNextCodePoint() == '=')
            {
                ConsumeNextCodePoint();
                return new SubstringMatchToken(CreateInfo());
            }
            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '+')
        {
            if (StartsNumber())
            {
                Reconsume(cp);
                return ConsumeNumericToken();
            }
            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '-')
        {
            if (StartsNumber())
            {
                Reconsume(cp);
                return ConsumeNumericToken();
            }

            if (StartsIdentifier())
            {
                Reconsume(cp);
                return ConsumeIdentLikeToken();
            }

            if (TryLookNextCodePoint(out char cp1, out char cp2) && cp1 == '-' && cp2 == '>')
            {
                ConsumeNextCodePoint();
                ConsumeNextCodePoint();
                return new CdcToken(CreateInfo());
            }

            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '.')
        {
            if (StartsNumber())
            {
                Reconsume(cp);
                return ConsumeNumericToken();
            }

            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '/')
        {
            if (LookNextCodePoint() == '*')
            {
                ConsumeNextCodePoint();
                string comment = ConsumeComment();
                var token = ConsumeToken();
                if (token != null)
                {
                    token.Comment = comment;
                }
                return token;
            }

            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '<')
        {
            if (TryLookNextCodePoint(out char cp1, out char cp2, out char cp3) && cp1 == '!' && cp2 == '-' && cp3 == '-')
            {
                ConsumeNextCodePoint();
                ConsumeNextCodePoint();
                ConsumeNextCodePoint();
                return new CdoToken(CreateInfo());
            }

            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '@')
        {
            if (StartsIdentifier())
            {
                string name = ConsumeName();
                var akt = new AtKeywordToken(CreateInfo(), name);
                return akt;
            }

            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '\\')
        {
            if (StartsValidEscape())
            {
                Reconsume(cp);
                return ConsumeIdentLikeToken();
            }

            var dt = new DelimToken(CreateInfo(), cp);
            OnParseError(dt, CssParseErrorType.TokenizerBadEscape);
            return dt;
        }

        if (cp == '^')
        {
            if (LookNextCodePoint() == '=')
            {
                ConsumeNextCodePoint();
                return new PrefixMatchToken(CreateInfo());
            }
        }

        if (IsDigit(cp))
        {
            Reconsume(cp);
            return ConsumeNumericToken();
        }

        if (cp == 'u' || cp == 'U')
        {
            if (TryLookNextCodePoint(out char cp1, out char cp2) && cp1 == '+' && IsHexDigit(cp2))
            {
                ConsumeNextCodePoint();
                return ConsumeUnicodeRangeToken();
            }

            Reconsume(cp);
            return ConsumeIdentLikeToken();
        }

        if (IsNameStart(cp))
        {
            Reconsume(cp);
            return ConsumeIdentLikeToken();
        }

        if (cp == '|')
        {
            if (LookNextCodePoint() == '=')
            {
                ConsumeNextCodePoint();
                return new DashMatchToken(CreateInfo());
            }

            if (LookNextCodePoint() == '|')
            {
                ConsumeNextCodePoint();
                return new ColumnToken(CreateInfo());
            }

            return new DelimToken(CreateInfo(), cp);
        }

        if (cp == '~')
        {
            if (LookNextCodePoint() == '=')
            {
                ConsumeNextCodePoint();
                return new IncludeMatchToken(CreateInfo());
            }

            return new DelimToken(CreateInfo(), cp);
        }

        return new DelimToken(CreateInfo(), cp);
    }

    private CssTokenizerInfo CreateInfo() => new(Line, Column);

    private void OnParseError(CssComponentValue value, CssParseErrorType type)
    {
        var error = new CssParseError(new CssTokenizerInfo(Line, Column));
        error.Value = value;
        error.Type = type;
        OnParseError(this, new CssParseErrorEventArgs(error));
    }

    private char ConsumeNextCodePoint()
    {
        TryConsumeNextCodePoint(out char cp);
        return cp;
    }

    private char LookNextCodePoint()
    {
        TryLookNextCodePoint(out char cp);
        return cp;
    }

    private bool TryConsumeNextCodePoint(out char cp)
    {
        var cps = new char[1];
        if (!ReadBuffer(cps, true))
        {
            cp = '\0';
            return false;
        }

        cp = cps[0];
        return true;
    }

    private bool TryLookNextCodePoint(out char cp)
    {
        var cps = new char[1];
        if (!ReadBuffer(cps, false))
        {
            cp = '\0';
            return false;
        }

        cp = cps[0];
        return true;
    }

    private bool TryLookNextCodePoint(out char cp1, out char cp2)
    {
        var cps = new char[2];
        if (!ReadBuffer(cps, false))
        {
            cp1 = '\0';
            cp2 = '\0';
            return false;
        }

        cp1 = cps[0];
        cp2 = cps[1];
        return true;
    }

    private bool TryLookNextCodePoint(out char cp1, out char cp2, out char cp3)
    {
        var cps = new char[3];
        if (!ReadBuffer(cps, false))
        {
            cp1 = '\0';
            cp2 = '\0';
            cp3 = '\0';
            return false;
        }

        cp1 = cps[0];
        cp2 = cps[1];
        cp3 = cps[2];
        return true;
    }

    private void Reconsume(char cp) => _buffer.InsertAtBeginning(cp);

    private bool ReadBuffer(char[] cps, bool dequeue)
    {
        if (_buffer.Count < cps.Length)
        {
            int needed = cps.Length - _buffer.Count;
            for (int i = 0; i < needed; i++)
            {
                int c = BaseReader.Read();
                if (c < 0)
                    return false;

                switch (c)
                {
                    case 0:
                        c = ReplacementCharacter;
                        break;

                    case '\f':
                        c = '\n';
                        break;

                    case '\r':
                        if (BaseReader.Peek() == '\n')
                        {
                            BaseReader.Read();
                        }
                        c = '\n';
                        break;
                }

                if (c == '\n')
                {
                    Line++;
                    Column = 1;
                }
                else
                {
                    Column++;
                }

                _buffer.AppendToEnd((char)c);
            }
        }

        _buffer.Read(cps, dequeue);
        return true;
    }
}
