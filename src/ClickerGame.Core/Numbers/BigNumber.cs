using System.Globalization;
using System.Text;

namespace ClickerGame.Core.Numbers;

/// <summary>
/// Неизменяемое неотрицательное целое число с ручной арифметикой в основании 1000.
/// Дробная часть результата умножения и деления отбрасывается.
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

    // Операции передают сюда новые блоки от младшего к старшему.
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

    private BigNumber Add(BigNumber other)
    {
        int length = Math.Max(number.Length, other.number.Length);
        var result = new int[length + 1];
        int carry = 0;
        for (int index = 0; index < length; index++)
        {
            int left = index < number.Length ? number[index] : 0;
            int right = index < other.number.Length ? other.number[index] : 0;
            int sum = left + right + carry;
            result[index] = sum % Base;
            carry = sum / Base;
        }

        result[length] = carry;
        return new BigNumber(result);
    }

    private BigNumber Subtract(BigNumber other)
    {
        if (CompareTo(other) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(other),
                "Результат вычитания не может быть отрицательным.");
        }

        var result = new int[number.Length];
        int borrow = 0;
        for (int index = 0; index < number.Length; index++)
        {
            int right = index < other.number.Length ? other.number[index] : 0;
            int difference = number[index] - right - borrow;
            borrow = difference < 0 ? 1 : 0;
            result[index] = difference < 0 ? difference + Base : difference;
        }

        return new BigNumber(result);
    }

    private BigNumber Multiply(double multiplier)
    {
        var (numerator, denominator) = ToFraction(multiplier, nameof(multiplier));
        // 1.15 представляется как 115/100; большое число не преобразуется в double.
        BigNumber product = MultiplyBlocks(numerator);
        return product.DivideBlocks(denominator, out _);
    }

    private BigNumber Divide(double divisor)
    {
        var (numerator, denominator) = ToFraction(divisor, nameof(divisor));
        if (numerator == Zero)
        {
            throw new DivideByZeroException("Нельзя делить BigNumber на ноль.");
        }

        // a / (p/q) = (a*q)/p. Только последняя операция отбрасывает остаток.
        return MultiplyBlocks(denominator).DivideBlocks(numerator, out _);
    }

    /// <summary>Умножение двух массивов блоков школьным способом.</summary>
    private BigNumber MultiplyBlocks(BigNumber other)
    {
        if (this == Zero || other == Zero)
        {
            return new BigNumber(0);
        }

        var result = new int[number.Length + other.number.Length];
        for (int left = 0; left < number.Length; left++)
        {
            int carry = 0;
            for (int right = 0; right < other.number.Length; right++)
            {
                int position = left + right;
                // Максимум: 999*999 + 999 + 999 = 999999. int достаточно.
                // Нормализуем блок сразу, чтобы не накапливать огромную сумму.
                int product = number[left] * other.number[right] + result[position] + carry;
                result[position] = product % Base;
                carry = product / Base;
            }

            // Эта позиция ещё не затронута предыдущими строками умножения.
            result[left + other.number.Length] = carry;
        }

        return new BigNumber(result);
    }

    /// <summary>Вспомогательное умножение на один блок от 0 до 999.</summary>
    private BigNumber MultiplyByBlock(int block)
    {
        var result = new int[number.Length + 1];
        int carry = 0;
        for (int index = 0; index < number.Length; index++)
        {
            int product = number[index] * block + carry;
            result[index] = product % Base;
            carry = product / Base;
        }

        result[number.Length] = carry;
        return new BigNumber(result);
    }

    /// <summary>
    /// Деление уголком. Остаток тоже хранится как BigNumber: выражение
    /// remainder*1000 не переполняет int даже при большом делителе.
    /// </summary>
    private BigNumber DivideBlocks(BigNumber divisor, out BigNumber remainder)
    {
        if (divisor == Zero)
        {
            throw new DivideByZeroException("Нельзя делить BigNumber на ноль.");
        }

        if (divisor == One)
        {
            remainder = new BigNumber(0);
            return Clone();
        }

        if (CompareTo(divisor) < 0)
        {
            remainder = Clone();
            return new BigNumber(0);
        }

        var quotient = new int[number.Length];
        remainder = new BigNumber(0);
        for (int index = number.Length - 1; index >= 0; index--)
        {
            // «Сносим» очередной блок: remainder = remainder*1000 + number[index].
            var shifted = new int[remainder.number.Length + 1];
            Array.Copy(remainder.number, 0, shifted, 1, remainder.number.Length);
            shifted[0] = number[index];
            remainder = new BigNumber(shifted);

            // До сноса остаток меньше делителя, поэтому новая цифра частного <= 999.
            // Ищем наибольшую цифру, при которой произведение не больше остатка.
            int low = 0;
            int high = Base - 1;
            int digit = 0;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                BigNumber candidate = divisor.MultiplyByBlock(middle);
                if (candidate <= remainder)
                {
                    digit = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            quotient[index] = digit;
            remainder = remainder.Subtract(divisor.MultiplyByBlock(digit));
        }

        return new BigNumber(quotient);
    }

    /// <summary>
    /// double используется только как входной формат коэффициента, не для арифметики.
    /// Берём его кратчайшую десятичную запись с обратимым преобразованием ("R").
    /// Например, 1.15 -> 115/100, 1E-7 -> 1/10000000.
    /// Если вызывающий код уже вычислил неточный double, эта точность не восстанавливается.
    /// </summary>
    private static (BigNumber Numerator, BigNumber Denominator) ToFraction(
        double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName,
                "Коэффициент должен быть конечным неотрицательным числом.");
        }

        // Включая -0.0: его числовое значение равно нулю.
        if (value == 0)
        {
            return (new BigNumber(0), new BigNumber(1));
        }

        string text = value.ToString("R", CultureInfo.InvariantCulture);
        int exponentIndex = text.IndexOfAny(['E', 'e']);
        int exponent = 0;
        if (exponentIndex >= 0)
        {
            exponent = int.Parse(text.AsSpan(exponentIndex + 1),
                NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            text = text[..exponentIndex];
        }

        int pointIndex = text.IndexOf('.');
        int fractionalDigits = pointIndex < 0 ? 0 : text.Length - pointIndex - 1;
        string digits = text.Replace(".", "");
        int scale = fractionalDigits - exponent;
        if (scale <= 0)
        {
            return (new BigNumber(digits + new string('0', -scale)), new BigNumber(1));
        }

        return (new BigNumber(digits), new BigNumber("1" + new string('0', scale)));
    }
}
