using UnityEngine;

namespace FallenGrace
{
    /// <summary>Осколок души — опыт, который роняют враги. Притягивается к герою.</summary>
    public class XPGem : MonoBehaviour
    {
        int _value;
        bool _magnet;
        float _speed;
        [SerializeField] Transform _visual;
        [SerializeField] Renderer _renderer;
        [SerializeField] Material _small, _big;

        /// <summary>Собрать префаб осколка. Вызывается из меню редактора.</summary>
        public static XPGem BuildObject()
        {
            var go = new GameObject("SoulShard");
            var g = go.AddComponent<XPGem>();
            g._small = Art.Mat(new Color(0.3f, 0.8f, 1f), 3f);
            g._big = Art.Mat(new Color(0.8f, 0.35f, 1f), 3.5f);
            g._visual = Art.Prim(PrimitiveType.Cube, "Shard", go.transform, new Vector3(0, 0.4f, 0),
                Vector3.one * 0.25f, g._small).transform;
            g._renderer = g._visual.GetComponent<Renderer>();
            return g;
        }

        public void Drop(Vector3 pos, int value)
        {
            pos.y = 0;
            transform.position = pos;
            _value = Mathf.Max(1, value);
            _magnet = false;
            _speed = 0f;
            bool big = _value >= 5;
            _renderer.sharedMaterial = big ? _big : _small;
            _visual.localScale = Vector3.one * (big ? 0.4f : 0.25f);
        }

        void Update()
        {
            var player = GameManager.I.Player;
            _visual.Rotate(40f * Time.deltaTime, 90f * Time.deltaTime, 0f);

            var to = player.transform.position - transform.position;
            to.y = 0;
            float d2 = to.sqrMagnitude;

            if (!_magnet && d2 < player.PickupRadius * player.PickupRadius) _magnet = true;
            if (!_magnet) return;

            _speed += 30f * Time.deltaTime;
            transform.position += to.normalized * Mathf.Min(_speed * Time.deltaTime, Mathf.Sqrt(d2));
            if (d2 < 0.36f)
            {
                player.AddXP(_value);
                GameManager.I.Gems.Release(this);
            }
        }
    }
}
