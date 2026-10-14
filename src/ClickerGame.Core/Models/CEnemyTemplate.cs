using System.Text.Json.Serialization;

namespace EnemyEditor.Core;

/// <summary>
/// Хранит неизменяемый шаблон противника будущей игры-кликера.
/// </summary>
public sealed class CEnemyTemplate
{
    [JsonInclude]
    public string Name { get; private set; }

    [JsonInclude]
    public string IconName { get; private set; }

    [JsonInclude]
    public int BaseLife { get; private set; }

    [JsonInclude]
    public double LifeModifier { get; private set; }

    [JsonInclude]
    public int BaseGold { get; private set; }

    [JsonInclude]
    public double GoldModifier { get; private set; }

    [JsonInclude]
    public double SpawnChance { get; private set; }

    public CEnemyTemplate(
        string name,
        string iconName,
        int baseLife,
        double lifeModifier,
        int baseGold,
        double goldModifier,
        double spawnChance)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Имя противника не должно быть пустым.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(iconName))
        {
            throw new ArgumentException("Необходимо выбрать иконку.", nameof(iconName));
        }

        if (baseLife <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseLife), "Базовое здоровье должно быть больше нуля.");
        }

        if (!double.IsFinite(lifeModifier) || lifeModifier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lifeModifier), "Модификатор здоровья должен быть больше нуля.");
        }

        if (baseGold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseGold), "Базовое золото не может быть отрицательным.");
        }

        if (!double.IsFinite(goldModifier) || goldModifier < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(goldModifier), "Модификатор золота не может быть отрицательным.");
        }

        if (!double.IsFinite(spawnChance) || spawnChance < 0 || spawnChance > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(spawnChance), "Шанс появления должен находиться в диапазоне от 0 до 1.");
        }

        Name = name.Trim();
        IconName = iconName.Trim();
        BaseLife = baseLife;
        LifeModifier = lifeModifier;
        BaseGold = baseGold;
        GoldModifier = goldModifier;
        SpawnChance = spawnChance;
    }

    public override string ToString()
    {
        return Name;
    }
}
