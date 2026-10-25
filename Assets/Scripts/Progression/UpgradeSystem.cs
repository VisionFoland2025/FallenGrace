using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallenGrace
{
    public class UpgradeOption
    {
        public string Title;
        public string Desc;
        public string Tag;          // "НОВОЕ" или "Ур. 3"
        public GracePath Path;
        public Action Apply;
    }

    /// <summary>
    /// Выбор улучшений при повышении уровня: 3 случайных варианта Света или Тьмы.
    /// </summary>
    public class UpgradeSystem : MonoBehaviour
    {
        // ---------- Оружие ----------
        class WeaponInfo
        {
            public Type Type;
            public string Title;
            public GracePath Path;
            public string Desc;
            public string[] Levels;   // что даёт уровень 2..5
        }

        static readonly WeaponInfo[] WeaponInfos =
        {
            new()
            {
                Type = typeof(HolyBolt), Title = "Святая стрела", Path = GracePath.Light,
                Desc = "Стреляет святыми стрелами в ближайших врагов.",
                Levels = new[] { "+1 стрела, +урон", "Стрелы пробивают 1 врага, +урон", "+1 стрела, +урон", "Пробивают ещё 1 врага, +урон" }
            },
            new()
            {
                Type = typeof(RadiantAura), Title = "Сияющая аура", Path = GracePath.Light,
                Desc = "Святой свет жжёт всех врагов вокруг героя.",
                Levels = new[] { "+15% радиус, +урон", "+15% радиус, +урон", "+15% радиус, +урон", "+15% радиус, +урон" }
            },
            new()
            {
                Type = typeof(ShadowBlades), Title = "Теневые клинки", Path = GracePath.Dark,
                Desc = "Клинки тьмы вращаются вокруг героя и режут врагов.",
                Levels = new[] { "+1 клинок, +урон", "+1 клинок, +урон", "+1 клинок, +урон", "+1 клинок, +урон" }
            },
            new()
            {
                Type = typeof(BloodNova), Title = "Кровавая волна", Path = GracePath.Dark,
                Desc = "Взрыв крови вокруг героя. Лечит за каждого задетого врага.",
                Levels = new[] { "Чаще, шире, +урон", "Чаще, шире, +урон", "Чаще, шире, +урон", "Чаще, шире, +урон" }
            },
        };

        // ---------- Пассивные умения ----------
        class PassiveInfo
        {
            public string Id;
            public string Title;
            public GracePath Path;
            public string Desc;
            public Action<PlayerController> Apply;
        }

        static readonly PassiveInfo[] Passives =
        {
            new() { Id = "hp", Title = "Благословение", Path = GracePath.Light, Desc = "+20 к макс. здоровью и лечение",
                Apply = p => { p.MaxHP += 20; p.Heal(20); } },
            new() { Id = "cd", Title = "Святое рвение", Path = GracePath.Light, Desc = "Оружие срабатывает на 8% быстрее",
                Apply = p => p.CooldownMult *= 0.92f },
            new() { Id = "speed", Title = "Небесная поступь", Path = GracePath.Light, Desc = "+10% к скорости движения",
                Apply = p => p.MoveSpeed *= 1.1f },
            new() { Id = "dmg", Title = "Ярость бездны", Path = GracePath.Dark, Desc = "+15% к урону",
                Apply = p => p.DamageMult += 0.15f },
            new() { Id = "magnet", Title = "Жадность тьмы", Path = GracePath.Dark, Desc = "+40% радиус сбора душ",
                Apply = p => p.PickupRadius *= 1.4f },
            new() { Id = "regen", Title = "Кровавая плоть", Path = GracePath.Dark, Desc = "+0.6 здоровья в секунду",
                Apply = p => p.Regen += 0.6f },
            new() { Id = "area", Title = "Тень крыльев", Path = GracePath.Dark, Desc = "+10% к размеру атак",
                Apply = p => p.AreaMult += 0.1f },
        };

        const int PassiveMaxLevel = 5;
        readonly Dictionary<string, int> _passiveLevels = new();
        int _pending;

        GameManager GM => GameManager.I;
        PlayerController P => GameManager.I.Player;

        public void QueueLevelUp()
        {
            _pending++;
            if (!GM.IsChoosing) ShowNext();
        }

        void ShowNext()
        {
            if (_pending <= 0 || GM.IsOver) return;
            _pending--;
            GM.IsChoosing = true;
            GM.UpdateTimeScale();
            GM.UI.ShowLevelUp(P.Level, Roll(3), Choose);
        }

        void Choose(UpgradeOption o)
        {
            o.Apply();
            if (o.Path == GracePath.Light) P.LightPoints++;
            else P.DarkPoints++;

            GM.UI.HideLevelUp();
            GM.IsChoosing = false;
            GM.UpdateTimeScale();
            P.NotifyChanged();
            if (_pending > 0) ShowNext();
        }

        List<UpgradeOption> Roll(int count)
        {
            var all = new List<UpgradeOption>();

            foreach (var info in WeaponInfos)
            {
                var owned = P.GetWeapon(info.Type);
                var wi = info;
                if (owned == null)
                {
                    all.Add(new UpgradeOption
                    {
                        Title = wi.Title, Desc = wi.Desc, Tag = "НОВОЕ", Path = wi.Path,
                        Apply = () => P.AddWeapon(wi.Type)
                    });
                }
                else if (owned.Level < Weapon.MaxLevel)
                {
                    all.Add(new UpgradeOption
                    {
                        Title = wi.Title, Desc = wi.Levels[owned.Level - 1], Tag = $"Ур. {owned.Level + 1}", Path = wi.Path,
                        Apply = () => owned.LevelUp()
                    });
                }
            }

            foreach (var ps in Passives)
            {
                _passiveLevels.TryGetValue(ps.Id, out int lvl);
                if (lvl >= PassiveMaxLevel) continue;
                var pi = ps;
                all.Add(new UpgradeOption
                {
                    Title = pi.Title, Desc = pi.Desc, Tag = lvl == 0 ? "НОВОЕ" : $"Ур. {lvl + 1}", Path = pi.Path,
                    Apply = () =>
                    {
                        _passiveLevels[pi.Id] = lvl + 1;
                        pi.Apply(P);
                    }
                });
            }

            // Перемешиваем и берём первые count
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            if (all.Count > count) all.RemoveRange(count, all.Count - count);

            if (all.Count == 0)
            {
                all.Add(new UpgradeOption
                {
                    Title = "Исцеление", Desc = "Восстановить 50% здоровья", Tag = "", Path = GracePath.Light,
                    Apply = () => P.Heal(P.MaxHP * 0.5f)
                });
            }
            return all;
        }
    }
}
