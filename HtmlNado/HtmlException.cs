namespace HtmlNado;

[Serializable]
public class HtmlException : Exception
{
    public const string Prefix = "HTM";

    public HtmlException()
        : base(Prefix + "0001: HtmlNado exception.")
    {
    }

    public HtmlException(string message)
        : base(Prefix + ":" + message)
    {
    }

    public HtmlException(Exception innerException)
        : base(null, innerException)
    {
    }

    public HtmlException(string message, Exception innerException)
        : base(Prefix + ":" + message, innerException)
    {
    }

    public int Code => GetCode(Message);

    public static int GetCode(string message)
    {
        if (message == null)
            return -1;

        if (!message.StartsWith(Prefix, StringComparison.Ordinal))
            return -1;

        var pos = message.IndexOf(':', Prefix.Length);
        if (pos < 0)
            return -1;

        if (int.TryParse(message[Prefix.Length..pos], NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
            return i;

        return -1;
    }
}
