using System.IO;
using System.Text.Json;
using System.Windows;
using ClickerGame.Core.Numbers;
using ClickerGame.Core.Services;
using ClickerGame.Wpf.Services;
using Microsoft.Win32;

namespace ClickerGame.Wpf;

public partial class MainWindow : Window
{
    private readonly GameSession session = new();
    private readonly EnemyIconLoader iconLoader = new(
        Path.Combine(AppContext.BaseDirectory, "Assets", "Icons"));
    private string? loadedFile;
    private string? displayedIcon;

    public MainWindow()
    {
        InitializeComponent();
        StatusText.Text = "Откройте JSON, сохранённый редактором из Лабы 1.";
        RefreshUi();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // После отмены выбора окно остаётся доступным; файл можно открыть кнопкой.
        Loaded -= Window_Loaded;
        ChooseFile();
    }

    private void LoadFile_Click(object sender, RoutedEventArgs e) => ChooseFile();

    private void ChooseFile()
    {
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        var dialog = new OpenFileDialog
        {
            Title = "Выберите шаблоны противников из Лабы 1",
            Filter = "Шаблоны противников (*.json)|*.json",
            DefaultExt = ".json",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = Directory.Exists(dataDirectory) ? dataDirectory : AppContext.BaseDirectory,
            FileName = "enemies.example.json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        RunCommand(() =>
        {
            session.LoadFromJson(dialog.FileName);
            loadedFile = dialog.FileName;
            displayedIcon = null;
            StatusText.Text = "Игра началась. Нажимайте на противника!";
        });
    }

    private void Restart_Click(object sender, RoutedEventArgs e)
    {
        RunCommand(() =>
        {
            session.Restart();
            displayedIcon = null;
            StatusText.Text = "Новая игра: золото, улучшения и количество побед сброшены.";
        });
    }

    private void Attack_Click(object sender, RoutedEventArgs e)
    {
        RunCommand(() =>
        {
            string enemyName = session.CurrentEnemy?.Name ?? "Противник";
            BigNumber damage = session.Player.DealDamage();
            bool defeated = session.Attack(out BigNumber reward);
            StatusText.Text = defeated
                ? $"Победа над «{enemyName}»! Получено золота: {reward}. Появился следующий противник."
                : $"Нанесено урона: {damage}.";
        });
    }

    private void Upgrade_Click(object sender, RoutedEventArgs e)
    {
        RunCommand(() =>
        {
            StatusText.Text = session.TryUpgrade()
                ? $"Улучшение куплено. Уровень: {session.Player.Level}, урон: {session.Player.Damage}."
                : "Недостаточно золота для улучшения.";
        });
    }

    private void RunCommand(Action command)
    {
        try
        {
            command();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or JsonException or ArgumentException or InvalidOperationException or OverflowException)
        {
            StatusText.Text = $"Не удалось выполнить действие: {exception.Message}";
            MessageBox.Show(this, exception.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            RefreshUi();
        }
    }

    private void RefreshUi()
    {
        // Окно только читает состояние. Здоровье, золото и уровень здесь не меняются.
        GoldText.Text = session.Player.Gold.ToString();
        DamageText.Text = session.Player.Damage.ToString();
        LevelText.Text = session.Player.Level.ToString();
        UpgradeCostText.Text = session.Player.UpgradeCost.ToString();
        VictoriesText.Text = session.Victories.ToString();
        TemplateCountText.Text = $"Загружено шаблонов: {session.TemplateCount}";
        SourceText.Text = loadedFile is null ? "Файл ещё не выбран." : $"Файл: {loadedFile}";
        RestartButton.IsEnabled = session.IsStarted;
        AttackButton.IsEnabled = session.IsStarted;
        UpgradeButton.IsEnabled = session.CanUpgrade;

        var enemy = session.CurrentEnemy;
        if (enemy is null)
        {
            EnemyNameText.Text = "Ожидание противника";
            EnemyHealthText.Text = "—";
            EnemyRewardText.Text = "—";
            EnemyImage.Source = null;
            IconPlaceholder.Visibility = Visibility.Visible;
            IconHintText.Text = "Выберите JSON, чтобы начать игру.";
            return;
        }

        EnemyNameText.Text = enemy.Name;
        EnemyHealthText.Text = $"{enemy.CurrentHitPoints} / {enemy.MaxHitPoints}";
        EnemyRewardText.Text = $"{enemy.GoldReward} золота";
        if (displayedIcon != enemy.Icon)
        {
            UpdateIcon(enemy.Icon);
        }
    }

    private void UpdateIcon(string iconName)
    {
        displayedIcon = iconName;
        EnemyImage.Source = null;
        try
        {
            EnemyImage.Source = iconLoader.Load(iconName);
            IconHintText.Text = EnemyImage.Source is null
                ? $"Иконка «{iconName}» не найдена. Для атаки нажимайте на знак вопроса."
                : "";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or ArgumentException or InvalidOperationException or NotSupportedException or FormatException)
        {
            // Ошибка изображения не мешает бою: доступна кнопка с заглушкой.
            IconHintText.Text = $"Не удалось открыть иконку: {exception.Message}";
        }

        IconPlaceholder.Visibility = EnemyImage.Source is null ? Visibility.Visible : Visibility.Collapsed;
    }
}
