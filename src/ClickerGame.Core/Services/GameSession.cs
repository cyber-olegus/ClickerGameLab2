using ClickerGame.Core.Models;
using ClickerGame.Core.Numbers;

namespace ClickerGame.Core.Services;

/// <summary>Связывает игровые классы. Окно вызывает команды и читает результат.</summary>
public sealed class GameSession
{
    private readonly Random random;
    private readonly EnemyFactory enemyFactory = new();
    private EnemyTemplateRepository? repository;

    public Player Player { get; private set; } = new();
    public Enemy? CurrentEnemy { get; private set; }
    public int Victories { get; private set; }
    public bool IsStarted => repository is not null;
    public int TemplateCount => repository?.Count ?? 0;
    public bool CanUpgrade => IsStarted && Player.Gold >= Player.UpgradeCost;

    public GameSession(Random? random = null)
    {
        this.random = random ?? new Random();
    }

    public void LoadFromJson(string path)
    {
        var candidate = new EnemyTemplateRepository(random);
        candidate.LoadFromJson(path);
        Start(candidate);
    }

    public void LoadFromJsonText(string json)
    {
        var candidate = new EnemyTemplateRepository(random);
        candidate.LoadFromJsonText(json);
        Start(candidate);
    }

    public void Restart()
    {
        if (repository is null)
        {
            throw new InvalidOperationException("Сначала откройте JSON с противниками.");
        }

        Start(repository);
    }

    /// <summary>Возвращает true только при победе; reward — награда этой атаки.</summary>
    public bool Attack(out BigNumber reward)
    {
        reward = BigNumber.Zero;
        if (repository is null || CurrentEnemy is null)
        {
            return false;
        }

        BigNumber damage = Player.DealDamage();
        Enemy? nextEnemy = null;
        if (damage >= CurrentEnemy.CurrentHitPoints && !CurrentEnemy.IsDead)
        {
            if (Victories == int.MaxValue)
            {
                throw new InvalidOperationException("Достигнуто максимальное количество побед.");
            }

            // Готовим следующего врага ДО изменения состояния боя.
            // Ошибка создания не должна оставить награду без нового врага.
            nextEnemy = enemyFactory.Create(repository.SelectRandom(), Victories + 1);
        }

        if (!CurrentEnemy.TakeDamage(damage, out reward))
        {
            return false;
        }

        Player.AddGold(reward);
        Victories++;
        CurrentEnemy = nextEnemy!;
        return true;
    }

    public bool TryUpgrade() => IsStarted && Player.TryUpgrade();

    private void Start(EnemyTemplateRepository source)
    {
        Enemy firstEnemy = enemyFactory.Create(source.SelectRandom(), 0);
        var newPlayer = new Player();
        // Заменяем игру только после успешной загрузки и создания первого врага.
        repository = source;
        Player = newPlayer;
        CurrentEnemy = firstEnemy;
        Victories = 0;
    }
}
