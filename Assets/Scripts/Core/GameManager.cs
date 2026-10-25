using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FallenGrace
{
    /// <summary>
    /// Главный объект игры: время, убийства, пауза, смерть, пулы снарядов и опыта.
    /// Все ссылки заполняет меню Fallen Grace → Собрать сцену и префабы.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [Header("Объекты сцены")]
        public PlayerController Player;
        public Camera Cam;
        public GameUI UI;
        public UpgradeSystem Upgrades;
        public EnemySpawner Spawner;
        public Light Sun;

        [Header("Префабы")]
        public Projectile ProjectilePrefab;
        public XPGem GemPrefab;

        [Header("Бесконечный пол и декор")]
        public Transform Ground;
        public float GroundTile = 4f;
        public Transform[] Decor;

        public float GameTime { get; private set; }
        public int Kills { get; set; }
        public bool IsOver { get; private set; }
        public bool IsChoosing { get; set; }   // открыто окно выбора улучшения
        public bool IsPaused { get; private set; }

        public static bool IsMobile => Application.isMobilePlatform;

        public Pool<Projectile> Projectiles { get; private set; }
        public Pool<XPGem> Gems { get; private set; }

        void Awake()
        {
            I = this;
            Time.timeScale = 1f;
            Enemy.Active.Clear();
            Enemy.CurrentBoss = null;
            Application.targetFrameRate = IsMobile ? 60 : -1;
            if (IsMobile && Sun != null) Sun.shadows = LightShadows.None;   // тени дорогие для телефона

            var root = new GameObject("Pools").transform;
            Projectiles = new Pool<Projectile>(() => Instantiate(ProjectilePrefab, root));
            Projectiles.Prewarm(40);
            Gems = new Pool<XPGem>(() => Instantiate(GemPrefab, root));
            Gems.Prewarm(100);
        }

        void Start()
        {
            Player.AddWeapon<HolyBolt>();   // стартовое оружие — Святая стрела
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (!IsOver && !IsChoosing && kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
                TogglePause();

            if (IsOver || IsChoosing || IsPaused) return;
            GameTime += Time.deltaTime;

            // Пол и декор следуют за игроком — мир бесконечный.
            var p = Player.transform.position;
            if (Ground != null)
                Ground.position = new Vector3(Mathf.Round(p.x / GroundTile) * GroundTile, 0f,
                    Mathf.Round(p.z / GroundTile) * GroundTile);
            if (Decor != null)
                foreach (var d in Decor)
                {
                    var off = d.position - p;
                    off.y = 0;
                    if (off.sqrMagnitude > 42f * 42f)
                        d.position = p - off.normalized * 38f + new Vector3(Random.Range(-6f, 6f), 0, Random.Range(-6f, 6f));
                }
        }

        public void TogglePause()
        {
            if (IsOver || IsChoosing) return;
            IsPaused = !IsPaused;
            UpdateTimeScale();
            UI.ShowPause(IsPaused);
        }

        public void UpdateTimeScale()
        {
            Time.timeScale = (IsOver || IsChoosing || IsPaused) ? 0f : 1f;
        }

        public void OnPlayerDied()
        {
            if (IsOver) return;
            IsOver = true;
            UpdateTimeScale();
            UI.ShowGameOver(GameTime, Kills, Player.Level);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public static Vector3 RandomAround(Vector3 center, float minR, float maxR)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(minR, maxR);
            return center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        public static string FormatTime(float t)
        {
            int s = Mathf.FloorToInt(t);
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
