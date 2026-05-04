using System.Diagnostics.CodeAnalysis;

namespace HtmlNado.Utilities;

// this stream remembers the last N written characters
public class BufferedStreamWriter : StreamWriter
{
    private char[] _buffer;
    private int _currentIndex;

    public BufferedStreamWriter(Stream stream, int bufferSize = 1, Encoding? encoding = null, bool leaveOpen = false)
        : base(stream, encoding ?? HtmlDocument.UTF8NoBOMEncoding, 0x400, leaveOpen)
    {
        Init(bufferSize);
    }

    public BufferedStreamWriter(string path, int bufferSize = 1, bool append = false, Encoding? encoding = null)
        : base(path, append, encoding ?? HtmlDocument.UTF8NoBOMEncoding)
    {
        Init(bufferSize);
    }

    [MemberNotNull(nameof(_buffer))]
    private void Init(int bufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);
        _buffer = new char[bufferSize];
        LastWrittenIndex = -1;
    }

    public char[] GetWrittenBuffer() => _buffer;
    public int LastWrittenIndex { get; private set; }
    public bool IsWrittenBufferFull { get; private set; }

    public string? GetLastWrittenString(int maxLength = int.MaxValue)
    {
        var chars = GetLastWrittenChars(maxLength);
        if (chars == null)
            return null;

        return new string(chars);
    }

    public char[]? GetLastWrittenChars(int maxLength = int.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);

        if (LastWrittenIndex < 0)
            return null;

        // special cases where it's easy to optimize
        if (_buffer.Length == 1)
            return _buffer;

        if (_buffer.Length == 2)
        {
            if (LastWrittenIndex == 1)
            {
                if (maxLength > 1)
                    return _buffer;

                return [_buffer[0]];
            }

            if (IsWrittenBufferFull)
            {
                if (maxLength > 1)
                    return [_buffer[1], _buffer[0]];

                return [_buffer[1]];
            }

            return [_buffer[0]];
        }

        char[] array;
        if (IsWrittenBufferFull && (LastWrittenIndex + 1) < _buffer.Length)
        {
            array = new char[Math.Min(maxLength, _buffer.Length)];
            var upperLength = Math.Min(array.Length, _buffer.Length - (LastWrittenIndex + 1));
            Array.Copy(_buffer, LastWrittenIndex + 1, array, 0, upperLength);
            if (upperLength < array.Length)
            {
                Array.Copy(_buffer, 0, array, upperLength, array.Length - upperLength);
            }
        }
        else
        {
            array = new char[Math.Min(maxLength, LastWrittenIndex + 1)];
            Array.Copy(_buffer, 0, array, 0, array.Length);
        }
        return array;
    }

    public char LastWrittenChar
    {
        get
        {
            if (LastWrittenIndex < 0)
                return '\uFFFF'; // not unicode

            return _buffer[LastWrittenIndex];
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer == null || count == 0)
            return;

        base.Write(buffer, index, count);
        if (_buffer.Length == 0)
            return;

        BufferWrite(buffer, index, count);
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        base.Write(value);
        if (_buffer.Length == 0)
            return;

        var chars = value.ToCharArray();
        BufferWrite(chars, 0, chars.Length);
    }

    public override void Write(char[]? buffer)
    {
        if (buffer == null || buffer.Length == 0)
            return;

        base.Write(buffer);
        if (_buffer.Length == 0)
            return;

        BufferWrite(buffer, 0, buffer.Length);
    }

    public override void Write(char value)
    {
        base.Write(value);
        if (_buffer.Length == 0)
            return;

        BufferWrite(value);
    }

    private void UpdateCurrent(int increment = 1)
    {
        _currentIndex += increment;
        if (_currentIndex >= _buffer.Length)
        {
            _currentIndex = 0;
            IsWrittenBufferFull = true;
        }
    }

    protected virtual void BufferWrite(char value)
    {
        _buffer[_currentIndex] = value;
        LastWrittenIndex = _currentIndex;
        UpdateCurrent();
    }

    protected virtual void BufferWrite(char[] buffer, int index, int count)
    {
        if (count == 1)
        {
            BufferWrite(buffer[index]);
            return;
        }

        if (count >= _buffer.Length)
        {
            // copy buffer's trail
            Array.Copy(buffer, index + count - _buffer.Length, _buffer, 0, _buffer.Length);
            LastWrittenIndex = _buffer.Length - 1;
            IsWrittenBufferFull = true;
            _currentIndex = 0;
            return;
        }

        var upperLength = _buffer.Length - _currentIndex;
        if (upperLength >= count)
        {
            Array.Copy(buffer, index, _buffer, _currentIndex, count);
            LastWrittenIndex = _currentIndex + count - 1;
            UpdateCurrent(count);
            return;
        }

        IsWrittenBufferFull = true;
        Array.Copy(buffer, index, _buffer, _currentIndex, upperLength);

        var left = count - upperLength;
        Array.Copy(buffer, index + upperLength, _buffer, 0, left);
        _currentIndex = left;
        LastWrittenIndex = _currentIndex - 1;
    }
}
