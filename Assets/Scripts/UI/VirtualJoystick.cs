using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FallenGrace
{
    /// <summary>
    /// "Плавающий" джойстик: появляется там, где палец коснулся экрана.
    /// На ПК работает и мышью (зажать ЛКМ и тянуть) — удобно тестировать в редакторе.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour
    {
        public Vector2 Value { get; private set; }

        const float Radius = 110f;   // в единицах канваса
        [SerializeField] Canvas _canvas;
        [SerializeField] RectTransform _base, _knob;
        Vector2 _start;
        bool _active;

        public static VirtualJoystick Create(Canvas canvas)
        {
            var go = new GameObject("Joystick", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var j = go.AddComponent<VirtualJoystick>();
            j._canvas = canvas;

            j._base = MakeCircle("Base", go.transform, Radius * 2f, new Color(1f, 0.85f, 0.6f, 0.18f));
            j._knob = MakeCircle("Knob", j._base, 90f, new Color(1f, 0.85f, 0.6f, 0.55f));
            j._base.gameObject.SetActive(false);
            return j;
        }

        static RectTransform MakeCircle(string name, Transform parent, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Art.Circle;
            img.color = color;
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(size, size);
            return rt;
        }

        void Update()
        {
            var ptr = Pointer.current;
            if (ptr == null || Time.timeScale == 0f)
            {
                Release();
                return;
            }

            bool pressed = ptr.press.isPressed;
            Vector2 pos = ptr.position.ReadValue();

            if (!_active && ptr.press.wasPressedThisFrame && !GameUI.PointerOverUI())
            {
                _active = true;
                _start = pos;
                _base.gameObject.SetActive(true);
                _base.position = pos;
            }

            if (_active && pressed)
            {
                float radiusPx = Radius * _canvas.scaleFactor;
                Value = Vector2.ClampMagnitude((pos - _start) / radiusPx, 1f);
                _knob.anchoredPosition = Value * Radius;
            }
            else if (_active)
            {
                Release();
            }
        }

        void Release()
        {
            _active = false;
            Value = Vector2.zero;
            if (_base != null)
            {
                _base.gameObject.SetActive(false);
                _knob.anchoredPosition = Vector2.zero;
            }
        }
    }
}
