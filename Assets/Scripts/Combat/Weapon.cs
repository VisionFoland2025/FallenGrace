using UnityEngine;

namespace FallenGrace
{
    public enum GracePath { Light, Dark }

    /// <summary>
    /// Базовое оружие. Каждое оружие принадлежит пути Света или Тьмы
    /// и прокачивается до 5 уровня.
    /// </summary>
    public abstract class Weapon : MonoBehaviour
    {
        public const int MaxLevel = 5;
        public int Level { get; private set; } = 1;
        protected PlayerController P;

        public void Init(PlayerController p)
        {
            P = p;
            OnLevelChanged();
        }

        public void LevelUp()
        {
            if (Level >= MaxLevel) return;
            Level++;
            OnLevelChanged();
        }

        protected virtual void OnLevelChanged() { }

        protected float CD(float baseCd) => baseCd * P.CooldownMult;
        protected float Dmg(float baseDmg) => baseDmg * P.DamageMult;
        protected bool Paused => Time.timeScale == 0f || GameManager.I.IsOver;
    }
}
