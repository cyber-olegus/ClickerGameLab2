using System.IO;
using System.Text.Json;
using ClickerGame.Core.Models;
using ClickerGame.Core.Numbers;
using ClickerGame.Core.Services;
using Xunit;

namespace ClickerGame.Tests;

public sealed class GameSessionTests
{
    [Fact]
    public void BeforeLoading_CommandsDoNotChangePlayer()
    {
        var game = new GameSession();
        Assert.False(game.IsStarted);
        Assert.Null(game.CurrentEnemy);
        Assert.Equal(0, game.TemplateCount);
        Assert.False(game.CanUpgrade);
        Assert.False(game.Attack(out BigNumber reward));
        Assert.Equal(BigNumber.Zero, reward);
        Assert.False(game.TryUpgrade());
        Assert.Equal("0", game.Player.Gold.ToString());
        Assert.Equal("10", game.Player.Damage.ToString());
        Assert.Throws<InvalidOperationException>(() => game.Restart());
    }

    [Fact]
    public void LoadFromFile_ReadsCopiedExampleFromLab1()
    {
        var game = new GameSession(new FixedRandom(0));
        game.LoadFromJson(Path.Combine(AppContext.BaseDirectory, "Data", "enemies.example.json"));
        Assert.True(game.IsStarted);
        Assert.Equal(3, game.TemplateCount);
        Assert.Equal("Гоблин-разведчик", game.CurrentEnemy!.Name);
        Assert.Equal("80", game.CurrentEnemy.CurrentHitPoints.ToString());
        Assert.Equal("goblin_1.png", game.CurrentEnemy.Icon);
    }

