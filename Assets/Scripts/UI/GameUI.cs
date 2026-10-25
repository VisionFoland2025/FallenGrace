using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FallenGrace
{
    /// <summary>
    /// Интерфейс: полоска опыта, таймер, здоровье над героем, полоска босса,
    /// цифры урона, окно выбора улучшения, пауза и экран смерти.
    /// Собирается в редакторе (меню Fallen Grace), поэтому его видно в сцене и можно править.
    /// Подстраивается под горизонтальный (ПК) и вертикальный (телефон) экран.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [SerializeField] VirtualJoystick _joystick;
        public VirtualJoystick Joystick => _joystick;

        static readonly Color Gold = new(1f, 0.8f, 0.4f);
        static readonly Color Crimson = new(0.85f, 0.15f, 0.2f);
        static readonly Color PanelColor = new(0.06f, 0.04f, 0.06f, 0.92f);

        [SerializeField] Font _font;
        [SerializeField] Canvas _canvas;
        [SerializeField] CanvasScaler _scaler;
        [SerializeField] RectTransform _root;

        [SerializeField] RectTransform _xpFill, _hpFill, _hpBar, _bossFill;
        [SerializeField] GameObject _bossBar;
        [SerializeField] Text _levelText, _timerText, _killsText, _pathText, _bossName, _announce;
        float _announceTimer;

        [SerializeField] GameObject _levelUpPanel, _pausePanel, _gameOverPanel;
        [SerializeField] RectTransform _cardsRoot;
        [SerializeField] Text _levelUpTitle, _gameOverStats;
        [SerializeField] Button _pauseButton, _resumeButton, _restartButton;
        readonly List<UpgradeOption> _currentOptions = new();
        Action<UpgradeOption> _onChoose;

        // Цифры урона
        [SerializeField] Text[] _dmgTexts;
        float[] _dmgLife;
        Vector3[] _dmgWorld;
        int _dmgNext;

        void Awake()
        {
            _dmgLife = new float[_dmgTexts.Length];
            _dmgWorld = new Vector3[_dmgTexts.Length];
            _announce.text = "";
            // Кнопки подключаем в коде при запуске
            _pauseButton.onClick.AddListener(() => GameManager.I.TogglePause());
            _resumeButton.onClick.AddListener(() => GameManager.I.TogglePause());
            _restartButton.onClick.AddListener(() => GameManager.I.Restart());
        }

        /// <summary>Собрать интерфейс. Вызывается из меню редактора, не во время игры.</summary>
        public void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)transform;

            // Джойстик — под всем остальным UI
            _joystick = VirtualJoystick.Create(_canvas);
            Stretch((RectTransform)_joystick.transform);

            // Цифры урона
            var dmgRoot = NewRect("DamageNumbers", _root);
            Stretch(dmgRoot);
            const int n = 40;
            _dmgTexts = new Text[n];
            for (int i = 0; i < n; i++)
            {
                _dmgTexts[i] = MakeText(dmgRoot, "", 26, Color.white, TextAnchor.MiddleCenter);
                _dmgTexts[i].rectTransform.sizeDelta = new Vector2(120, 40);
                _dmgTexts[i].gameObject.SetActive(false);
            }

            BuildHud();
            BuildLevelUp();
            BuildPause();
            BuildGameOver();
        }

        // ================= HUD =================
        void BuildHud()
        {
            // Полоска опыта во всю ширину сверху
            var xpBg = MakeImage(_root, new Color(0, 0, 0, 0.7f));
            Anchor(xpBg.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            xpBg.rectTransform.sizeDelta = new Vector2(0, 22);
            _xpFill = MakeImage(xpBg.rectTransform, new Color(0.3f, 0.75f, 1f)).rectTransform;
            Anchor(_xpFill, Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f));
            _xpFill.offsetMin = _xpFill.offsetMax = Vector2.zero;

            _levelText = MakeText(_root, "Ур. 1", 34, Gold, TextAnchor.UpperLeft);
            Anchor(_levelText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            _levelText.rectTransform.anchoredPosition = new Vector2(24, -30);
            _levelText.rectTransform.sizeDelta = new Vector2(400, 50);

            _pathText = MakeText(_root, "", 26, Color.white, TextAnchor.UpperLeft);
            _pathText.supportRichText = true;
            Anchor(_pathText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            _pathText.rectTransform.anchoredPosition = new Vector2(24, -72);
            _pathText.rectTransform.sizeDelta = new Vector2(400, 40);

            _timerText = MakeText(_root, "00:00", 48, Color.white, TextAnchor.UpperCenter);
            Anchor(_timerText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            _timerText.rectTransform.anchoredPosition = new Vector2(0, -30);
            _timerText.rectTransform.sizeDelta = new Vector2(300, 60);

            _killsText = MakeText(_root, "Убито: 0", 30, new Color(0.9f, 0.85f, 0.85f), TextAnchor.UpperRight);
            Anchor(_killsText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            _killsText.rectTransform.anchoredPosition = new Vector2(-110, -32);
            _killsText.rectTransform.sizeDelta = new Vector2(320, 50);

            // Кнопка паузы (нужна на телефоне)
            var pauseBtn = MakeButton(_root, "II", 34, new Vector2(80, 80), null);
            _pauseButton = pauseBtn.GetComponent<Button>();
            Anchor(pauseBtn, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            pauseBtn.anchoredPosition = new Vector2(-16, -34);

            // Здоровье под героем
            var hpBg = MakeImage(_root, new Color(0, 0, 0, 0.75f));
            _hpBar = hpBg.rectTransform;
            _hpBar.sizeDelta = new Vector2(110, 14);
            _hpFill = MakeImage(_hpBar, Crimson).rectTransform;
            Anchor(_hpFill, Vector2.zero, new Vector2(1, 1), new Vector2(0, 0.5f));
            _hpFill.offsetMin = new Vector2(2, 2);
            _hpFill.offsetMax = new Vector2(-2, -2);

            // Полоска босса
            var boss = MakeImage(_root, new Color(0, 0, 0, 0.8f));
            _bossBar = boss.gameObject;
            Anchor(boss.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            boss.rectTransform.anchoredPosition = new Vector2(0, -130);
            boss.rectTransform.sizeDelta = new Vector2(700, 26);
            _bossFill = MakeImage(boss.rectTransform, new Color(0.7f, 0.05f, 0.05f)).rectTransform;
            Anchor(_bossFill, Vector2.zero, new Vector2(1, 1), new Vector2(0, 0.5f));
            _bossFill.offsetMin = new Vector2(3, 3);
            _bossFill.offsetMax = new Vector2(-3, -3);
            _bossName = MakeText(boss.rectTransform, "", 28, Gold, TextAnchor.LowerCenter);
            Anchor(_bossName.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0));
            _bossName.rectTransform.sizeDelta = new Vector2(700, 40);
            _bossBar.SetActive(false);

            // Объявления ("Орда приближается!")
            _announce = MakeText(_root, "", 54, Gold, TextAnchor.MiddleCenter);
            Anchor(_announce.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f));
            _announce.rectTransform.sizeDelta = new Vector2(1000, 80);
            _announce.gameObject.AddComponent<Outline>().effectColor = Color.black;
        }

        void Update()
        {
            // Портрет (телефон) / альбом (ПК)
            bool portrait = Screen.height > Screen.width;
            _scaler.referenceResolution = portrait ? new Vector2(1080, 1920) : new Vector2(1920, 1080);

            var gm = GameManager.I;
            var p = gm.Player;
            if (p == null) return;

            _xpFill.anchorMax = new Vector2(Mathf.Clamp01(p.XP / (float)p.XPToNext), 1);
            _levelText.text = $"Ур. {p.Level}";
            _pathText.text = $"<color=#FFCC66>Свет {p.LightPoints}</color>  ·  <color=#D9263A>Тьма {p.DarkPoints}</color>";
            _timerText.text = GameManager.FormatTime(gm.GameTime);
            _killsText.text = $"Убито: {gm.Kills}";

            // Здоровье над героем
            var screen = gm.Cam.WorldToScreenPoint(p.transform.position + Vector3.down * 0.3f);
            _hpBar.position = screen;
            _hpFill.anchorMax = new Vector2(Mathf.Clamp01(p.HP / p.MaxHP), 1);

            // Босс
            var boss = Enemy.CurrentBoss;
            _bossBar.SetActive(boss != null);
            if (boss != null)
            {
                _bossName.text = boss.Def.DisplayName;
                _bossFill.anchorMax = new Vector2(Mathf.Clamp01(boss.HP / boss.MaxHP), 1);
            }

            // Объявления
            if (_announceTimer > 0f)
            {
                _announceTimer -= Time.unscaledDeltaTime;
                var c = _announce.color;
                c.a = Mathf.Clamp01(_announceTimer);
                _announce.color = c;
            }

            // Цифры урона
            for (int i = 0; i < _dmgTexts.Length; i++)
            {
                if (_dmgLife[i] <= 0f) continue;
                _dmgLife[i] -= Time.deltaTime;
                var t = _dmgTexts[i];
                if (_dmgLife[i] <= 0f) { t.gameObject.SetActive(false); continue; }
                _dmgWorld[i] += Vector3.up * (2f * Time.deltaTime);
                t.rectTransform.position = gm.Cam.WorldToScreenPoint(_dmgWorld[i]);
                var col = t.color;
                col.a = Mathf.Clamp01(_dmgLife[i] * 2.5f);
                t.color = col;
            }

            // Горячие клавиши: 1/2/3 — выбор улучшения, R/Enter — заново
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (_levelUpPanel.activeSelf)
                {
                    if (kb.digit1Key.wasPressedThisFrame) Pick(0);
                    else if (kb.digit2Key.wasPressedThisFrame) Pick(1);
                    else if (kb.digit3Key.wasPressedThisFrame) Pick(2);
                }
                if (_gameOverPanel.activeSelf && (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    gm.Restart();
            }
        }

        public void ShowDamage(Vector3 world, float dmg)
        {
            int i = _dmgNext;
            _dmgNext = (_dmgNext + 1) % _dmgTexts.Length;
            var t = _dmgTexts[i];
            t.gameObject.SetActive(true);
            t.text = Mathf.CeilToInt(dmg).ToString();
            t.color = dmg >= 40f ? Gold : Color.white;
            t.fontSize = dmg >= 40f ? 34 : 26;
            _dmgWorld[i] = world + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0, 0);
            _dmgLife[i] = 0.6f;
            t.rectTransform.position = GameManager.I.Cam.WorldToScreenPoint(_dmgWorld[i]);
        }

        public void Announce(string text)
        {
            _announce.text = text;
            _announceTimer = 2.5f;
        }

        // ================= ВЫБОР УЛУЧШЕНИЯ =================
        void BuildLevelUp()
        {
            var overlay = MakeImage(_root, new Color(0, 0, 0, 0.7f));
            Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;   // блокирует клики по игре
            _levelUpPanel = overlay.gameObject;

            _levelUpTitle = MakeText(overlay.rectTransform, "НОВЫЙ УРОВЕНЬ", 60, Gold, TextAnchor.MiddleCenter);
            Anchor(_levelUpTitle.rectTransform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.5f));
            _levelUpTitle.rectTransform.sizeDelta = new Vector2(1000, 90);

            var hint = MakeText(overlay.rectTransform, "Выбери путь: Свет или Тьма", 30, new Color(0.8f, 0.75f, 0.75f), TextAnchor.MiddleCenter);
            Anchor(hint.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f));
            hint.rectTransform.sizeDelta = new Vector2(1000, 50);

            _cardsRoot = NewRect("Cards", overlay.rectTransform);
            Anchor(_cardsRoot, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f));
            _levelUpPanel.SetActive(false);
        }

        public void ShowLevelUp(int level, List<UpgradeOption> options, Action<UpgradeOption> onChoose)
        {
            _onChoose = onChoose;
            _currentOptions.Clear();
            _currentOptions.AddRange(options);
            _levelUpTitle.text = $"УРОВЕНЬ {level}";

            for (int i = _cardsRoot.childCount - 1; i >= 0; i--) Destroy(_cardsRoot.GetChild(i).gameObject);

            bool portrait = Screen.height > Screen.width;
            var cardSize = portrait ? new Vector2(900, 260) : new Vector2(420, 360);
            float gap = 30f;
            int n = options.Count;

            for (int i = 0; i < n; i++)
            {
                var o = options[i];
                int index = i;
                var pathColor = o.Path == GracePath.Light ? Gold : Crimson;

                var card = MakeButton(_cardsRoot, "", 1, cardSize, () => Pick(index));
                card.GetComponent<Image>().color = PanelColor;
                var border = card.gameObject.AddComponent<Outline>();
                border.effectColor = pathColor;
                border.effectDistance = new Vector2(3, -3);

                float offset = (i - (n - 1) / 2f) * ((portrait ? cardSize.y : cardSize.x) + gap);
                card.anchoredPosition = portrait ? new Vector2(0, -offset) : new Vector2(offset, 0);

                var path = MakeText(card, $"{(o.Path == GracePath.Light ? "СВЕТ" : "ТЬМА")}   {o.Tag}", 24, pathColor, TextAnchor.UpperLeft);
                Anchor(path.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
                path.rectTransform.offsetMin = new Vector2(24, -60);
                path.rectTransform.offsetMax = new Vector2(-24, -18);

                var title = MakeText(card, o.Title, 38, Color.white, TextAnchor.UpperLeft);
                Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
                title.rectTransform.offsetMin = new Vector2(24, -120);
                title.rectTransform.offsetMax = new Vector2(-24, -58);

                var desc = MakeText(card, o.Desc, 28, new Color(0.8f, 0.78f, 0.78f), TextAnchor.UpperLeft);
                Anchor(desc.rectTransform, Vector2.zero, new Vector2(1, 1), new Vector2(0.5f, 0.5f));
                desc.rectTransform.offsetMin = new Vector2(24, 20);
                desc.rectTransform.offsetMax = new Vector2(-24, -128);

                var key = MakeText(card, portrait ? "" : $"[{i + 1}]", 24, new Color(1, 1, 1, 0.35f), TextAnchor.LowerRight);
                Stretch(key.rectTransform);
                key.rectTransform.offsetMin = new Vector2(16, 12);
                key.rectTransform.offsetMax = new Vector2(-16, -12);
            }

            _levelUpPanel.SetActive(true);
            _levelUpPanel.transform.SetAsLastSibling();
        }

        void Pick(int index)
        {
            if (!_levelUpPanel.activeSelf || index >= _currentOptions.Count) return;
            var o = _currentOptions[index];
            _onChoose?.Invoke(o);
        }

        public void HideLevelUp() => _levelUpPanel.SetActive(false);

        // ================= ПАУЗА =================
        void BuildPause()
        {
            var overlay = MakeImage(_root, new Color(0, 0, 0, 0.75f));
            Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;   // блокирует клики по игре
            _pausePanel = overlay.gameObject;

            var title = MakeText(overlay.rectTransform, "ПАУЗА", 72, Gold, TextAnchor.MiddleCenter);
            title.rectTransform.sizeDelta = new Vector2(800, 100);
            title.rectTransform.anchoredPosition = new Vector2(0, 120);

            var btn = MakeButton(overlay.rectTransform, "Продолжить", 40, new Vector2(420, 100), null);
            _resumeButton = btn.GetComponent<Button>();
            btn.anchoredPosition = new Vector2(0, -40);
            _pausePanel.SetActive(false);
        }

        public void ShowPause(bool on)
        {
            _pausePanel.SetActive(on);
            _pausePanel.transform.SetAsLastSibling();
        }

        // ================= СМЕРТЬ =================
        void BuildGameOver()
        {
            var overlay = MakeImage(_root, new Color(0.1f, 0f, 0f, 0.85f));
            Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;   // блокирует клики по игре
            _gameOverPanel = overlay.gameObject;

            var title = MakeText(overlay.rectTransform, "ТЫ ПАЛ", 96, Crimson, TextAnchor.MiddleCenter);
            title.rectTransform.sizeDelta = new Vector2(1000, 130);
            title.rectTransform.anchoredPosition = new Vector2(0, 200);

            _gameOverStats = MakeText(overlay.rectTransform, "", 38, Color.white, TextAnchor.MiddleCenter);
            _gameOverStats.rectTransform.sizeDelta = new Vector2(1000, 200);
            _gameOverStats.rectTransform.anchoredPosition = new Vector2(0, 30);

            var btn = MakeButton(overlay.rectTransform, "Снова в бой", 40, new Vector2(460, 100), null);
            _restartButton = btn.GetComponent<Button>();
            btn.anchoredPosition = new Vector2(0, -150);
            _gameOverPanel.SetActive(false);
        }

        public void ShowGameOver(float time, int kills, int level)
        {
            _levelUpPanel.SetActive(false);
            _pausePanel.SetActive(false);
            _gameOverStats.text = $"Продержался: {GameManager.FormatTime(time)}\nУбито врагов: {kills}\nУровень: {level}";
            _gameOverPanel.SetActive(true);
            _gameOverPanel.transform.SetAsLastSibling();
        }

        // ================= ПОМОЩНИКИ =================
        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        Image MakeImage(Transform parent, Color color)
        {
            var rt = NewRect("Image", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text MakeText(Transform parent, string text, int size, Color color, TextAnchor align)
        {
            var rt = NewRect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        RectTransform MakeButton(Transform parent, string label, int size, Vector2 rectSize, Action onClick)
        {
            var img = MakeImage(parent, new Color(0.25f, 0.1f, 0.12f, 0.95f));
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.sizeDelta = rectSize;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.3f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            if (!string.IsNullOrEmpty(label))
            {
                var t = MakeText(rt, label, size, Color.white, TextAnchor.MiddleCenter);
                Stretch(t.rectTransform);
            }
            return rt;
        }

        /// <summary>Касание пришлось на кнопку UI (чтобы джойстик не срабатывал от кнопки паузы).</summary>
        public static bool PointerOverUI()
        {
            var es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }
    }
}
