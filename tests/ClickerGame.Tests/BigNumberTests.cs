using System.Globalization;
using ClickerGame.Core.Numbers;
using Xunit;

namespace ClickerGame.Tests;

public sealed class BigNumberTests
{
    [Theory]
    [InlineData("0", "0")]
    [InlineData("000000", "0")]
    [InlineData("00019005017", "19005017")]
    [InlineData("1000000001", "1000000001")]
    [InlineData("999", "999")]
    [InlineData("1000", "1000")]
    public void Constructor_FormatsBlocksWithoutLosingZeros(string input, string expected)
    {
        Assert.Equal(expected, new BigNumber(input).ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void Constructor_AcceptsNonnegativeInt(int value)
    {
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), new BigNumber(value).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("1.2")]
    [InlineData("1 000")]
    [InlineData("12a")]
    [InlineData(" 1")]
    public void Constructor_RejectsInvalidInput(string input)
    {
        Assert.Throws<ArgumentException>(() => new BigNumber(input));
    }

    [Fact]
    public void Constructor_RejectsNullAndNegativeInt()
    {
        Assert.Throws<ArgumentNullException>(() => new BigNumber((string)null!));
        Assert.Throws<ArgumentException>(() => new BigNumber(-1));
    }

    [Theory]
    [InlineData("3712117", "15512900", "19225017")]
    [InlineData("1000000000", "999999999", "1999999999")]
    [InlineData("999999999", "1", "1000000000")]
    [InlineData("0", "0", "0")]
    [InlineData("1", "1000000000", "1000000001")]
    [InlineData("1000000000", "1", "1000000001")]
    public void Add_HandlesCarryAndDifferentLengths(string left, string right, string expected)
    {
        Assert.Equal(expected, (new BigNumber(left) + new BigNumber(right)).ToString());
    }

    [Theory]
    [InlineData("3712117", "1911900", "1800217")]
    [InlineData("1000000000", "1", "999999999")]
    [InlineData("19005017", "19005017", "0")]
    [InlineData("0", "0", "0")]
    [InlineData("1000001", "0", "1000001")]
    [InlineData("1000001", "1000000", "1")]
    public void Subtract_HandlesBorrowAndZero(string left, string right, string expected)
    {
        Assert.Equal(expected, (new BigNumber(left) - new BigNumber(right)).ToString());
    }

    [Fact]
    public void Subtract_RejectsNegativeResult()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BigNumber(1) - new BigNumber(2));
    }

    [Theory]
    [InlineData("7135654", 23, "164120042")]
    [InlineData("999999999", 999, "998999999001")]
    [InlineData("999999999", 1000, "999999999000")]
    [InlineData("101", 1.15, "116")]
    [InlineData("5", 0.5, "2")]
    [InlineData("999", 2147483647, "2145336163353")]
    [InlineData("12345", 0, "0")]
    [InlineData("0", 1.15, "0")]
    [InlineData("12345", 1, "12345")]
    [InlineData("123", 1e20, "12300000000000000000000")]
    [InlineData("100000000000000000000", 1e-20, "1")]
    public void Multiply_HandlesCarryFractionsAndLargeFactors(string input, double factor, string expected)
    {
        Assert.Equal(expected, (new BigNumber(input) * factor).ToString());
    }

    [Theory]
    [InlineData("10014217", 3, "3338072")]
    [InlineData("1000000000", 1000, "1000000")]
    [InlineData("1000000001", 1000, "1000000")]
    [InlineData("2", 3, "0")]
    [InlineData("0", 3, "0")]
    [InlineData("12345", 1, "12345")]
    [InlineData("101", 1.15, "87")]
    [InlineData("5", 0.5, "10")]
    [InlineData("2145336163353", 2147483647, "999")]
    [InlineData("184467440737095516160", 4294967296, "42949672960")]
    [InlineData("100000000000000000001", 1e20, "1")]
    [InlineData("1", 1e-20, "100000000000000000000")]
    public void Divide_HandlesRemainderFractionsAndLargeDivisors(string input, double divisor, string expected)
    {
        Assert.Equal(expected, (new BigNumber(input) / divisor).ToString());
    }

    [Fact]
    public void Divide_MethodicalExampleHasRemainderOne()
    {
        var dividend = new BigNumber("10014217");
        BigNumber quotient = dividend / 3;
        BigNumber remainder = dividend - quotient * 3;
        Assert.Equal("3338072", quotient.ToString());
        Assert.Equal("1", remainder.ToString());
    }

    [Fact]
    public void Divide_RejectsZeroIncludingNegativeZero()
    {
        Assert.Throws<DivideByZeroException>(() => new BigNumber(5) / 0.0);
        Assert.Throws<DivideByZeroException>(() => new BigNumber(5) / -0.0);
        Assert.Throws<DivideByZeroException>(() => BigNumber.Zero / 0.0);
        Assert.Equal("0", (new BigNumber(5) * -0.0).ToString());
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Arithmetic_RejectsInvalidFactors(double factor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BigNumber(5) * factor);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BigNumber(5) / factor);
    }

    [Fact]
    public void Arithmetic_DoesNotMutateOperandsAndReturnsNewObjects()
    {
        var left = new BigNumber("10014217");
        var right = new BigNumber("19005017");
        BigNumber alias = left;
        BigNumber[] results = [left + right, right - left, left * 23, left / 3,
            left + BigNumber.Zero, left - BigNumber.Zero, left * 1, left / 1];
        foreach (BigNumber result in results)
        {
            Assert.NotSame(left, result);
            Assert.NotSame(right, result);
        }

        Assert.Equal("10014217", left.ToString());
        Assert.Equal("10014217", alias.ToString());
        Assert.Equal("19005017", right.ToString());
        Assert.Equal("0", BigNumber.Zero.ToString());
        Assert.Equal("1", BigNumber.One.ToString());
    }

    [Fact]
    public void CloneEqualityComparisonAndHashCodeAgree()
    {
        var original = new BigNumber("00019005017");
        BigNumber clone = original.Clone();
        Assert.NotSame(original, clone);
        Assert.True(original == clone);
        Assert.False(original != clone);
        Assert.True(original >= clone);
        Assert.True(original <= clone);
        Assert.False(original > clone);
        Assert.False(original < clone);
        Assert.Equal(0, original.CompareTo(clone));
        Assert.True(original.Equals((object)clone));
        Assert.Equal(original.GetHashCode(), clone.GetHashCode());
        Assert.True(new BigNumber("1000000") > new BigNumber("999999"));
        Assert.True(new BigNumber("1001") < new BigNumber("1999"));
        Assert.True(new BigNumber("1999") < new BigNumber("2000"));
        Assert.True(original.CompareTo(null) > 0);
        Assert.False(original.Equals("19005017"));
    }

    [Fact]
    public void NullOperandsAreHandledExplicitly()
    {
        BigNumber? missing = null;
        Assert.True(missing == null);
        Assert.True(BigNumber.Zero != missing);
        Assert.Throws<ArgumentNullException>(() => missing! + BigNumber.One);
        Assert.Throws<ArgumentNullException>(() => BigNumber.One + missing!);
        Assert.Throws<ArgumentNullException>(() => missing! - BigNumber.One);
        Assert.Throws<ArgumentNullException>(() => BigNumber.One - missing!);
        Assert.Throws<ArgumentNullException>(() => missing! * 2);
        Assert.Throws<ArgumentNullException>(() => missing! / 2);
        Assert.Throws<ArgumentNullException>(() => { _ = missing! < BigNumber.One; });
    }

    [Fact]
    public void Arithmetic_WorksFarBeyondStandardIntegerRange()
    {
        string nines = new('9', 100);
        string powerOfTen = "1" + new string('0', 100);
        var value = new BigNumber(nines);
        Assert.Equal(powerOfTen, (value + BigNumber.One).ToString());
        Assert.Equal(nines, (new BigNumber(powerOfTen) - BigNumber.One).ToString());
        Assert.Equal("2" + new string('9', 99) + "7", (value * 3).ToString());
        Assert.Equal(new string('3', 100), (value / 3).ToString());
    }

    [Fact]
    public void Arithmetic_HandlesVerySmallAndVeryLargeFiniteCoefficients()
    {
        Assert.Equal("1" + new string('0', 308), (BigNumber.One * 1e308).ToString());
        Assert.Equal("0", (BigNumber.One / double.MaxValue).ToString());
        Assert.Equal("0", (BigNumber.One * double.Epsilon).ToString());
        // Double.Epsilon имеет кратчайшую запись 5E-324: используем именно её.
        Assert.Equal("2" + new string('0', 323), (BigNumber.One / double.Epsilon).ToString());
        Assert.Equal("5", (new BigNumber("1" + new string('0', 324)) * double.Epsilon).ToString());
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public void FractionConversion_DoesNotDependOnLocale(string cultureName)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            Assert.Equal("116", (new BigNumber(101) * 1.15).ToString());
            Assert.Equal("87", (new BigNumber(101) / 1.15).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Arithmetic_MatchesIndependentIntOracleOnSmallInputs()
    {
        // int используется только как эталон для небольших тестовых чисел.
        // Заданный диапазон исключает переполнение самого эталона.
        var random = new Random(2026);
        for (int iteration = 0; iteration < 300; iteration++)
        {
            int left = random.Next(0, 30000);
            int right = random.Next(0, 30000);
            int factor = random.Next(1, 500);
            Assert.Equal((left + right).ToString(), (new BigNumber(left) + new BigNumber(right)).ToString());
            Assert.Equal(Math.Abs(left - right).ToString(),
                (new BigNumber(Math.Max(left, right)) - new BigNumber(Math.Min(left, right))).ToString());
            Assert.Equal((left * factor).ToString(), (new BigNumber(left) * factor).ToString());
            Assert.Equal((left / factor).ToString(), (new BigNumber(left) / factor).ToString());
            Assert.Equal((left * 115 / 100).ToString(), (new BigNumber(left) * 1.15).ToString());
            Assert.Equal((left * 100 / 115).ToString(), (new BigNumber(left) / 1.15).ToString());
        }
    }
}
