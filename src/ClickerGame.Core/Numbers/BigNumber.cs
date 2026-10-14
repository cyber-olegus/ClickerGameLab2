using System.Globalization;
using System.Text;

namespace ClickerGame.Core.Numbers;

/// <summary>
/// Неотрицательное целое число. Часть 1: хранение, сравнение и контракт арифметики.
/// Четыре арифметических метода будут реализованы в части 2.
/// </summary>
public sealed class BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
{
    private const int Base = 1000;

    // Младший блок расположен первым: 3 712 117 хранится как [117, 712, 3].
    // Массив никогда не выдаётся наружу и после конструктора не изменяется.
    private readonly int[] number;

    public static BigNumber Zero { get; } = new(0);
    public static BigNumber One { get; } = new(1);

    public BigNumber(int value)
        : this(value.ToString(CultureInfo.InvariantCulture))
    {
    }

    /// <summary>Принимает только десятичные цифры, без знака, пробелов и разделителей.</summary>
    public BigNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0 || value.Any(character => character < '0' || character > '9'))
        {
            throw new ArgumentException("Число должно содержать только цифры от 0 до 9.", nameof(value));
        }

        string normalized = value.TrimStart('0');
        if (normalized.Length == 0)
        {
            number = [0];
            return;
        }

        number = new int[(normalized.Length - 1) / 3 + 1];
        int end = normalized.Length;
        for (int index = 0; end > 0; index++)
        {
            int start = Math.Max(0, end - 3);
            // Разбираем только один блок (не более 999), а не число целиком.
            number[index] = int.Parse(normalized.AsSpan(start, end - start),
                NumberStyles.None, CultureInfo.InvariantCulture);
            end = start;
        }
    }

    // Будущие операции передают сюда свои блоки от младшего к старшему.
    private BigNumber(int[] blocks)
    {
        if (blocks.Length == 0 || blocks.Any(block => block < 0 || block >= Base))
        {
            throw new ArgumentException("Каждый блок должен быть от 0 до 999.", nameof(blocks));
        }

        int length = blocks.Length;
        while (length > 1 && blocks[length - 1] == 0)
        {
            length--;
        }

        // readonly запрещает заменить ссылку, но не защищает элементы массива.
        // Поэтому копируем входные блоки и не передаём внутренний массив вызывающему коду.
        number = new int[length];
        Array.Copy(blocks, number, length);
    }

    public BigNumber Clone() => new(number);

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(number[^1].ToString(CultureInfo.InvariantCulture));
        for (int index = number.Length - 2; index >= 0; index--)
        {
            result.Append(number[index].ToString("D3", CultureInfo.InvariantCulture));
        }

        // Конструкторы удаляют старшие нулевые блоки; ноль остаётся строкой "0".
        return result.ToString();
    }

    public int CompareTo(BigNumber? other)
    {
        if (other is null)
        {
            return 1;
        }

        int lengthComparison = number.Length.CompareTo(other.number.Length);
        if (lengthComparison != 0)
        {
            return lengthComparison;
        }

        for (int index = number.Length - 1; index >= 0; index--)
        {
            int blockComparison = number[index].CompareTo(other.number[index]);
            if (blockComparison != 0)
            {
                return blockComparison;
            }
        }

        return 0;
    }

    public bool Equals(BigNumber? other) => other is not null && CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is BigNumber other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (int block in number)
        {
            hash.Add(block);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(BigNumber? left, BigNumber? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(BigNumber? left, BigNumber? right) => !(left == right);
    public static bool operator >(BigNumber left, BigNumber right) => Compare(left, right) > 0;
    public static bool operator <(BigNumber left, BigNumber right) => Compare(left, right) < 0;
    public static bool operator >=(BigNumber left, BigNumber right) => Compare(left, right) >= 0;
    public static bool operator <=(BigNumber left, BigNumber right) => Compare(left, right) <= 0;

    private static int Compare(BigNumber left, BigNumber right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right);
    }

    public static BigNumber operator +(BigNumber left, BigNumber right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Add(right);
    }

    public static BigNumber operator -(BigNumber left, BigNumber right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Subtract(right);
    }

    public static BigNumber operator *(BigNumber value, double multiplier)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Multiply(multiplier);
    }

    public static BigNumber operator /(BigNumber value, double divisor)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Divide(divisor);
    }

    // Это явно обозначенные точки для следующей части, а не работающая арифметика.
    private BigNumber Add(BigNumber other) =>
        throw new NotImplementedException("Сложение BigNumber будет реализовано в части 2.");

    private BigNumber Subtract(BigNumber other) =>
        throw new NotImplementedException("Вычитание BigNumber будет реализовано в части 2.");

    private BigNumber Multiply(double multiplier) =>
        throw new NotImplementedException("Умножение BigNumber будет реализовано в части 2.");

    private BigNumber Divide(double divisor) =>
        throw new NotImplementedException("Деление BigNumber будет реализовано в части 2.");
}
