using ClickerGame.Core.Numbers;

namespace ClickerGame.Core.Models;

/// <summary>
/// Состояние игрока и правила улучшений. Окно может читать свойства,
/// но начислять золото и покупать улучшения можно только через методы игрока.
/// </summary>
public sealed class Player
{
    public BigNumber Gold { get; private set; }
    public BigNumber Damage { get; private set; }
    public int Level { get; private set; }
    public BigNumber UpgradeCost { get; private set; }
    public double DamageModifier { get; private set; }
    public double UpgradeModifier { get; private set; }

    public Player()
        : this(new BigNumber(0), new BigNumber(10), new BigNumber(20), 1.2, 1.2)
    {
    }

    /// <summary>
    /// Дополнительный конструктор позволяет проверять правила на разных
    /// начальных значениях. Первоначальный уровень всегда равен 1.
    /// </summary>
    public Player(BigNumber initialGold, BigNumber initialDamage,
        BigNumber initialUpgradeCost, double damageModifier = 1.2,
        double upgradeModifier = 1.2)
    {
        ArgumentNullException.ThrowIfNull(initialGold);
        ArgumentNullException.ThrowIfNull(initialDamage);
        ArgumentNullException.ThrowIfNull(initialUpgradeCost);

        if (initialDamage <= BigNumber.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDamage),
                "Начальный урон должен быть больше нуля.");
        }

        if (initialUpgradeCost <= BigNumber.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialUpgradeCost),
                "Цена улучшения должна быть больше нуля.");
        }

        if (!double.IsFinite(damageModifier) || damageModifier <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(damageModifier),
                "Модификатор урона должен быть конечным числом больше 1.");
        }

        if (!double.IsFinite(upgradeModifier) || upgradeModifier <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(upgradeModifier),
                "Модификатор цены должен быть конечным числом больше 1.");
        }

        // BigNumber неизменяем, поэтому ссылки на исходные значения безопасны.
        Gold = initialGold;
        Damage = initialDamage;
        UpgradeCost = initialUpgradeCost;
        DamageModifier = damageModifier;
        UpgradeModifier = upgradeModifier;
        Level = 1;
    }

    public void AddGold(BigNumber amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        // Отрицательное amount невозможно: BigNumber не допускает такие значения.
        Gold = Gold + amount;
    }

    /// <summary>
    /// Возвращает урон одного клика. Получение урона и награда будут обязанностью Enemy.
    /// Это не позволяет игроку напрямую изменять здоровье противника.
    /// </summary>
    public BigNumber DealDamage() => Damage;

    /// <summary>
    /// false означает нехватку золота; состояние в таком случае не меняется.
    /// При успехе списывается старая цена и обновляются уровень, урон и следующая цена.
    /// </summary>
    public bool TryUpgrade()
    {
        if (Gold < UpgradeCost)
        {
            return false;
        }

        if (Level == int.MaxValue)
        {
            throw new InvalidOperationException("Достигнут максимальный номер уровня.");
        }

        // Все вычисления выполняются до списания золота: ошибка вычисления
        // не должна оставлять покупку оплаченной, но не применённой.
        BigNumber nextDamage = CalculateTotalDamage();
        BigNumber nextUpgradeCost = CalculateNextUpgradeCost();
        int nextLevel = Level + 1;

        if (!TrySpendGold(UpgradeCost))
        {
            return false;
        }

        RecalculateStats(nextDamage, nextUpgradeCost, nextLevel);
        return true;
    }

    private bool TrySpendGold(BigNumber amount)
    {
        if (Gold < amount)
        {
            return false;
        }

        Gold = Gold - amount;
        return true;
    }

    private BigNumber CalculateTotalDamage()
    {
        BigNumber nextDamage = Damage * DamageModifier;
        // Покупка должна приносить пользу даже при 1 * 1.01, когда дробь отбрасывается.
        return nextDamage > Damage ? nextDamage : Damage + BigNumber.One;
    }

    private BigNumber CalculateNextUpgradeCost()
    {
        // Формула: старая цена * (модификатор * текущий уровень).
        // Сначала умножаем BigNumber на целый уровень — это точно.
        // Затем применяем дробный коэффициент и один раз отбрасываем дробь.
        // Так не вычисляем произведение коэффициентов средствами double.
        BigNumber nextCost = (UpgradeCost * Level) * UpgradeModifier;
        return nextCost > UpgradeCost ? nextCost : UpgradeCost + BigNumber.One;
    }

    private void RecalculateStats(BigNumber nextDamage, BigNumber nextUpgradeCost, int nextLevel)
    {
        // Применяем уже рассчитанные значения после успешной оплаты.
        Damage = nextDamage;
        UpgradeCost = nextUpgradeCost;
        Level = nextLevel;
    }
}
