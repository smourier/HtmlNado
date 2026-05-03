namespace HtmlNado.Css;

public class CssNumber
{
    private float? _singleValue;
    private double? _doubleValue;
    private string _representation;

    public CssNumber()
    {
        Representation = string.Empty;
    }

    public virtual NumberTypeFlag TypeFlag { get; set; }

    public virtual string Representation
    {
        get => _representation ?? string.Empty;
        set
        {
            if (_representation == value)
                return;

            _representation = value;
            _singleValue = null;
            _doubleValue = null;
        }
    }

    public virtual float SingleValue
    {
        get
        {
            if (!_singleValue.HasValue)
            {
                if (float.TryParse(Representation, NumberStyles.Number, CultureInfo.InvariantCulture, out float value))
                {
                    _singleValue = value;
                }
                else
                {
                    _singleValue = float.NaN;
                }
            }
            return _singleValue.Value;
        }
    }

    public virtual double DoubleValue
    {
        get
        {
            if (!_doubleValue.HasValue)
            {
                if (double.TryParse(Representation, NumberStyles.Number, CultureInfo.InvariantCulture, out double value))
                {
                    _doubleValue = value;
                }
                else
                {
                    _doubleValue = double.NaN;
                }
            }
            return _doubleValue.Value;
        }
    }

    public override string ToString() => Representation;
}
