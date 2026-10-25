using System.Collections.Generic;
using UnityEngine;

namespace FallenGrace
{
    /// <summary>
    /// Враг: идёт на игрока, бьёт при касании, при смерти роняет осколок души (опыт).
    /// Толкаются друг с другом через физику, чтобы орда не слипалась в одну точку.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> Active = new();
        public static Enemy CurrentBoss;

        [SerializeField] EnemyDef _def;
        [SerializeField] Transform _visual;
        [SerializeField] Renderer[] _renderers;

        public EnemyDef Def => _def;
        public float HP { get; private set; }
        public float MaxHP { get; private set; }
        public float Radius { get; private set; }
        public bool IsAlive => _index >= 0;

        Pool<Enemy> _pool;
        Rigidbody _rb;
        MaterialPropertyBlock _mpb;
        float _flash;
        float _attackCd;
        Vector3 _knock;
        int _index = -1;
        float _speedMult = 1f;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _mpb = new MaterialPropertyBlock();
            Radius = 0.45f * _def.Size;
        }

        public void SetPool(Pool<Enemy> pool) => _pool = pool;

        // ---------- Сборка префаба (вызывается из меню редактора) ----------
        public static Enemy BuildObject(EnemyDef def)
        {
            var go = new GameObject(def.name);
            var e = go.AddComponent<Enemy>();
            e._def = def;
            float radius = 0.45f * def.Size;

            var col = go.AddComponent<SphereCollider>();
            col.radius = radius;
            col.center = new Vector3(0, radius, 0);

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = def.Size * def.Size * (def.IsBoss ? 20f : 1f);
            rb.linearDamping = 0f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.Interpolate;   // плавное движение при высоком FPS

            e._visual = new GameObject("Visual").transform;
            e._visual.SetParent(go.transform, false);
            e._visual.localScale = Vector3.one * def.Size;
            BuildShape(def, e._visual);
            e._renderers = e._visual.GetComponentsInChildren<Renderer>();
            return e;
        }

        static void BuildShape(EnemyDef def, Transform v)
        {
            var body = Art.Mat(def.Color, def.Faction == Faction.Angel ? 0.3f : 0f, 0.3f);
            var horn = Art.Mat(new Color(0.12f, 0.08f, 0.06f));
            var eyes = Art.Mat(new Color(1f, 0.6f, 0.1f), 4f);
            var gold = Art.Mat(new Color(1f, 0.8f, 0.35f), 2.5f);
            var dark = Art.Mat(new Color(0.15f, 0.12f, 0.12f));
            bool shadows = def.IsBoss;

            switch (def.Shape)
            {
                case Shape.Imp:
                    Art.Prim(PrimitiveType.Capsule, "Body", v, new Vector3(0, 0.6f, 0), new Vector3(0.7f, 0.6f, 0.7f), body);
                    Art.Prim(PrimitiveType.Cube, "HornL", v, new Vector3(-0.2f, 1.25f, 0.05f), new Vector3(0.1f, 0.35f, 0.1f), horn).transform.localRotation = Quaternion.Euler(0, 0, 25);
                    Art.Prim(PrimitiveType.Cube, "HornR", v, new Vector3(0.2f, 1.25f, 0.05f), new Vector3(0.1f, 0.35f, 0.1f), horn).transform.localRotation = Quaternion.Euler(0, 0, -25);
                    Art.Prim(PrimitiveType.Cube, "Eyes", v, new Vector3(0, 0.95f, 0.33f), new Vector3(0.35f, 0.07f, 0.05f), eyes);
                    break;

                case Shape.Ghoul:
                    Art.Prim(PrimitiveType.Capsule, "Body", v, new Vector3(0, 0.85f, 0.1f), new Vector3(0.7f, 0.85f, 0.6f), body).transform.localRotation = Quaternion.Euler(25, 0, 0);
                    Art.Prim(PrimitiveType.Cube, "ArmL", v, new Vector3(-0.42f, 0.7f, 0.35f), new Vector3(0.15f, 0.15f, 0.9f), body);
                    Art.Prim(PrimitiveType.Cube, "ArmR", v, new Vector3(0.42f, 0.7f, 0.35f), new Vector3(0.15f, 0.15f, 0.9f), body);
                    Art.Prim(PrimitiveType.Cube, "Eyes", v, new Vector3(0, 1.35f, 0.55f), new Vector3(0.3f, 0.06f, 0.05f), eyes);
                    break;

                case Shape.Brute:
                    Art.Prim(PrimitiveType.Cube, "Body", v, new Vector3(0, 0.6f, 0), new Vector3(1f, 1.1f, 0.8f), body);
                    Art.Prim(PrimitiveType.Sphere, "Head", v, new Vector3(0, 1.3f, 0.15f), new Vector3(0.55f, 0.5f, 0.55f), body);
                    Art.Prim(PrimitiveType.Cube, "HornL", v, new Vector3(-0.35f, 1.6f, 0.1f), new Vector3(0.12f, 0.5f, 0.12f), horn).transform.localRotation = Quaternion.Euler(0, 0, 40);
                    Art.Prim(PrimitiveType.Cube, "HornR", v, new Vector3(0.35f, 1.6f, 0.1f), new Vector3(0.12f, 0.5f, 0.12f), horn).transform.localRotation = Quaternion.Euler(0, 0, -40);
                    Art.Prim(PrimitiveType.Cube, "Eyes", v, new Vector3(0, 1.35f, 0.42f), new Vector3(0.35f, 0.06f, 0.05f), eyes);
                    break;

                case Shape.Hunter:
                    Art.Prim(PrimitiveType.Capsule, "Body", v, new Vector3(0, 0.9f, 0), new Vector3(0.6f, 0.9f, 0.6f), body);
                    Art.Prim(PrimitiveType.Cylinder, "Halo", v, new Vector3(0, 2f, 0), new Vector3(0.5f, 0.02f, 0.5f), gold);
                    Art.Prim(PrimitiveType.Cube, "Spear", v, new Vector3(0.38f, 1f, 0.3f), new Vector3(0.06f, 0.06f, 1.6f), gold);
                    Art.Prim(PrimitiveType.Cube, "WingL", v, new Vector3(-0.45f, 1.2f, -0.3f), new Vector3(0.8f, 0.7f, 0.05f), body).transform.localRotation = Quaternion.Euler(0, 30, 25);
                    Art.Prim(PrimitiveType.Cube, "WingR", v, new Vector3(0.45f, 1.2f, -0.3f), new Vector3(0.8f, 0.7f, 0.05f), body).transform.localRotation = Quaternion.Euler(0, -30, -25);
                    break;

                case Shape.Boss:
                    if (def.Faction == Faction.Demon)
                    {
                        Art.Prim(PrimitiveType.Cube, "Body", v, new Vector3(0, 0.75f, 0), new Vector3(1f, 1.3f, 0.75f), body, shadows);
                        Art.Prim(PrimitiveType.Sphere, "Head", v, new Vector3(0, 1.6f, 0.1f), new Vector3(0.55f, 0.5f, 0.55f), body, shadows);
                        Art.Prim(PrimitiveType.Cube, "HornL", v, new Vector3(-0.4f, 2f, 0f), new Vector3(0.12f, 0.7f, 0.12f), horn).transform.localRotation = Quaternion.Euler(-20, 0, 35);
                        Art.Prim(PrimitiveType.Cube, "HornR", v, new Vector3(0.4f, 2f, 0f), new Vector3(0.12f, 0.7f, 0.12f), horn).transform.localRotation = Quaternion.Euler(-20, 0, -35);
                        Art.Prim(PrimitiveType.Cube, "Eyes", v, new Vector3(0, 1.65f, 0.37f), new Vector3(0.35f, 0.06f, 0.05f), eyes);
                        Art.Prim(PrimitiveType.Cube, "Axe", v, new Vector3(0.65f, 0.9f, 0.3f), new Vector3(0.1f, 0.1f, 1.4f), dark);
                    }
                    else
                    {
                        Art.Prim(PrimitiveType.Capsule, "Body", v, new Vector3(0, 1f, 0), new Vector3(0.7f, 1f, 0.7f), body, shadows);
                        Art.Prim(PrimitiveType.Cylinder, "Halo", v, new Vector3(0, 2.3f, 0), new Vector3(0.7f, 0.03f, 0.7f), gold);
                        Art.Prim(PrimitiveType.Cube, "WingL", v, new Vector3(-0.8f, 1.5f, -0.3f), new Vector3(1.4f, 1.2f, 0.05f), body).transform.localRotation = Quaternion.Euler(0, 25, 25);
                        Art.Prim(PrimitiveType.Cube, "WingR", v, new Vector3(0.8f, 1.5f, -0.3f), new Vector3(1.4f, 1.2f, 0.05f), body).transform.localRotation = Quaternion.Euler(0, -25, -25);
                        Art.Prim(PrimitiveType.Cube, "Sword", v, new Vector3(0.55f, 1.2f, 0.4f), new Vector3(0.12f, 0.12f, 1.6f), gold);
                    }
                    break;
            }
        }

        // ---------- Жизненный цикл ----------
        public void Spawn(Vector3 pos, float hpMult)
        {
            Teleport(pos);
            MaxHP = Def.HP * hpMult;
            HP = MaxHP;
            _knock = Vector3.zero;
            _flash = 0f;
            _attackCd = 0f;
            _speedMult = Random.Range(0.9f, 1.1f);
            SetFlash(false);
            _rb.linearVelocity = Vector3.zero;

            _index = Active.Count;
            Active.Add(this);
            if (Def.IsBoss) CurrentBoss = this;
        }

        void FixedUpdate()
        {
            var player = GameManager.I.Player;
            var to = player.transform.position - transform.position;
            to.y = 0f;
            var dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.zero;
            _rb.linearVelocity = dir * (Def.Speed * _speedMult) + _knock;
            _knock = Vector3.MoveTowards(_knock, Vector3.zero, 30f * Time.fixedDeltaTime);
        }

        void Update()
        {
            var player = GameManager.I.Player;
            var to = player.transform.position - transform.position;
            to.y = 0f;

            if (to.sqrMagnitude > 0.01f)
                _visual.rotation = Quaternion.LookRotation(to);

            // Удар при касании
            _attackCd -= Time.deltaTime;
            float reach = Radius + 0.45f;
            if (_attackCd <= 0f && to.sqrMagnitude < reach * reach)
            {
                player.TakeDamage(Def.Damage);
                _attackCd = 0.5f;
            }

            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                if (_flash <= 0f) SetFlash(false);
            }
        }

        public void TakeDamage(float dmg, Vector3 from, float knockback = 4f)
        {
            if (!IsAlive) return;
            HP -= dmg;
            GameManager.I.UI.ShowDamage(transform.position + Vector3.up * (Radius * 2.2f), dmg);

            if (!Def.IsBoss)
            {
                var away = transform.position - from;
                away.y = 0;
                if (away.sqrMagnitude > 0.001f) _knock = away.normalized * knockback;
            }
            _flash = 0.08f;
            SetFlash(true);

            if (HP <= 0f) Die();
        }

        void Die()
        {
            RemoveFromActive();
            GameManager.I.Kills++;

            var gem = GameManager.I.Gems.Get();
            gem.Drop(transform.position, Def.XP);

            if (Def.IsBoss)
            {
                // Босс роняет кучу опыта и полностью лечит героя.
                for (int i = 0; i < 12; i++)
                    GameManager.I.Gems.Get().Drop(GameManager.RandomAround(transform.position, 0.5f, 2.5f), Def.XP / 6);
                GameManager.I.Player.Heal(GameManager.I.Player.MaxHP);
                GameManager.I.Cam.GetComponent<CameraFollow>().Shake(0.6f);
                GameManager.I.UI.Announce($"{Def.DisplayName} повержен!");
            }

            _pool.Release(this);
        }

        /// <summary>Мгновенно переместить (без "проезда" через интерполяцию физики).</summary>
        public void Teleport(Vector3 pos)
        {
            pos.y = 0f;
            _rb.position = pos;
            transform.position = pos;
        }

        /// <summary>Убрать без награды (например, если враг слишком далеко).</summary>
        public void Despawn()
        {
            RemoveFromActive();
            _pool.Release(this);
        }

        void RemoveFromActive()
        {
            if (_index < 0) return;
            int last = Active.Count - 1;
            var moved = Active[last];
            Active[_index] = moved;
            moved._index = _index;
            Active.RemoveAt(last);
            _index = -1;
            if (CurrentBoss == this) CurrentBoss = null;
        }

        void SetFlash(bool on)
        {
            foreach (var r in _renderers)
            {
                if (on)
                {
                    _mpb.SetColor("_BaseColor", Color.white);
                    r.SetPropertyBlock(_mpb);
                }
                else r.SetPropertyBlock(null);
            }
        }

        // ---------- Поиск целей для оружия ----------
        static readonly List<Enemy> _query = new();

        /// <summary>
        /// Все враги в радиусе. Возвращает копию списка, поэтому по ней безопасно
        /// наносить урон (убитые враги удаляются из Active).
        /// </summary>
        public static List<Enemy> InRadius(Vector3 center, float radius)
        {
            _query.Clear();
            for (int i = 0; i < Active.Count; i++)
            {
                var e = Active[i];
                var off = e.transform.position - center;
                off.y = 0;
                float r = radius + e.Radius;
                if (off.sqrMagnitude < r * r) _query.Add(e);
            }
            return _query;
        }

        public static Enemy Nearest(Vector3 pos, float maxDist = 999f)
        {
            Enemy best = null;
            float bestD = maxDist * maxDist;
            for (int i = 0; i < Active.Count; i++)
            {
                var d = (Active[i].transform.position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = Active[i]; }
            }
            return best;
        }
    }
}
