using ClickerGame.Core.Models;
using ClickerGame.Core.Numbers;
using Xunit;

namespace ClickerGame.Tests;

public sealed class PlayerTests
{
    [Fact]
    public void DefaultConstructor_SetsStartingStats()
    {
        var player = new Player();
        Assert.Equal("0", player.Gold.ToString());
        Assert.Equal("10", player.Damage.ToString());
        Assert.Equal("20", player.UpgradeCost.ToString());
        Assert.Equal(1, player.Level);
        Assert.Equal(1.2, player.DamageModifier);
        Assert.Equal(1.2, player.UpgradeModifier);
    }

    [Fact]
    public void CustomConstructor_PreservesConfiguration()
    {
        var player = new Player(new BigNumber(42), new BigNumber(15),
            new BigNumber(5), 1.5, 1.4);
        Assert.Equal("42", player.Gold.ToString());
        Assert.Equal("15", player.Damage.ToString());
        Assert.Equal("5", player.UpgradeCost.ToString());
        Assert.Equal(1, player.Level);
        Assert.Equal(1.5, player.DamageModifier);
        Assert.Equal(1.4, player.UpgradeModifier);
    }

    [Fact]
    public void Constructor_RejectsNullAmounts()
    {
        Assert.Throws<ArgumentNullException>(() => new Player(null!, BigNumber.One, BigNumber.One));
        Assert.Throws<ArgumentNullException>(() => new Player(BigNumber.Zero, null!, BigNumber.One));
        Assert.Throws<ArgumentNullException>(() => new Player(BigNumber.Zero, BigNumber.One, null!));
    }

