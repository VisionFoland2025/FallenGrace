using System.Collections.Generic;
using UnityEngine;

namespace FallenGrace
{
    /// <summary>Сияющая аура: постоянно жжёт врагов рядом с героем.</summary>
    public class RadiantAura : Weapon
    {
        Transform _disk;
        float _timer;

        float Radius => 2.4f * (1f + 0.15f * (Level - 1)) * P.AreaMult;
        float Damage => 5f + 3f * (Level - 1);

        protected override void OnLevelChanged()
        {
            if (_disk == null)
                _disk = Art.Prim(PrimitiveType.Cylinder, "RadiantAura", P.transform, new Vector3(0, 0.03f, 0),
                    Vector3.one, Art.Mat(new Color(1f, 0.75f, 0.3f), 1.2f)).transform;
        }

        void Update()
        {
            float r = Radius;
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
            _disk.localScale = new Vector3(r * 2f * pulse, 0.01f, r * 2f * pulse);
            if (Paused) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CD(0.5f);

            var c = P.transform.position;
            foreach (var e in Enemy.InRadius(c, r))
                e.TakeDamage(Dmg(Damage), c, 1.5f);
        }
    }
}
