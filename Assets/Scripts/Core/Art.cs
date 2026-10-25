using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FallenGrace
{
    /// <summary>
    /// Временная "графика" из примитивов. Позже заменим на модели из Asset Store.
    /// В редакторе (меню Fallen Grace) материалы сохраняются как файлы в Assets/Game/Materials,
    /// поэтому их можно открыть и перекрасить в Inspector.
    /// </summary>
    public static class Art
    {
        /// <summary>Если задана — материалы сохраняются в эту папку как ассеты (режим сборки в редакторе).</summary>
        public static string BakeFolder;

        static Material _baseMat;
        static readonly Dictionary<string, Material> Cache = new();
        static Sprite _circle;

        static Material BaseMat
        {
            get
            {
                if (_baseMat != null) return _baseMat;
                var rp = GraphicsSettings.currentRenderPipeline;
                _baseMat = rp != null && rp.defaultMaterial != null
                    ? rp.defaultMaterial
                    : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                return _baseMat;
            }
        }

        static Material Create(Color color, float glow, float smooth, string name)
        {
            var m = new Material(BaseMat) { name = name, enableInstancing = true };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smooth);
            if (glow > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.SetColor("_EmissionColor", color * glow);
            }
            return m;
        }

        public static Material Mat(Color color, float glow = 0f, float smooth = 0.2f)
        {
            string key = $"{ColorUtility.ToHtmlStringRGB(color)}_g{glow:0.#}_s{smooth:0.#}";
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(BakeFolder))
            {
                string path = $"{BakeFolder}/M_{key}.mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing != null) return existing;
                var asset = Create(color, glow, smooth, "M_" + key);
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
#endif
            // != null у Unity-объектов ловит и уничтожённые материалы.
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = Create(color, glow, smooth, "FG_" + key);
            Cache[key] = m;
            return m;
        }

        /// <summary>Примитив без коллайдера.</summary>
        public static GameObject Prim(PrimitiveType type, string name, Transform parent,
            Vector3 localPos, Vector3 scale, Material mat, bool shadows = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = shadows;
            return go;
        }

        /// <summary>Круглый спрайт для джойстика (в редакторе подменяется сохранённым ассетом).</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = Sprite.Create(CircleTexture(), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);
                return _circle;
            }
            set => _circle = value;
        }

        public static Texture2D CircleTexture()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Circle" };
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
            }
            tex.Apply();
            return tex;
        }

        /// <summary>Тёмная каменная плитка для пола.</summary>
        public static Texture2D StoneTiles()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "StoneTiles"
            };
            var rnd = new System.Random(7);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool seam = x < 2 || y < 2;
                float n = (float)rnd.NextDouble() * 0.04f;
                float v = seam ? 0.05f : 0.13f + n;
                tex.SetPixel(x, y, new Color(v * 1.1f, v * 0.95f, v * 0.95f, 1));
            }
            tex.Apply();
            return tex;
        }
    }
}
