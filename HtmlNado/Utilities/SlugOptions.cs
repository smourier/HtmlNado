namespace HtmlNado;

public class SlugOptions
{
    public const int DefaultMaximumLength = 80;
    public const string DefaultSeparator = "-";

    private bool _toLower;
    private bool _toUpper;

    public SlugOptions()
    {
        MaximumLength = DefaultMaximumLength;
        Separator = DefaultSeparator;
        AllowedUnicodeCategories =
        [
            UnicodeCategory.UppercaseLetter,
            UnicodeCategory.LowercaseLetter,
            UnicodeCategory.DecimalDigitNumber,
        ];
        AllowedRanges =
        [
            new Tuple<short, short>((short)'a', (short)'z'),
            new Tuple<short, short>((short)'A', (short)'Z'),
            new Tuple<short, short>((short)'0', (short)'9'),
        ];
    }

    public virtual IList<UnicodeCategory> AllowedUnicodeCategories { get; }
    public virtual IList<Tuple<short, short>> AllowedRanges { get; }
    public virtual int MaximumLength { get; set; }
    public virtual string Separator { get; set; }
    public virtual CultureInfo? Culture { get; set; }
    public virtual bool CanEndWithSeparator { get; set; }
    public virtual bool EarlyTruncate { get; set; }

    public virtual bool ToLower
    {
        get => _toLower;
        set
        {
            _toLower = value;
            if (_toLower)
            {
                _toUpper = false;
            }
        }
    }

    public virtual bool ToUpper
    {
        get => _toUpper;
        set
        {
            _toUpper = value;
            if (_toUpper)
            {
                _toLower = false;
            }
        }
    }

    public virtual bool IsAllowed(char character)
    {
        foreach (var p in AllowedRanges)
        {
            if (character >= p.Item1 && character <= p.Item2)
                return true;
        }
        return false;
    }

    public virtual string Replace(char character) => character.ToString(CultureInfo.CurrentCulture);
}
