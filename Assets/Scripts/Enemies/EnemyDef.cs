using UnityEngine;

namespace FallenGrace
{
    public enum Faction { Demon, Angel }
    public enum Shape { Imp, Ghoul, Brute, Hunter, Boss }

    /// <summary>
    /// Тип врага — файл-настройка в Assets/Game/Enemies.
    /// Открой его в Inspector и меняй здоровье, скорость, урон без кода.
    /// Новый тип: ПКМ в Project → Create → Fallen Grace → Enemy.
    /// </summary>
    [CreateAssetMenu(menuName = "Fallen Grace/Enemy", fileName = "NewEnemy")]
    public class EnemyDef : ScriptableObject
    {
        [Header("Описание")]
        public string DisplayName = "Враг";
        public Faction Faction = Faction.Demon;
        public Shape Shape = Shape.Imp;
        public bool IsBoss;

        [Header("Характеристики")]
        public float HP = 10;
        public float Speed = 3;
        [Tooltip("Урон за удар (раз в 0.5 с при касании)")]
        public float Damage = 5;
        [Tooltip("Сколько опыта даёт осколок души")]
        public int XP = 1;
        [Tooltip("Множитель размера модели и коллайдера")]
        public float Size = 1;
        public Color Color = Color.red;

        [Header("Префаб (создаётся меню Fallen Grace)")]
        public Enemy Prefab;

        public void Set(string displayName, Faction faction, Shape shape, float hp, float speed,
            float damage, int xp, float size, Color color, bool boss = false)
        {
            DisplayName = displayName; Faction = faction; Shape = shape; HP = hp; Speed = speed;
            Damage = damage; XP = xp; Size = size; Color = color; IsBoss = boss;
        }
    }
}
