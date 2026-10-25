using System.Collections.Generic;
using UnityEngine;

namespace FallenGrace
{
    /// <summary>Теневые клинки: вращаются вокруг героя и режут всех, кого заденут.</summary>
    public class ShadowBlades : Weapon
    {
        readonly List<Transform> _blades = new();
        readonly Dictionary<Enemy, float> _lastHit = new();
        float _angle;

        int Count => 1 + Level;
        float Damage => 8f + 4f * (Level - 1);
        float Orbit => 2.6f * P.AreaMult;
        float Speed => 180f + 25f * (Level - 1);

        protected override void OnLevelChanged()
        {
            var mat = Art.Mat(new Color(0.45f, 0.1f, 0.6f), 3f);
            while (_blades.Count < Count)
            {
                var b = Art.Prim(PrimitiveType.Cube, "ShadowBlade", null, Vector3.zero,
                    new Vector3(0.15f, 0.08f, 1.1f), mat).transform;
                _blades.Add(b);
            }
        }

        void Update()
        {
            if (!Paused) _angle += Speed * Time.deltaTime;
            var c = P.transform.position + Vector3.up * 0.9f;
            float orbit = Orbit;
            float size = P.AreaMult;

            for (int i = 0; i < _blades.Count; i++)
            {
                float a = (_angle + 360f / _blades.Count * i) * Mathf.Deg2Rad;
                var pos = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * orbit;
                var b = _blades[i];
                b.position = pos;
                b.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)));
                b.localScale = new Vector3(0.15f, 0.08f, 1.1f) * size;
            }
            if (Paused) return;

            float now = Time.time;
            var list = Enemy.Active;
            float hitR = 0.6f * size;
            foreach (var b in _blades)
            {
                var bp = b.position;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (i >= list.Count) continue;
                    var e = list[i];
                    var off = e.transform.position - bp;
                    off.y = 0;
                    float rr = hitR + e.Radius;
                    if (off.sqrMagnitude > rr * rr) continue;
                    if (_lastHit.TryGetValue(e, out var t) && now - t < 0.35f) continue;
                    _lastHit[e] = now;
                    e.TakeDamage(Dmg(Damage), c, 5f);
                }
            }
            if (_lastHit.Count > 800) _lastHit.Clear();
        }

        void OnDestroy()
        {
            foreach (var b in _blades) if (b != null) Destroy(b.gameObject);
        }
    }
}
