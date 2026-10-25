using UnityEngine;

namespace FallenGrace
{
    /// <summary>
    /// Изометрическая камера как в Diablo: сверху под углом, повёрнута на 45°.
    /// На вертикальном экране телефона отъезжает дальше, чтобы было видно врагов.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float Pitch = 50f;
        public float Yaw = 45f;
        public float Distance = 24f;
        public float Smooth = 10f;

        float _shake;
        bool _init;

        void LateUpdate()
        {
            if (Target == null) return;

            var cam = GetComponent<Camera>();
            float aspect = cam.aspect;
            float dist = Distance * (aspect < 1f ? 1.45f : 1f);   // портретная ориентация

            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            var wanted = Target.position + Vector3.up * 1f - rot * Vector3.forward * dist;
            transform.position = _init
                ? Vector3.Lerp(transform.position, wanted, 1f - Mathf.Exp(-Smooth * Time.unscaledDeltaTime))
                : wanted;
            _init = true;
            transform.rotation = rot;

            if (_shake > 0f)
            {
                transform.position += Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 2f);
            }
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);
    }
}