    [Fact]
    public void Constructor_RejectsZeroDamageAndPrice()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Player(BigNumber.Zero, BigNumber.Zero, BigNumber.One));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Player(BigNumber.Zero, BigNumber.One, BigNumber.Zero));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_RejectsInvalidModifiers(double modifier)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Player(BigNumber.Zero, BigNumber.One, BigNumber.One, modifier, 1.2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Player(BigNumber.Zero, BigNumber.One, BigNumber.One, 1.2, modifier));
    }

    [Fact]
    public void AddGold_AccumulatesRewardsWithoutChangingOtherStats()
    {
        var player = new Player();
        var reward = new BigNumber(7);
        player.AddGold(reward);
        player.AddGold(new BigNumber(18));
        player.AddGold(BigNumber.Zero);
        Assert.Equal("25", player.Gold.ToString());
        Assert.Equal("7", reward.ToString());
        Assert.Equal("10", player.Damage.ToString());
        Assert.Equal("20", player.UpgradeCost.ToString());
        Assert.Equal(1, player.Level);
    }

    [Fact]
    public void AddGold_RejectsNullWithoutChangingBalance()
    {
        var player = new Player();
        player.AddGold(new BigNumber(7));
        Assert.Throws<ArgumentNullException>(() => player.AddGold(null!));
        Assert.Equal("7", player.Gold.ToString());
    }

    [Fact]
    public void AddGold_SupportsHundredDigitAmounts()
    {
        var player = new Player();
        player.AddGold(new BigNumber(new string('9', 100)));
        player.AddGold(BigNumber.One);
        Assert.Equal("1" + new string('0', 100), player.Gold.ToString());
    }

    [Fact]
    public void DealDamage_DoesNotChargeGoldOrChangeStats()
    {
        var player = new Player();
        player.AddGold(new BigNumber(30));
        BigNumber firstHit = player.DealDamage();
        BigNumber secondHit = player.DealDamage();
        Assert.Equal("10", firstHit.ToString());
        Assert.Equal("10", secondHit.ToString());
        Assert.Equal("30", player.Gold.ToString());
        Assert.Equal("10", player.Damage.ToString());
        Assert.Equal("20", player.UpgradeCost.ToString());
        Assert.Equal(1, player.Level);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(19)]
    public void TryUpgrade_InsufficientGoldLeavesStateUnchanged(int balance)
    {
        var player = new Player(new BigNumber(balance), new BigNumber(10), new BigNumber(20));
        Assert.False(player.TryUpgrade());
        Assert.Equal(balance.ToString(), player.Gold.ToString());
        Assert.Equal("10", player.Damage.ToString());
        Assert.Equal("20", player.UpgradeCost.ToString());
        Assert.Equal(1, player.Level);
        Assert.Equal(1.2, player.DamageModifier);
        Assert.Equal(1.2, player.UpgradeModifier);
    }

    [Fact]
    public void TryUpgrade_ExactPriceBuysOneUpgrade()
    {
        var player = new Player();
        player.AddGold(new BigNumber(20));
        Assert.True(player.TryUpgrade());
        Assert.Equal("0", player.Gold.ToString());
        Assert.Equal("12", player.Damage.ToString());
        Assert.Equal("24", player.UpgradeCost.ToString());
        Assert.Equal(2, player.Level);
        Assert.Equal(1.2, player.DamageModifier);
        Assert.Equal(1.2, player.UpgradeModifier);
        Assert.False(player.TryUpgrade());
        Assert.Equal("0", player.Gold.ToString());
        Assert.Equal("12", player.Damage.ToString());
        Assert.Equal("24", player.UpgradeCost.ToString());
        Assert.Equal(2, player.Level);
    }

    [Fact]
    public void TryUpgrade_SpendsOldPriceInsteadOfNewPrice()
    {
        var player = new Player();
        player.AddGold(new BigNumber(100));
        Assert.True(player.TryUpgrade());
        Assert.Equal("80", player.Gold.ToString());
        Assert.Equal("24", player.UpgradeCost.ToString());
    }

    [Theory]
    [InlineData(1, "12", "24")]
    [InlineData(2, "14", "57")]
    [InlineData(3, "16", "205")]
    [InlineData(4, "19", "984")]
    [InlineData(5, "22", "5904")]
    public void TryUpgrade_UsesLevelBeforePurchaseForNextPrice(int purchases,
        string expectedDamage, string expectedPrice)
    {
        var player = new Player();
        for (int index = 0; index < purchases; index++)
        {
            player.AddGold(player.UpgradeCost);
            Assert.True(player.TryUpgrade());
        }

        Assert.Equal(purchases + 1, player.Level);
        Assert.Equal(expectedDamage, player.Damage.ToString());
        Assert.Equal(expectedPrice, player.UpgradeCost.ToString());
        Assert.Equal("0", player.Gold.ToString());
    }

    [Fact]
    public void TryUpgrade_RoundsPriceOnlyAfterMultiplyingByLevel()
    {
        var player = new Player(new BigNumber(100), new BigNumber(100),
            new BigNumber(11), 1.01, 1.15);
        Assert.True(player.TryUpgrade());
        Assert.Equal("12", player.UpgradeCost.ToString());
        Assert.True(player.TryUpgrade());
        // floor(12 * 2 * 1.15) = 27. Если округлить раньше, получилось бы 26.
        Assert.Equal("27", player.UpgradeCost.ToString());
        Assert.Equal("77", player.Gold.ToString());
        Assert.Equal("102", player.Damage.ToString());
        Assert.Equal(3, player.Level);
    }

    [Fact]
    public void TryUpgrade_GuaranteesGrowthWhenFractionIsDiscarded()
    {
        var player = new Player(BigNumber.One, BigNumber.One, BigNumber.One, 1.01, 1.01);
        Assert.True(player.TryUpgrade());
        Assert.Equal("0", player.Gold.ToString());
        Assert.Equal("2", player.Damage.ToString());
        Assert.Equal("2", player.UpgradeCost.ToString());
        Assert.Equal(2, player.Level);
    }

    [Fact]
    public void TryUpgrade_SupportsAmountsBeyondStandardIntegerRange()
    {
        var player = new Player(new BigNumber("1" + new string('0', 100)),
            new BigNumber("1" + new string('0', 40)),
            new BigNumber("1" + new string('0', 40)));
        Assert.True(player.TryUpgrade());
        Assert.Equal(new string('9', 60) + new string('0', 40), player.Gold.ToString());
        Assert.Equal("12" + new string('0', 39), player.Damage.ToString());
        Assert.Equal("12" + new string('0', 39), player.UpgradeCost.ToString());
    }

    [Fact]
    public void Upgrade_DoesNotChangePreviouslyReadValuesOrOtherPlayers()
    {
        var gold = new BigNumber(100);
        var damage = new BigNumber(10);
        var price = new BigNumber(20);
        var first = new Player(gold, damage, price);
        var second = new Player(gold, damage, price);
        BigNumber previousHit = first.DealDamage();
        BigNumber previousBalance = first.Gold;
        Assert.True(first.TryUpgrade());
        Assert.Equal("12", first.DealDamage().ToString());
        Assert.Equal("10", previousHit.ToString());
        Assert.Equal("100", previousBalance.ToString());
        Assert.Equal("100", gold.ToString());
        Assert.Equal("10", damage.ToString());
        Assert.Equal("20", price.ToString());
        Assert.Equal("100", second.Gold.ToString());
        Assert.Equal("10", second.Damage.ToString());
        Assert.Equal("20", second.UpgradeCost.ToString());
        Assert.Equal(1, second.Level);
    }

    [Fact]
    public void ThirtyUpgrades_KeepGrowingWithoutNegativeBalances()
    {
        var player = new Player();
        for (int purchase = 0; purchase < 30; purchase++)
        {
            BigNumber oldDamage = player.Damage;
            BigNumber oldCost = player.UpgradeCost;
            player.AddGold(oldCost);
            Assert.True(player.TryUpgrade());
            Assert.Equal("0", player.Gold.ToString());
            Assert.True(player.Damage > oldDamage);
            Assert.True(player.UpgradeCost > oldCost);
        }

        Assert.Equal(31, player.Level);
        Assert.True(player.UpgradeCost.ToString().Length > 19);
    }
}
