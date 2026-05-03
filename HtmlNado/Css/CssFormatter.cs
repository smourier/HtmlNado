namespace HtmlNado.Css;

// check out http://cssguidelin.es
public class CssFormatter
{
    private readonly TextWriter _writer;
    private bool _indentsPending;

    public CssFormatter(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        _writer = writer;
        IndentText = "    ";
        BraceStyle = CssFormatBraceStyle.C;
        MaxCharactersPerLine = 80;
        LineCountBetweenRules = 1;
    }

    public virtual bool Minify { get; set; } // m
    public virtual int LineCountBetweenRules { get; set; } // l
    public virtual string IndentText { get; set; } // t
    public virtual CssFormatBraceStyle BraceStyle { get; set; } // b
    public virtual int MaxCharactersPerLine { get; set; } // c
    public virtual int IndentationLevel { get; set; }
    public char LastWrittenCharacter { get; private set; }

    protected virtual void WriteIdents()
    {
        if (_indentsPending)
        {
            for (int i = 0; i < IndentationLevel; i++)
            {
                _writer.Write(IndentText);
            }
            _indentsPending = false;
        }
    }

    public virtual void Write(char character)
    {
        WriteIdents();
        _writer.Write(character);
        LastWrittenCharacter = character;
        if (character == '\n')
        {
            _indentsPending = true;
        }
    }

    public virtual void Write(string text)
    {
        if (text == null)
            return;

        WriteIdents();
        text = text.Replace(Environment.NewLine, "\n");
        string[] texts = text.Split('\n');
        foreach (string s in texts)
        {
            if (s.Length > 0)
            {
                _writer.Write(s);
                LastWrittenCharacter = s[s.Length - 1];
            }
        }

        if (texts.Length > 1)
        {
            _writer.WriteLine();
            LastWrittenCharacter = '\n';
            _indentsPending = true;
        }
    }
}
