using System.IO;
using System.Text.Json;
using EnemyEditor.Core;

namespace ClickerGame.Core.Services;

/// <summary>Загружает JSON Лабы 1 и выбирает шаблон по нормализованным шансам.</summary>
public sealed class EnemyTemplateRepository
{
    private readonly Random random;

    public IReadOnlyList<CEnemyTemplate> Templates { get; private set; } =
        Array.AsReadOnly(Array.Empty<CEnemyTemplate>());

    public IReadOnlyList<double> NormalizedChances { get; private set; } =
        Array.AsReadOnly(Array.Empty<double>());

    public int Count => Templates.Count;

    public EnemyTemplateRepository(Random? random = null)
    {
        // Подмена генератора полезна для воспроизводимых тестов выбора шаблона.
        this.random = random ?? new Random();
    }

    public void LoadFromJson(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Укажите путь к JSON-файлу.", nameof(path));
        }

        LoadFromJsonText(File.ReadAllText(path));
    }

    public void LoadFromJsonText(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Корневой элемент должен быть массивом шаблонов противников.");
        }

        var loaded = new List<CEnemyTemplate>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            int position = loaded.Count + 1;
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException($"Шаблон №{position} должен быть JSON-объектом.");
            }

            CEnemyTemplate template;
            try
            {
                template = new CEnemyTemplate(
                    ReadString(element, "Name"),
                    ReadString(element, "IconName"),
                    ReadInt(element, "BaseLife"),
                    ReadDouble(element, "LifeModifier"),
                    ReadInt(element, "BaseGold"),
                    ReadDouble(element, "GoldModifier"),
                    ReadDouble(element, "SpawnChance"));
            }
            catch (Exception exception) when (exception is ArgumentException or JsonException)
            {
                throw new JsonException($"Ошибка в шаблоне №{position}: {exception.Message}", exception);
            }

            if (!names.Add(template.Name))
            {
                throw new JsonException($"Имя противника «{template.Name}» повторяется.");
            }

            loaded.Add(template);
        }

        double[] chances = NormalizeChances(loaded);
        // Старое состояние заменяется только после проверки ВСЕГО файла и шансов.
        // Неудачная загрузка не должна разрушать ранее загруженную коллекцию.
        Templates = loaded.AsReadOnly();
        NormalizedChances = Array.AsReadOnly(chances);
    }

    public CEnemyTemplate SelectRandom() => FindByChance(random.NextDouble());

    /// <summary>Принимает случайную величину в диапазоне [0, 1), без правой границы.</summary>
    public CEnemyTemplate FindByChance(double chance)
    {
        if (!double.IsFinite(chance) || chance < 0 || chance >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chance),
                "Выпавшая вероятность должна быть от 0 включительно до 1 не включительно.");
        }

        if (Count == 0)
        {
            throw new InvalidOperationException("Сначала загрузите шаблоны противников.");
        }

        double cumulative = 0;
        int lastPositive = -1;
        for (int index = 0; index < Count; index++)
        {
            double weight = NormalizedChances[index];
            if (weight == 0)
            {
                continue;
            }

            lastPositive = index;
            cumulative += weight;
            // Строгое < распределяет интервалы без пересечения и пропускает нулевые шансы.
            if (chance < cumulative)
            {
                return Templates[index];
            }
        }

        // Сумма double может получиться чуть меньше 1 из-за округления.
        // Последний шаблон с положительным шансом закрывает этот числовой зазор.
        if (lastPositive >= 0)
        {
            return Templates[lastPositive];
        }

        throw new InvalidOperationException("Нет шаблонов с положительным шансом появления.");
    }

    private static double[] NormalizeChances(IReadOnlyList<CEnemyTemplate> templates)
    {
        if (templates.Count == 0)
        {
            throw new JsonException("Список противников не должен быть пустым.");
        }

        double sum = templates.Sum(template => template.SpawnChance);
        if (!double.IsFinite(sum) || sum <= 0)
        {
            throw new JsonException("Хотя бы один противник должен иметь положительный шанс появления.");
        }

        // SpawnChance в неизменяемых шаблонах не трогаем: храним нормализованные
        // значения отдельно, не добавляя публичные set в класс из Лабы 1.
        return templates.Select(template => template.SpawnChance / sum).ToArray();
    }

    private static string ReadString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Свойство {name} должно быть строкой.");
        }

        return property.GetString()!;
    }

    private static int ReadInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out int value))
        {
            throw new JsonException($"Свойство {name} должно быть целым числом в диапазоне int.");
        }

        return value;
    }

    private static double ReadDouble(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetDouble(out double value)
            || !double.IsFinite(value))
        {
            throw new JsonException($"Свойство {name} должно быть конечным числом.");
        }

        return value;
    }
}
