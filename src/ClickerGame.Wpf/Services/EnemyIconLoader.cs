using System.IO;
using System.Windows.Media.Imaging;

namespace ClickerGame.Wpf.Services;

/// <summary>Ищет иконки Лабы 1 в выходной папке, включая вложенные каталоги.</summary>
public sealed class EnemyIconLoader
{
    private readonly string iconDirectory;
    private readonly Dictionary<string, BitmapSource> cache = new(StringComparer.OrdinalIgnoreCase);

    public EnemyIconLoader(string iconDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iconDirectory);
        this.iconDirectory = Path.GetFullPath(iconDirectory);
    }

    public BitmapSource? Load(string iconName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iconName);
        // JSON редактора хранит имя файла. Не разрешаем ему задавать внешний путь.
        if (Path.GetFileName(iconName) != iconName)
        {
            throw new InvalidDataException("В шаблоне должно быть имя иконки без пути к папке.");
        }

        if (cache.TryGetValue(iconName, out BitmapSource? cached))
        {
            return cached;
        }

        if (!Directory.Exists(iconDirectory))
        {
            return null;
        }

        string[] matches = Directory.EnumerateFiles(iconDirectory, "*", SearchOption.AllDirectories)
            .Where(path => string.Equals(Path.GetFileName(path), iconName, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        if (matches.Length == 0)
        {
            return null;
        }

        if (matches.Length > 1)
        {
            throw new InvalidDataException($"Найдено несколько иконок с именем {iconName}.");
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        // OnLoad освобождает файл: изображение остаётся в памяти, файл не блокируется.
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 256;
        bitmap.UriSource = new Uri(Path.GetFullPath(matches[0]), UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        cache[iconName] = bitmap;
        return bitmap;
    }
}
