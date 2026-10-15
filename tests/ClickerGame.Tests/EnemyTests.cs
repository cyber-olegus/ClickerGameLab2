using ClickerGame.Core.Models;
using ClickerGame.Core.Numbers;
using Xunit;

namespace ClickerGame.Tests;

public sealed class EnemyTests
{
    [Fact]
    public void Constructor_CreatesLivingEnemyWithFullHealth()
    {
        var enemy = new Enemy(" Гоблин ", new BigNumber(80), new BigNumber(7), " goblin_1.png ");
        Assert.Equal("Гоблин", enemy.Name);
        Assert.Equal("goblin_1.png", enemy.Icon);
        Assert.Equal("80", enemy.MaxHitPoints.ToString());
        Assert.Equal("80", enemy.CurrentHitPoints.ToString());
        Assert.Equal("7", enemy.GoldReward.ToString());
        Assert.False(enemy.IsDead);
    }

    [Fact]
    public void Constructor_RejectsInvalidState()
    {
        Assert.Throws<ArgumentException>(() => new Enemy(" ", BigNumber.One, BigNumber.Zero, "a.png"));
        Assert.Throws<ArgumentException>(() => new Enemy("A", BigNumber.One, BigNumber.Zero, " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Enemy("A", BigNumber.Zero, BigNumber.Zero, "a.png"));
        Assert.Throws<ArgumentNullException>(() => new Enemy("A", null!, BigNumber.Zero, "a.png"));
        Assert.Throws<ArgumentNullException>(() => new Enemy("A", BigNumber.One, null!, "a.png"));
    }

    [Fact]
    public void NonlethalHit_ReducesOnlyCurrentHealthAndGivesNoGold()
    {
        var enemy = new Enemy("A", new BigNumber(80), new BigNumber(7), "a.png");
        var damage = new BigNumber(10);
        Assert.False(enemy.TakeDamage(damage, out BigNumber reward));
        Assert.Equal("70", enemy.CurrentHitPoints.ToString());
        Assert.Equal("80", enemy.MaxHitPoints.ToString());
        Assert.Equal("7", enemy.GoldReward.ToString());
        Assert.Equal("0", reward.ToString());
        Assert.Equal("10", damage.ToString());
        Assert.False(enemy.IsDead);
    }

    [Theory]
    [InlineData("80")]
    [InlineData("81")]
    [InlineData("1000000000000000000000000000000000000000000")]
    public void LethalHit_ClampsHealthToZeroAndReturnsReward(string damage)
    {
        var enemy = new Enemy("A", new BigNumber(80), new BigNumber(7), "a.png");
        Assert.True(enemy.TakeDamage(new BigNumber(damage), out BigNumber reward));
        Assert.Equal("0", enemy.CurrentHitPoints.ToString());
        Assert.Equal("80", enemy.MaxHitPoints.ToString());
        Assert.Equal("7", reward.ToString());
        Assert.True(enemy.IsDead);
    }

    [Fact]
    public void RepeatedHitOnDeadEnemy_DoesNotReturnRewardAgain()
    {
        var enemy = new Enemy("A", BigNumber.One, new BigNumber(7), "a.png");
        Assert.True(enemy.TakeDamage(BigNumber.One, out BigNumber first));
        Assert.Equal("7", first.ToString());
        Assert.False(enemy.TakeDamage(new BigNumber(100), out BigNumber second));
        Assert.Equal("0", second.ToString());
        Assert.True(enemy.IsDead);
    }

    [Fact]
    public void ZeroDamage_DoesNotKillOrChangeHealth()
    {
        var enemy = new Enemy("A", BigNumber.One, BigNumber.Zero, "a.png");
        Assert.False(enemy.TakeDamage(BigNumber.Zero, out BigNumber reward));
        Assert.Equal("1", enemy.CurrentHitPoints.ToString());
        Assert.Equal("0", reward.ToString());
        Assert.False(enemy.IsDead);
    }

    [Fact]
    public void NullDamage_IsRejectedWithoutChangingState()
    {
        var enemy = new Enemy("A", BigNumber.One, BigNumber.Zero, "a.png");
        Assert.Throws<ArgumentNullException>(() => enemy.TakeDamage(null!, out _));
        Assert.Equal("1", enemy.CurrentHitPoints.ToString());
        Assert.False(enemy.IsDead);
    }

    [Fact]
    public void ZeroRewardEnemy_CanStillBeDefeated()
    {
        var enemy = new Enemy("A", BigNumber.One, BigNumber.Zero, "a.png");
        Assert.True(enemy.TakeDamage(BigNumber.One, out BigNumber reward));
        Assert.True(enemy.IsDead);
        Assert.Equal("0", reward.ToString());
    }

    [Fact]
    public void HealthAndReward_SupportHugeValues()
    {
        var health = new BigNumber("1" + new string('0', 100));
        var prize = new BigNumber(new string('9', 100));
        var enemy = new Enemy("A", health, prize, "a.png");
        Assert.False(enemy.TakeDamage(BigNumber.One, out _));
        Assert.Equal(new string('9', 100), enemy.CurrentHitPoints.ToString());
        Assert.True(enemy.TakeDamage(health, out BigNumber reward));
        Assert.Equal(prize, reward);
        Assert.Equal("1" + new string('0', 100), health.ToString());
    }

    [Fact]
    public void PlayerAndEnemy_CreditOnlyOneRewardForOneEnemy()
    {
        var player = new Player();
        var enemy = new Enemy("A", new BigNumber(20), new BigNumber(7), "a.png");
        for (int hit = 0; hit < 5; hit++)
        {
            if (enemy.TakeDamage(player.DealDamage(), out BigNumber reward))
            {
                player.AddGold(reward);
            }
        }

        Assert.Equal("7", player.Gold.ToString());
        Assert.True(enemy.IsDead);
        Assert.Equal("0", enemy.CurrentHitPoints.ToString());
    }
}
