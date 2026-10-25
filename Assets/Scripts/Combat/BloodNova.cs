using System.Collections.Generic;
using UnityEngine;

namespace FallenGrace
{
    /// <summary>Кровавая волна: взрыв вокруг героя, лечит за каждого задетого врага.</summary>
    public class BloodNova : Weapon
    {
        Transform _ring;
        float _timer = 1f;
        float _anim = -1f;

        float Radius => 3.5f * (1f + 0.12f * (Level - 1)) * P.AreaMult;
        float Damage => 20f + 12f * (Level - 1);
        float Cooldown => 3.5f - 0.3f * (Level - 1);

        protected override void OnLevelChanged()
        {
            if (_ring == null)
            {
                _ring = Art.Prim(PrimitiveType.Cylinder, "BloodNova", P.transform, new Vector3(0, 0.05f, 0),
                    Vector3.one, Art.Mat(new Color(0.8f, 0.05f, 0.08f), 2.5f)).transform;
                _ring.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (_anim >= 0f)
            {
                _anim += Time.deltaTime / 0.3f;
                float s = Mathf.Lerp(0.3f, 1f, _anim) * Radius * 2f;
                _ring.localScale = new Vector3(s, 0.01f, s);
                if (_anim >= 1f) { _anim = -1f; _ring.gameObject.SetActive(false); }
            }
            if (Paused) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CD(Cooldown);

            _anim = 0f;
            _ring.gameObject.SetActive(true);

            var c = P.transform.position;
            float r = Radius;
            var targets = Enemy.InRadius(c, r);
            int hits = targets.Count;
            foreach (var e in targets)
                e.TakeDamage(Dmg(Damage), c, 8f);
            if (hits > 0) P.Heal(Mathf.Min(hits * 0.6f, 12f));
        }
    }
}
