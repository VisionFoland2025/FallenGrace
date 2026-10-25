using System.Collections.Generic;
using UnityEngine;

namespace FallenGrace
{
    /// <summary>
    /// Волны врагов. Все числа настраиваются в Inspector на объекте Spawner.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Типы врагов (файлы в Assets/Game/Enemies)")]
        public EnemyDef Imp;
        public EnemyDef Ghoul;
        public EnemyDef Brute;
        public EnemyDef Hunter;
        public EnemyDef ArchDemon;
        public EnemyDef Punisher;

        [Header("Когда появляются (секунды)")]
        public float GhoulFrom = 30f;
        public float BruteFrom = 90f;
        public float HunterFrom = 120f;

        [Header("Скорость спавна")]
        [Tooltip("Врагов в секунду в начале")] public float BaseRate = 1.2f;
        [Tooltip("Через сколько секунд добавляется +1 враг/сек")] public float RateGrowthSeconds = 18f;
        [Tooltip("За сколько секунд здоровье врагов удваивается")] public float HpDoubleSeconds = 150f;

        [Header("Лимиты врагов на экране")]
        public int MaxEnemiesPC = 600;
        public int MaxEnemiesMobile = 250;

        [Header("Орда и боссы")]
        public float SwarmInterval = 60f;
        public float FirstBossTime = 180f;
        public float BossInterval = 180f;

        readonly Dictionary<EnemyDef, Pool<Enemy>> _pools = new();
        Transform _root;
        float _spawnAcc;
        float _nextSwarm;
        float _nextBoss;
        int _bossCount;

        int MaxEnemies => GameManager.IsMobile ? MaxEnemiesMobile : MaxEnemiesPC;

        const float SpawnMin = 20f, SpawnMax = 26f, DespawnDist = 42f;

        void Awake()
        {
            _root = new GameObject("Enemies (pool)").transform;
            _nextSwarm = SwarmInterval;
            _nextBoss = FirstBossTime;
            foreach (var def in new[] { Imp, Ghoul, Brute, Hunter, ArchDemon, Punisher })
            {
                if (def == null || def.Prefab == null)
                {
                    Debug.LogWarning($"Fallen Grace: у врага {(def != null ? def.name : "(пусто)")} нет префаба. Запусти меню Fallen Grace → Собрать сцену и префабы.");
                    continue;
                }
                Pool<Enemy> pool = null;
                var d = def;
                pool = new Pool<Enemy>(() =>
                {
                    var e = Instantiate(d.Prefab, _root);
                    e.SetPool(pool);
                    return e;
                });
                pool.Prewarm(def.IsBoss ? 0 : 20);   // после присвоения pool, чтобы враги знали свой пул
                _pools[def] = pool;
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm.IsOver || Time.timeScale == 0f) return;
            float t = gm.GameTime;
            var playerPos = gm.Player.transform.position;

            float rate = BaseRate + t / RateGrowthSeconds;
            _spawnAcc += rate * Time.deltaTime;
            while (_spawnAcc >= 1f)
            {
                _spawnAcc -= 1f;
                if (Enemy.Active.Count < MaxEnemies)
                    SpawnOne(PickDef(t), GameManager.RandomAround(playerPos, SpawnMin, SpawnMax), t);
            }

            // Кольцо бесов вокруг героя
            if (t >= _nextSwarm)
            {
                _nextSwarm += SwarmInterval;
                int n = 24 + Mathf.FloorToInt(t / 60f) * 6;
                for (int i = 0; i < n && Enemy.Active.Count < MaxEnemies + 40; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f;
                    var pos = playerPos + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 16f;
                    SpawnOne(Imp, pos, t);
                }
                gm.UI.Announce("Орда приближается!");
            }

            // Боссы: по очереди демон и ангел
            if (t >= _nextBoss && Enemy.CurrentBoss == null)
            {
                _nextBoss += BossInterval;
                var def = _bossCount % 2 == 0 ? ArchDemon : Punisher;
                _bossCount++;
                if (SpawnOne(def, GameManager.RandomAround(playerPos, 14f, 15f), t))
                {
                    gm.UI.Announce($"Явился {def.DisplayName}!");
                    gm.Cam.GetComponent<CameraFollow>().Shake(0.4f);
                }
            }

            // Отставших врагов переносим вперёд, чтобы давление не пропадало
            for (int i = Enemy.Active.Count - 1; i >= 0; i--)
            {
                var e = Enemy.Active[i];
                if (e.Def.IsBoss) continue;
                var off = e.transform.position - playerPos;
                off.y = 0;
                if (off.sqrMagnitude > DespawnDist * DespawnDist)
                {
                    var forward = gm.Player.MoveDir.sqrMagnitude > 0.01f ? gm.Player.MoveDir.normalized : -off.normalized;
                    e.Teleport(playerPos + forward * SpawnMin
                        + new Vector3(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f)));
                }
            }
        }

        EnemyDef PickDef(float t)
        {
            float imp = 10f;
            float ghoul = t > GhoulFrom ? Mathf.Min(8f, (t - GhoulFrom) / 10f) : 0f;
            float brute = t > BruteFrom ? Mathf.Min(4f, (t - BruteFrom) / 30f) : 0f;
            float hunter = t > HunterFrom ? Mathf.Min(5f, (t - HunterFrom) / 20f) : 0f;
            float r = Random.Range(0f, imp + ghoul + brute + hunter);
            if ((r -= imp) < 0) return Imp;
            if ((r -= ghoul) < 0) return Ghoul;
            if ((r -= brute) < 0) return Brute;
            return Hunter;
        }

        bool SpawnOne(EnemyDef def, Vector3 pos, float t)
        {
            if (def == null || !_pools.TryGetValue(def, out var pool)) return false;
            float hpMult = 1f + t / HpDoubleSeconds;
            pool.Get().Spawn(pos, hpMult);
            return true;
        }
    }
}
