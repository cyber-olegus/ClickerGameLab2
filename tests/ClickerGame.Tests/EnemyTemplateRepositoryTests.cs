using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClickerGame.Core.Services;
using EnemyEditor.Core;
using Xunit;

namespace ClickerGame.Tests;

public sealed class EnemyTemplateRepositoryTests
{
    [Fact]
    public void EmptyRepository_RejectsSelection()
    {
        var repository = new EnemyTemplateRepository();
        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.Templates);
        Assert.Empty(repository.NormalizedChances);
        Assert.Throws<InvalidOperationException>(() => repository.SelectRandom());
    }

    [Fact]
    public void Load_ReadsActualExampleFromLab1()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJson(Path.Combine(AppContext.BaseDirectory, "Data", "enemies.example.json"));
        Assert.Equal(3, repository.Count);
        Assert.Equal("Гоблин-разведчик", repository.Templates[0].Name);
        Assert.Equal("goblin_1.png", repository.Templates[0].IconName);
        Assert.Equal(80, repository.Templates[0].BaseLife);
        Assert.Equal(7, repository.Templates[0].BaseGold);
        Assert.Equal(1.1, repository.Templates[0].LifeModifier);
        Assert.Equal(1.05, repository.Templates[0].GoldModifier);
        Assert.Equal("Тролль", repository.FindByChance(0.99).Name);
    }

    [Fact]
    public void Normalize_UsesSeparateProbabilitiesWithoutChangingTemplates()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("A", 0.2), ("B", 0.3)));
        Assert.Equal(0.4, repository.NormalizedChances[0], 12);
        Assert.Equal(0.6, repository.NormalizedChances[1], 12);
        Assert.Equal(1.0, repository.NormalizedChances.Sum(), 12);
        Assert.Equal(0.2, repository.Templates[0].SpawnChance);
        Assert.Equal(0.3, repository.Templates[1].SpawnChance);
    }

    [Theory]
    [InlineData(0.0, "A")]
    [InlineData(0.249, "A")]
    [InlineData(0.25, "B")]
    [InlineData(0.749, "B")]
    [InlineData(0.75, "C")]
    [InlineData(0.999999, "C")]
    public void FindByChance_UsesNonoverlappingIntervals(double chance, string name)
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("A", 0.25), ("B", 0.5), ("C", 0.25)));
        Assert.Equal(name, repository.FindByChance(chance).Name);
    }

    [Fact]
    public void ZeroWeightTemplates_AreNeverSelectedEvenAtZero()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("Zero1", 0), ("A", 1), ("Zero2", 0)));
        for (int index = 0; index < 100; index++)
        {
            Assert.Equal("A", repository.FindByChance(index / 100.0).Name);
        }
    }

    [Fact]
    public void Selection_HandlesRoundingGapBeforeOne()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("A", 1), ("B", 1), ("C", 1), ("D", 1),
            ("E", 1), ("F", 1), ("G", 1), ("Zero", 0)));
        Assert.Equal("G", repository.FindByChance(Math.BitDecrement(1.0)).Name);
    }

    [Fact]
    public void Normalize_AcceptsTinyPositiveSum()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("A", double.Epsilon), ("B", double.Epsilon)));
        Assert.Equal(0.5, repository.NormalizedChances[0]);
        Assert.Equal("B", repository.FindByChance(0.5).Name);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FindByChance_RejectsValuesOutsideHalfOpenInterval(double chance)
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("A", 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => repository.FindByChance(chance));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[1]")]
    [InlineData("[null]")]
    [InlineData("broken json")]
    public void Load_RejectsInvalidRootOrEmptyList(string json)
    {
        Assert.Throws<JsonException>(() => new EnemyTemplateRepository().LoadFromJsonText(json));
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("IconName")]
    [InlineData("BaseLife")]
    [InlineData("LifeModifier")]
    [InlineData("BaseGold")]
    [InlineData("GoldModifier")]
    [InlineData("SpawnChance")]
    public void Load_RequiresAllSevenProperties(string property)
    {
        JsonArray document = JsonNode.Parse(Json(("A", 1)))!.AsArray();
        document[0]!.AsObject().Remove(property);
        Assert.Throws<JsonException>(() => new EnemyTemplateRepository().LoadFromJsonText(document.ToJsonString()));
    }

    [Theory]
    [InlineData("Name", "\" \"")]
    [InlineData("Name", "null")]
    [InlineData("IconName", "\"\"")]
    [InlineData("BaseLife", "0")]
    [InlineData("BaseLife", "-1")]
    [InlineData("BaseLife", "1.5")]
    [InlineData("BaseLife", "2147483648")]
    [InlineData("BaseLife", "\"80\"")]
    [InlineData("BaseGold", "-1")]
    [InlineData("LifeModifier", "0")]
    [InlineData("LifeModifier", "-1")]
    [InlineData("LifeModifier", "1e309")]
    [InlineData("GoldModifier", "-1")]
    [InlineData("SpawnChance", "-0.1")]
    [InlineData("SpawnChance", "1.1")]
    [InlineData("SpawnChance", "\"bad\"")]
    public void Load_RejectsInvalidPropertyValues(string property, string badValue)
    {
        JsonArray document = JsonNode.Parse(Json(("A", 1)))!.AsArray();
        document[0]![property] = JsonNode.Parse(badValue);
        Assert.Throws<JsonException>(() => new EnemyTemplateRepository().LoadFromJsonText(document.ToJsonString()));
    }

    [Fact]
    public void Load_RejectsAllZeroChancesAndDuplicateNames()
    {
        var repository = new EnemyTemplateRepository();
        Assert.Throws<JsonException>(() => repository.LoadFromJsonText(Json(("A", 0), ("B", 0))));
        Assert.Throws<JsonException>(() => repository.LoadFromJsonText(Json(("A", 1), (" a ", 1))));
    }

    [Fact]
    public void FailedLoad_KeepsPreviousValidCollection()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("Original", 0.5)));
        Assert.Throws<JsonException>(() => repository.LoadFromJsonText(Json(("A", 1), (" ", 1))));
        Assert.Throws<JsonException>(() => repository.LoadFromJsonText(Json(("Zero", 0))));
        Assert.Equal(1, repository.Count);
        Assert.Equal("Original", repository.SelectRandom().Name);
        Assert.Equal(1.0, repository.NormalizedChances[0]);
    }

    [Fact]
    public void SuccessfulReload_ReplacesCollectionInsteadOfAppending()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("A", 1), ("B", 1)));
        repository.LoadFromJsonText(Json(("New", 0.1)));
        Assert.Equal(1, repository.Count);
        Assert.Equal("New", repository.FindByChance(0).Name);
        Assert.Equal(1.0, repository.NormalizedChances[0]);
    }

    [Fact]
    public void Collections_CannotBeMutatedThroughPublicProperties()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("A", 1)));
        Assert.Throws<NotSupportedException>(() => ((IList<double>)repository.NormalizedChances)[0] = 0);
        Assert.Throws<NotSupportedException>(() => ((IList<CEnemyTemplate>)repository.Templates).Clear());
        Assert.Equal("A", repository.SelectRandom().Name);
    }

    [Fact]
    public void Selection_DeterministicGridMatchesWeights()
    {
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(Json(("A", 0.1), ("B", 0.3), ("C", 0.6)));
        var counts = new Dictionary<string, int> { ["A"] = 0, ["B"] = 0, ["C"] = 0 };
        for (int index = 0; index < 1000; index++)
        {
            counts[repository.FindByChance((index + 0.5) / 1000).Name]++;
        }

        Assert.Equal(100, counts["A"]);
        Assert.Equal(300, counts["B"]);
        Assert.Equal(600, counts["C"]);
    }

    [Fact]
    public void SelectRandom_UsesProvidedGenerator()
    {
        var repository = new EnemyTemplateRepository(new FixedRandom(0.8));
        repository.LoadFromJsonText(Json(("A", 0.25), ("B", 0.5), ("C", 0.25)));
        Assert.Equal("C", repository.SelectRandom().Name);
    }

    [Fact]
    public void Loader_AcceptsCommentsAndTrailingCommaLikeLab1()
    {
        string json = Json(("A", 1));
        json = json[..^1] + ", /* комментарий */ ]";
        var repository = new EnemyTemplateRepository();
        repository.LoadFromJsonText(json);
        Assert.Equal("A", repository.SelectRandom().Name);
    }

    [Fact]
    public void Loader_ReportsMissingFileAndInvalidArguments()
    {
        var repository = new EnemyTemplateRepository();
        Assert.Throws<ArgumentNullException>(() => repository.LoadFromJsonText(null!));
        Assert.Throws<ArgumentException>(() => repository.LoadFromJson(" "));
        string missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        Assert.Throws<FileNotFoundException>(() => repository.LoadFromJson(missingFile));
    }

    private static string Json(params (string Name, double Chance)[] templates)
    {
        return JsonSerializer.Serialize(templates.Select(template => new
        {
            template.Name,
            IconName = "goblin_1.png",
            BaseLife = 80,
            LifeModifier = 1.1,
            BaseGold = 7,
            GoldModifier = 1.05,
            SpawnChance = template.Chance
        }));
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
