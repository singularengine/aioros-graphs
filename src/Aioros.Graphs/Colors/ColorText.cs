using System.Text;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Colors;

internal enum ColorFormat
{
    Hex,
    Hex8,
    Rgb,
    Hsl,
}

internal static class ColorText
{
    public const int MaxLength = 160;

    public static int Write(ColorFormat format, double c0, double c1, double c2, double opacity, Span<byte> destination)
    {
        var writer = new Writer(destination);
        switch (format)
        {
            case ColorFormat.Hex:
            case ColorFormat.Hex8:
                writer.Ascii("#"u8);
                writer.Hex(c0);
                writer.Hex(c1);
                writer.Hex(c2);
                if (format == ColorFormat.Hex8)
                {
                    writer.Hex((double.IsNaN(opacity) ? 1 : opacity) * 255);
                }

                break;
            case ColorFormat.Rgb:
                var a = ColorMath.ClampOpacity(opacity);
                writer.Ascii(a == 1 ? "rgb("u8 : "rgba("u8);
                writer.Integer((int)ColorMath.ClampChannel(c0));
                writer.Ascii(", "u8);
                writer.Integer((int)ColorMath.ClampChannel(c1));
                writer.Ascii(", "u8);
                writer.Integer((int)ColorMath.ClampChannel(c2));
                writer.Close(a);
                break;
            default:
                var alpha = ColorMath.ClampOpacity(opacity);
                writer.Ascii(alpha == 1 ? "hsl("u8 : "hsla("u8);
                writer.Number(ColorMath.ClampHue(c0));
                writer.Ascii(", "u8);
                writer.Number(ColorMath.ClampUnit(c1) * 100);
                writer.Ascii("%, "u8);
                writer.Number(ColorMath.ClampUnit(c2) * 100);
                writer.Ascii("%"u8);
                writer.Close(alpha);
                break;
        }

        return writer.Written;
    }

    public static bool TryWriteHex(double r, double g, double b, Span<byte> destination, out int written)
    {
        if (destination.Length < 7)
        {
            written = 0;
            return false;
        }

        destination[0] = (byte)'#';
        WriteHexByte((int)ColorMath.ClampChannel(r), destination[1..]);
        WriteHexByte((int)ColorMath.ClampChannel(g), destination[3..]);
        WriteHexByte((int)ColorMath.ClampChannel(b), destination[5..]);
        written = 7;
        return true;
    }

    public static bool TryWriteHex(double r, double g, double b, Span<char> destination, out int written)
    {
        Span<byte> buffer = stackalloc byte[7];
        TryWriteHex(r, g, b, buffer, out _);
        if (destination.Length < 7)
        {
            written = 0;
            return false;
        }

        for (var i = 0; i < 7; i++)
        {
            destination[i] = (char)buffer[i];
        }

        written = 7;
        return true;
    }

    private static void WriteHexByte(int value, Span<byte> destination)
    {
        destination[1] = "0123456789abcdef"u8[value & 15];
        destination[0] = "0123456789abcdef"u8[value >> 4];
    }

    public static bool TryWrite(ColorFormat format, double c0, double c1, double c2, double opacity, Span<byte> destination, out int written)
    {
        if (destination.Length >= Longest(format))
        {
            written = Write(format, c0, c1, c2, opacity, destination);
            return true;
        }

        Span<byte> buffer = stackalloc byte[MaxLength];
        var length = Write(format, c0, c1, c2, opacity, buffer);
        if (length > destination.Length)
        {
            written = 0;
            return false;
        }

        buffer[..length].CopyTo(destination);
        written = length;
        return true;
    }

    public static bool TryWrite(ColorFormat format, double c0, double c1, double c2, double opacity, Span<char> destination, out int written)
    {
        Span<byte> buffer = stackalloc byte[MaxLength];
        var length = Write(format, c0, c1, c2, opacity, buffer);
        if (length > destination.Length)
        {
            written = 0;
            return false;
        }

        for (var i = 0; i < length; i++)
        {
            destination[i] = (char)buffer[i];
        }

        written = length;
        return true;
    }

    private static int Longest(ColorFormat format) => format switch
    {
        ColorFormat.Hex => 7,
        ColorFormat.Hex8 => 9,
        ColorFormat.Rgb => 21 + JsNumber.MaxShortestLength,
        _ => MaxLength,
    };

    public static string ToText(ColorFormat format, double c0, double c1, double c2, double opacity)
    {
        Span<byte> buffer = stackalloc byte[MaxLength];
        return Encoding.ASCII.GetString(buffer[..Write(format, c0, c1, c2, opacity, buffer)]);
    }

    private ref struct Writer(Span<byte> destination)
    {
        private readonly Span<byte> destination = destination;

        public int Written { get; private set; }

        public void Ascii(ReadOnlySpan<byte> text)
        {
            text.CopyTo(destination[Written..]);
            Written += text.Length;
        }

        public void Number(double value) => Written += JsNumber.WriteShortest(value, destination[Written..]);

        public void Integer(int value)
        {
            if (value >= 100)
            {
                destination[Written++] = (byte)('0' + value / 100);
            }

            if (value >= 10)
            {
                destination[Written++] = (byte)('0' + value / 10 % 10);
            }

            destination[Written++] = (byte)('0' + value % 10);
        }

        public void Hex(double value)
        {
            var v = (int)ColorMath.ClampChannel(value);
            destination[Written++] = "0123456789abcdef"u8[v >> 4];
            destination[Written++] = "0123456789abcdef"u8[v & 15];
        }

        public void Close(double opacity)
        {
            if (opacity != 1)
            {
                Ascii(", "u8);
                Number(opacity);
            }

            Ascii(")"u8);
        }
    }
}