    [Fact]
    public void Load_StartsWithBaseStatsAndFreshPlayer()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(health: 80, gold: 7, lifeModifier: 2, goldModifier: 3));
        Assert.True(game.IsStarted);
        Assert.Equal(1, game.TemplateCount);
        Assert.Equal(0, game.Victories);
        Assert.Equal("80", game.CurrentEnemy!.MaxHitPoints.ToString());
        Assert.Equal("7", game.CurrentEnemy.GoldReward.ToString());
        Assert.Equal("0", game.Player.Gold.ToString());
        Assert.Equal("10", game.Player.Damage.ToString());
        Assert.Equal("20", game.Player.UpgradeCost.ToString());
        Assert.Equal(1, game.Player.Level);
    }

    [Fact]
    public void NonlethalAttack_OnlyChangesCurrentHealth()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json());
        Enemy original = game.CurrentEnemy!;
        Assert.False(game.Attack(out BigNumber reward));
        Assert.Same(original, game.CurrentEnemy);
        Assert.Equal("10", original.CurrentHitPoints.ToString());
        Assert.Equal("20", original.MaxHitPoints.ToString());
        Assert.Equal("0", reward.ToString());
        Assert.Equal("0", game.Player.Gold.ToString());
        Assert.Equal(0, game.Victories);
    }

    [Fact]
    public void Victory_CreditsOldRewardAndSpawnsScaledEnemy()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(lifeModifier: 2, goldModifier: 2));
        Enemy original = game.CurrentEnemy!;
        game.Attack(out _);
        Assert.True(game.Attack(out BigNumber reward));
        Assert.True(original.IsDead);
        Assert.Equal("0", original.CurrentHitPoints.ToString());
        Assert.Equal("7", reward.ToString());
        Assert.Equal("7", game.Player.Gold.ToString());
        Assert.Equal(1, game.Victories);
        Assert.NotSame(original, game.CurrentEnemy);
        Assert.False(game.CurrentEnemy!.IsDead);
        Assert.Equal("40", game.CurrentEnemy.CurrentHitPoints.ToString());
        Assert.Equal("14", game.CurrentEnemy.GoldReward.ToString());
    }

    [Fact]
    public void TwoVictories_CreditEachEnemyOnceAndUseGlobalVictoryCount()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(lifeModifier: 2, goldModifier: 2));
        // Две атаки по первому врагу (20 HP), четыре по второму (40 HP).
        for (int index = 0; index < 6; index++)
        {
            game.Attack(out _);
        }

        Assert.Equal(2, game.Victories);
        Assert.Equal("21", game.Player.Gold.ToString());
        Assert.Equal("80", game.CurrentEnemy!.CurrentHitPoints.ToString());
        Assert.Equal("28", game.CurrentEnemy.GoldReward.ToString());
        Assert.False(game.Attack(out BigNumber reward));
        Assert.Equal("0", reward.ToString());
        Assert.Equal("21", game.Player.Gold.ToString());
    }

    [Fact]
    public void Overkill_CannotMakeHealthNegative()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(health: 1));
        Enemy original = game.CurrentEnemy!;
        Assert.True(game.Attack(out _));
        Assert.Equal("0", original.CurrentHitPoints.ToString());
        Assert.Equal("1", game.CurrentEnemy!.CurrentHitPoints.ToString());
        Assert.Equal(1, game.Victories);
    }

    [Fact]
    public void ZeroReward_StillCountsAsVictory()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(health: 10, gold: 0));
        Assert.True(game.Attack(out BigNumber reward));
        Assert.Equal(BigNumber.Zero, reward);
        Assert.Equal(BigNumber.Zero, game.Player.Gold);
        Assert.Equal(1, game.Victories);
        Assert.False(game.CurrentEnemy!.IsDead);
    }

    [Fact]
    public void Upgrade_SpendsGoldAndChangesFollowingAttack()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(health: 13, gold: 20));
        Assert.False(game.CanUpgrade);
        game.Attack(out _);
        game.Attack(out _);
        Assert.True(game.CanUpgrade);
        Assert.True(game.TryUpgrade());
        Assert.Equal("0", game.Player.Gold.ToString());
        Assert.Equal("12", game.Player.Damage.ToString());
        Assert.Equal("24", game.Player.UpgradeCost.ToString());
        Assert.Equal(2, game.Player.Level);
        Assert.False(game.CanUpgrade);
        Assert.False(game.Attack(out _));
        Assert.Equal("1", game.CurrentEnemy!.CurrentHitPoints.ToString());
        Assert.Equal("13", game.CurrentEnemy.MaxHitPoints.ToString());
    }

    [Fact]
    public void UnaffordableUpgrade_PreservesCombatAndPlayer()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(health: 10, gold: 7));
        game.Attack(out _);
        Enemy current = game.CurrentEnemy!;
        Assert.False(game.TryUpgrade());
        Assert.Equal("7", game.Player.Gold.ToString());
        Assert.Equal("10", game.Player.Damage.ToString());
        Assert.Equal("20", game.Player.UpgradeCost.ToString());
        Assert.Equal(1, game.Player.Level);
        Assert.Same(current, game.CurrentEnemy);
        Assert.Equal("10", current.CurrentHitPoints.ToString());
    }

    [Fact]
    public void Restart_ResetsGoldDamageAndVictoriesButKeepsTemplates()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(health: 10, gold: 20, lifeModifier: 2));
        game.Attack(out _);
        game.TryUpgrade();
        Enemy previousEnemy = game.CurrentEnemy!;
        Player previousPlayer = game.Player;
        game.Restart();
        Assert.True(game.IsStarted);
        Assert.Equal(1, game.TemplateCount);
        Assert.Equal(0, game.Victories);
        Assert.NotSame(previousPlayer, game.Player);
        Assert.NotSame(previousEnemy, game.CurrentEnemy);
        Assert.Equal("0", game.Player.Gold.ToString());
        Assert.Equal("10", game.Player.Damage.ToString());
        Assert.Equal("20", game.Player.UpgradeCost.ToString());
        Assert.Equal(1, game.Player.Level);
        Assert.Equal("10", game.CurrentEnemy!.CurrentHitPoints.ToString());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("broken json")]
    public void FailedLoad_PreservesExistingGameIncludingDamagedEnemy(string invalidJson)
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(health: 30, gold: 20));
        for (int index = 0; index < 3; index++)
        {
            game.Attack(out _);
        }

        game.TryUpgrade();
        game.Attack(out _);
        Player player = game.Player;
        Enemy enemy = game.CurrentEnemy!;
        Assert.ThrowsAny<JsonException>(() => game.LoadFromJsonText(invalidJson));
        Assert.Same(player, game.Player);
        Assert.Same(enemy, game.CurrentEnemy);
        Assert.Equal("18", enemy.CurrentHitPoints.ToString());
        Assert.Equal("12", player.Damage.ToString());
        Assert.Equal(2, player.Level);
        Assert.Equal(1, game.Victories);
        Assert.Equal(1, game.TemplateCount);
    }

    [Fact]
    public void MissingFile_PreservesLoadedGame()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json());
        Enemy enemy = game.CurrentEnemy!;
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        Assert.Throws<FileNotFoundException>(() => game.LoadFromJson(path));
        Assert.True(game.IsStarted);
        Assert.Same(enemy, game.CurrentEnemy);
    }

    [Fact]
    public void FailedFirstLoad_DoesNotEnableGameCommands()
    {
        var game = new GameSession();
        Assert.Throws<JsonException>(() => game.LoadFromJsonText("[]"));
        Assert.False(game.IsStarted);
        Assert.Null(game.CurrentEnemy);
        Assert.False(game.TryUpgrade());
    }

    [Fact]
    public void SuccessfulReload_ReplacesTemplatesAndResetsPlayer()
    {
        var game = new GameSession();
        game.LoadFromJsonText(Json(health: 10, gold: 20));
        game.Attack(out _);
        game.TryUpgrade();
        game.LoadFromJsonText(Json(name: "B", health: 70, gold: 50));
        Assert.Equal("B", game.CurrentEnemy!.Name);
        Assert.Equal("70", game.CurrentEnemy.CurrentHitPoints.ToString());
        Assert.Equal("50", game.CurrentEnemy.GoldReward.ToString());
        Assert.Equal(0, game.Victories);
        Assert.Equal("0", game.Player.Gold.ToString());
        Assert.Equal(1, game.Player.Level);
    }

    [Fact]
    public void NewEnemy_UsesWeightedSelectionAndSelectedTemplatesOwnModifiers()
    {
        string json = JsonSerializer.Serialize(new[]
        {
            new { Name = "A", IconName = "a.png", BaseLife = 10, LifeModifier = 2.0,
                BaseGold = 7, GoldModifier = 2.0, SpawnChance = 0.25 },
            new { Name = "B", IconName = "b.png", BaseLife = 50, LifeModifier = 3.0,
                BaseGold = 2, GoldModifier = 3.0, SpawnChance = 0.75 }
        });
        var game = new GameSession(new SequenceRandom(0, 0.8));
        game.LoadFromJsonText(json);
        Assert.Equal("A", game.CurrentEnemy!.Name);
        Assert.True(game.Attack(out BigNumber reward));
        Assert.Equal("7", reward.ToString());
        Assert.Equal("B", game.CurrentEnemy!.Name);
        Assert.Equal("150", game.CurrentEnemy.CurrentHitPoints.ToString());
        Assert.Equal("6", game.CurrentEnemy.GoldReward.ToString());
        Assert.Equal("b.png", game.CurrentEnemy.Icon);
    }

    private static string Json(string name = "A", int health = 20, int gold = 7,
        double lifeModifier = 1, double goldModifier = 1)
    {
        return JsonSerializer.Serialize(new[]
        {
            new { Name = name, IconName = "goblin_1.png", BaseLife = health,
                LifeModifier = lifeModifier, BaseGold = gold,
                GoldModifier = goldModifier, SpawnChance = 1.0 }
        });
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }

    private sealed class SequenceRandom(params double[] values) : Random
    {
        private readonly Queue<double> remaining = new(values);
        public override double NextDouble() => remaining.Dequeue();
    }
}
