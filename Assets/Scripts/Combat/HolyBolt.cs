using System.Collections.Generic;
using UnityEngine;

namespace FallenGrace
{
    /// <summary>Святая стрела: стреляет в ближайших врагов.</summary>
    public class HolyBolt : Weapon
    {
        float _timer;

        int Count => 1 + (Level >= 2 ? 1 : 0) + (Level >= 4 ? 1 : 0);
        int Pierce => (Level >= 3 ? 1 : 0) + (Level >= 5 ? 1 : 0);
        float Damage => 10f + 4f * (Level - 1);
        float Cooldown => 1.1f - 0.08f * (Level - 1);

        void Update()
        {
            if (Paused) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            var origin = P.transform.position + Vector3.up * 1.2f;
            var target = Enemy.Nearest(P.transform.position, 18f);
            if (target == null) return;
            _timer = CD(Cooldown);

            var baseDir = target.transform.position + Vector3.up * target.Radius - origin;
            baseDir.y = 0f;
            for (int i = 0; i < Count; i++)
            {
                float angle = (i - (Count - 1) / 2f) * 12f;
                var dir = Quaternion.Euler(0, angle, 0) * baseDir;
                GameManager.I.Projectiles.Get().Fire(origin, dir, 16f, Dmg(Damage), Pierce, P.AreaMult);
            }
        }
    }
}
