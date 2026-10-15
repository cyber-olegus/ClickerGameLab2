using ClickerGame.Core.Models;
using ClickerGame.Core.Numbers;
using EnemyEditor.Core;

namespace ClickerGame.Core.Services;

/// <summary>Создаёт нового противника из шаблона и общего количества побед игрока.</summary>
public sealed class EnemyFactory
{
    public Enemy Create(CEnemyTemplate template, int victories)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (victories < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(victories),
                "Количество побед не может быть отрицательным.");
        }

        BigNumber health = new(template.BaseLife);
        BigNumber reward = new(template.BaseGold);

        // При нуле побед используются базовые значения. Затем один шаг на победу.
        // Каждый шаг отбрасывает дробь по правилам BigNumber; Math.Pow не используется.
        // Характеристики всегда считаются от выбранного шаблона, а не от прошлого врага.
        for (int step = 0; step < victories; step++)
        {
            health = health * template.LifeModifier;
            reward = reward * template.GoldModifier;
        }

        // Редактор разрешает множитель здоровья меньше 1. После округления нельзя
        // создавать противника с нулевым здоровьем, которого невозможно корректно атаковать.
        if (health == BigNumber.Zero)
        {
            health = BigNumber.One;
        }

        return new Enemy(template.Name, health, reward, template.IconName);
    }
}
