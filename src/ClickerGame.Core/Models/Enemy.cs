using ClickerGame.Core.Numbers;

namespace ClickerGame.Core.Models;

/// <summary>Конкретный противник в бою. Не изменяет шаблон из редактора.</summary>
public sealed class Enemy
{
    public string Name { get; private set; }
    public BigNumber MaxHitPoints { get; private set; }
    public BigNumber CurrentHitPoints { get; private set; }
    public BigNumber GoldReward { get; private set; }
    public bool IsDead { get; private set; }

    // В Core хранится имя иконки. Создание WPF-изображения будет задачей интерфейса.
    public string Icon { get; private set; }

    public Enemy(string name, BigNumber maxHitPoints, BigNumber goldReward, string icon)
    {
        ArgumentNullException.ThrowIfNull(maxHitPoints);
        ArgumentNullException.ThrowIfNull(goldReward);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("У противника должно быть имя.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(icon))
        {
            throw new ArgumentException("Необходимо указать имя иконки.", nameof(icon));
        }

        if (maxHitPoints <= BigNumber.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHitPoints),
                "Максимальное здоровье должно быть больше нуля.");
        }

        Name = name.Trim();
        Icon = icon.Trim();
        MaxHitPoints = maxHitPoints;
        CurrentHitPoints = maxHitPoints;
        GoldReward = goldReward;
        IsDead = false;
    }

    /// <summary>
    /// true означает, что именно эта атака победила противника.
    /// Награда возвращается один раз: повторная атака по мёртвому даёт false и ноль.
    /// </summary>
    public bool TakeDamage(BigNumber dmg, out BigNumber goldReward)
    {
        ArgumentNullException.ThrowIfNull(dmg);
        goldReward = BigNumber.Zero;
        if (IsDead)
        {
            return false;
        }

        if (dmg >= CurrentHitPoints)
        {
            // Не вычитаем из меньшего большее: BigNumber не хранит отрицательные числа.
            CurrentHitPoints = BigNumber.Zero;
            IsDead = true;
            goldReward = GoldReward;
            return true;
        }

        CurrentHitPoints = CurrentHitPoints - dmg;
        return false;
    }
}
