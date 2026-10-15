using ClickerGame.Core.Models;
using ClickerGame.Core.Numbers;
using ClickerGame.Core.Services;
using EnemyEditor.Core;
using Xunit;

namespace ClickerGame.Tests;

public sealed class EnemyFactoryTests
{
    [Theory]
    [InlineData(0, "100", "10")]
    [InlineData(1, "200", "30")]
    [InlineData(2, "400", "90")]
    [InlineData(3, "800", "270")]
    public void Create_ScalesFromBaseOncePerGlobalVictory(int victories, string health, string reward)
    {
        var template = new CEnemyTemplate("A", "a.png", 100, 2, 10, 3, 1);
        Enemy enemy = new EnemyFactory().Create(template, victories);
        Assert.Equal(health, enemy.MaxHitPoints.ToString());
        Assert.Equal(health, enemy.CurrentHitPoints.ToString());
        Assert.Equal(reward, enemy.GoldReward.ToString());
        Assert.Equal("A", enemy.Name);
        Assert.Equal("a.png", enemy.Icon);
        Assert.False(enemy.IsDead);
        Assert.Equal(100, template.BaseLife);
        Assert.Equal(10, template.BaseGold);
        Assert.Equal(1.0, template.SpawnChance);
    }

    [Fact]
    public void FractionalGrowth_TruncatesAfterEachStep()
    {
        var template = new CEnemyTemplate("A", "a.png", 80, 1.1, 7, 1.05, 1);
        Enemy enemy = new EnemyFactory().Create(template, 2);
        // floor(80*1.1)=88; floor(88*1.1)=96. Маленькая награда может не вырасти.
        Assert.Equal("96", enemy.MaxHitPoints.ToString());
        Assert.Equal("7", enemy.GoldReward.ToString());
    }

    [Fact]
    public void DecreasingModifiers_KeepHealthPositiveButAllowZeroReward()
    {
        var template = new CEnemyTemplate("A", "a.png", 2, 0.1, 7, 0, 1);
        Enemy enemy = new EnemyFactory().Create(template, 3);
        Assert.Equal("1", enemy.MaxHitPoints.ToString());
        Assert.Equal("0", enemy.GoldReward.ToString());
        Assert.False(enemy.IsDead);
    }

    [Fact]
    public void UnitModifiers_KeepBaseStats()
    {
        var template = new CEnemyTemplate("A", "a.png", 80, 1, 7, 1, 1);
        Enemy enemy = new EnemyFactory().Create(template, 12);
        Assert.Equal("80", enemy.MaxHitPoints.ToString());
        Assert.Equal("7", enemy.GoldReward.ToString());
    }

    [Fact]
    public void ZeroBaseReward_RemainsZero()
    {
        var template = new CEnemyTemplate("A", "a.png", 80, 1, 0, 2, 1);
        Assert.Equal("0", new EnemyFactory().Create(template, 5).GoldReward.ToString());
    }

    [Fact]
    public void Factory_DoesNotReusePreviouslyDamagedEnemy()
    {
        var factory = new EnemyFactory();
        var template = new CEnemyTemplate("A", "a.png", 80, 1, 7, 1, 1);
        Enemy first = factory.Create(template, 0);
        first.TakeDamage(new BigNumber(10), out _);
        Enemy second = factory.Create(template, 0);
        Assert.NotSame(first, second);
        Assert.Equal("70", first.CurrentHitPoints.ToString());
        Assert.Equal("80", second.CurrentHitPoints.ToString());
    }

    [Fact]
    public void Factory_UsesSelectedTemplatesOwnModifiers()
    {
        var factory = new EnemyFactory();
        var first = new CEnemyTemplate("A", "a.png", 100, 2, 10, 3, 0.5);
        var second = new CEnemyTemplate("B", "b.png", 50, 3, 2, 2, 0.5);
        factory.Create(first, 1);
        Enemy enemy = factory.Create(second, 2);
        Assert.Equal("450", enemy.MaxHitPoints.ToString());
        Assert.Equal("8", enemy.GoldReward.ToString());
    }

    [Fact]
    public void Growth_ExceedsStandardIntegerRange()
    {
        var template = new CEnemyTemplate("A", "a.png", int.MaxValue, 1000, 1, 1000, 1);
        Enemy enemy = new EnemyFactory().Create(template, 10);
        Assert.Equal("2147483647" + new string('0', 30), enemy.MaxHitPoints.ToString());
        Assert.Equal("1" + new string('0', 30), enemy.GoldReward.ToString());
    }

    [Fact]
    public void Factory_RejectsNullTemplateAndNegativeVictories()
    {
        var factory = new EnemyFactory();
        var template = new CEnemyTemplate("A", "a.png", 1, 1, 0, 1, 1);
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(template, -1));
    }
}
