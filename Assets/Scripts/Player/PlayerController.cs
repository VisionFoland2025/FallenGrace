using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FallenGrace
{
    /// <summary>
    /// Падший ангел. Игрок только двигается — оружие стреляет само.
    /// Управление: WASD/стрелки, геймпад или сенсорный джойстик.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // --- Характеристики (стартовые значения можно менять в Inspector) ---
        [Header("Характеристики")]
        public float MaxHP = 100f;
        [HideInInspector] public float HP;
        public float MoveSpeed = 5f;
        public float PickupRadius = 2.5f;
        public float Regen = 0f;          // HP в секунду
        public float DamageMult = 1f;
        public float CooldownMult = 1f;   // меньше = быстрее
        public float AreaMult = 1f;

        // --- Прогресс ---
        [HideInInspector] public int Level = 1;
        [HideInInspector] public int XP;
        public int XPToNext => 5 + Level * 5 + Level * Level;
        [HideInInspector] public int LightPoints;   // сколько улучшений Света взято
        [HideInInspector] public int DarkPoints;    // сколько улучшений Тьмы взято

        public readonly List<Weapon> Weapons = new();
        public Vector3 MoveDir { get; private set; }
        public Vector3 FacingDir { get; private set; } = Vector3.forward;

        public event Action OnStatsChanged;

        [SerializeField, HideInInspector] Transform _body;
        [SerializeField, HideInInspector] Renderer[] _renderers;
        MaterialPropertyBlock _mpb;
        float _flashTimer;

        void Awake()
        {
            HP = MaxHP;
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>Собрать модель героя из примитивов. Вызывается из меню редактора.</summary>
        public void BuildVisual()
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);

            var robe = Art.Mat(new Color(0.85f, 0.82f, 0.75f), 0f, 0.4f);
            var gold = Art.Mat(new Color(1f, 0.78f, 0.3f), 2.5f);
            var wing = Art.Mat(new Color(0.35f, 0.32f, 0.36f), 0f, 0.3f); // опалённые крылья

            Art.Prim(PrimitiveType.Capsule, "Robe", _body, new Vector3(0, 1f, 0), new Vector3(0.8f, 1f, 0.8f), robe, true);
            Art.Prim(PrimitiveType.Sphere, "Head", _body, new Vector3(0, 2.15f, 0), new Vector3(0.5f, 0.5f, 0.5f), robe, true);
            // Треснувший нимб
            Art.Prim(PrimitiveType.Cylinder, "Halo", _body, new Vector3(0, 2.6f, 0), new Vector3(0.6f, 0.02f, 0.6f), gold);
            // Крылья
            var lw = Art.Prim(PrimitiveType.Cube, "WingL", _body, new Vector3(-0.55f, 1.5f, -0.35f), new Vector3(0.9f, 1.1f, 0.06f), wing, true);
            lw.transform.localRotation = Quaternion.Euler(0, 30f, 20f);
            var rw = Art.Prim(PrimitiveType.Cube, "WingR", _body, new Vector3(0.55f, 1.5f, -0.35f), new Vector3(0.9f, 1.1f, 0.06f), wing, true);
            rw.transform.localRotation = Quaternion.Euler(0, -30f, -20f);
            // Меч
            Art.Prim(PrimitiveType.Cube, "Sword", _body, new Vector3(0.5f, 1.1f, 0.35f), new Vector3(0.08f, 0.08f, 1.1f), gold);

            // Свет благодати вокруг героя — мир вокруг тёмный.
            var light = new GameObject("Grace Light").AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.transform.localPosition = new Vector3(0, 3f, 0);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.range = 11f;
            light.intensity = 3f;

            _renderers = _body.GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm.IsOver || Time.timeScale == 0f) return;

            Vector2 input = ReadInput();
            // Движение относительно камеры (камера повёрнута на 45°, как в Diablo).
            var cam = gm.Cam.transform;
            var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var dir = fwd * input.y + right * input.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            MoveDir = dir;

            transform.position += dir * (MoveSpeed * Time.deltaTime);
            if (dir.sqrMagnitude > 0.01f)
            {
                FacingDir = dir.normalized;
                _body.rotation = Quaternion.Slerp(_body.rotation, Quaternion.LookRotation(FacingDir), 15f * Time.deltaTime);
            }

            // Лёгкое "парение"
            _body.localPosition = new Vector3(0, Mathf.Sin(Time.time * 3f) * 0.08f, 0);

            if (Regen > 0f && HP < MaxHP) Heal(Regen * Time.deltaTime);

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f) SetFlash(false);
            }
        }

        Vector2 ReadInput()
        {
            Vector2 v = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                var stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) v += stick;
            }
            var joy = GameManager.I.UI != null ? GameManager.I.UI.Joystick : null;
            if (joy != null && joy.Value.sqrMagnitude > 0.0001f) v += joy.Value;
            return Vector2.ClampMagnitude(v, 1f);
        }

        public void TakeDamage(float amount)
        {
            if (GameManager.I.IsOver) return;
            HP -= amount;
            SetFlash(true);
            _flashTimer = 0.1f;
            OnStatsChanged?.Invoke();
            if (HP <= 0f)
            {
                HP = 0f;
                GameManager.I.OnPlayerDied();
            }
        }

        public void Heal(float amount)
        {
            HP = Mathf.Min(MaxHP, HP + amount);
        }

        public void AddXP(int amount)
        {
            XP += amount;
            while (XP >= XPToNext)
            {
                XP -= XPToNext;
                Level++;
                GameManager.I.Upgrades.QueueLevelUp();
            }
            OnStatsChanged?.Invoke();
        }

        public T AddWeapon<T>() where T : Weapon => (T)AddWeapon(typeof(T));

        public Weapon AddWeapon(Type type)
        {
            var w = (Weapon)gameObject.AddComponent(type);
            w.Init(this);
            Weapons.Add(w);
            return w;
        }

        public Weapon GetWeapon(Type type) => Weapons.Find(w => w.GetType() == type);

        public void NotifyChanged() => OnStatsChanged?.Invoke();

        void SetFlash(bool on)
        {
            foreach (var r in _renderers)
            {
                if (on)
                {
                    _mpb.SetColor("_BaseColor", new Color(1f, 0.2f, 0.2f));
                    r.SetPropertyBlock(_mpb);
                }
                else r.SetPropertyBlock(null);
            }
        }
    }
}
