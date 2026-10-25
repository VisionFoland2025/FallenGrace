using UnityEngine;

namespace FallenGrace
{
    /// <summary>Летящий снаряд (Святая стрела). Попадания считаются по расстоянию — без физики.</summary>
    public class Projectile : MonoBehaviour
    {
        Vector3 _dir;
        float _speed, _damage, _life, _radius;
        int _pierce;
        readonly System.Collections.Generic.HashSet<Enemy> _hit = new();

        /// <summary>Собрать префаб снаряда. Вызывается из меню редактора.</summary>
        public static Projectile BuildObject()
        {
            var go = new GameObject("HolyBolt");
            var p = go.AddComponent<Projectile>();
            Art.Prim(PrimitiveType.Capsule, "Bolt", go.transform, Vector3.zero,
                new Vector3(0.18f, 0.45f, 0.18f), Art.Mat(new Color(1f, 0.85f, 0.4f), 5f))
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return p;
        }

        public void Fire(Vector3 pos, Vector3 dir, float speed, float damage, int pierce, float size)
        {
            transform.position = pos;
            _dir = dir.normalized;
            transform.rotation = Quaternion.LookRotation(_dir);
            transform.localScale = Vector3.one * size;
            _speed = speed;
            _damage = damage;
            _pierce = pierce;
            _radius = 0.35f * size;
            _life = 2.2f;
            _hit.Clear();
        }

        void Update()
        {
            transform.position += _dir * (_speed * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0f) { GameManager.I.Projectiles.Release(this); return; }

            var pos = transform.position;
            var list = Enemy.Active;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var e = list[i];
                if (_hit.Contains(e)) continue;
                var off = e.transform.position - pos;
                off.y = 0;
                float r = e.Radius + _radius;
                if (off.sqrMagnitude > r * r) continue;

                _hit.Add(e);
                e.TakeDamage(_damage, pos - _dir);
                if (--_pierce < 0)
                {
                    GameManager.I.Projectiles.Release(this);
                    return;
                }
            }
        }
    }
}
