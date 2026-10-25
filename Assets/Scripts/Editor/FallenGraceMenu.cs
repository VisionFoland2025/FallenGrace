using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FallenGrace.EditorTools
{
    /// <summary>
    /// Меню Fallen Grace в верхней панели Unity.
    /// "Собрать сцену и префабы" создаёт всё, что видно в редакторе:
    ///   Assets/Game/Prefabs    — герой, враги, снаряд, осколок души
    ///   Assets/Game/Enemies    — настройки врагов (здоровье, скорость, урон)
    ///   Assets/Game/Materials  — материалы (цвета)
    ///   Assets/Scenes/Game     — сцена с камерой, светом, полом, UI и GameManager
    /// Уже существующие префабы и настройки врагов НЕ перезаписываются — твои правки сохраняются.
    /// </summary>
    public static class FallenGraceMenu
    {
        const string Root = "Assets/Game";
        const string MatDir = Root + "/Materials";
        const string TexDir = Root + "/Textures";
        const string PrefabDir = Root + "/Prefabs";
        const string EnemyPrefabDir = PrefabDir + "/Enemies";
        const string EnemyDir = Root + "/Enemies";
        const string SettingsDir = Root + "/Settings";
        const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Fallen Grace/Собрать сцену и префабы", priority = 0)]
        public static void BuildAll() => Build(false);

        [MenuItem("Fallen Grace/Открыть и играть ▶", priority = 1)]
        public static void OpenAndPlay()
        {
            if (!File.Exists(ScenePath)) BuildAll();
            else
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Fallen Grace/Пересобрать префабы с нуля", priority = 20)]
        public static void RebuildPrefabs()
        {
            if (EditorUtility.DisplayDialog("Fallen Grace",
                    "Префабы героя, врагов, снаряда и осколка будут созданы заново.\n" +
                    "Ручные изменения в этих префабах пропадут. Настройки врагов (здоровье, урон) останутся.",
                    "Пересобрать", "Отмена"))
                Build(true);
        }

        // =====================================================================
        static void Build(bool forcePrefabs)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var dir in new[] { MatDir, TexDir, EnemyPrefabDir, EnemyDir, SettingsDir, "Assets/Scenes" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();

            // Новая пустая сцена — в ней же временно собираем префабы.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Art.BakeFolder = MatDir;
            try
            {
                SavePng(TexDir + "/Circle.png", Art.CircleTexture(), true);
                Art.Circle = AssetDatabase.LoadAssetAtPath<Sprite>(TexDir + "/Circle.png");
                SavePng(TexDir + "/StoneTiles.png", Art.StoneTiles(), false);
                var stoneTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/StoneTiles.png");
                var groundMat = GroundMaterial(stoneTex);

                // ---------- Враги ----------
                var imp = EnemyAsset("Imp", d => d.Set("Бес", Faction.Demon, Shape.Imp, 6, 3.3f, 5, 1, 0.7f, new Color(0.75f, 0.12f, 0.08f)));
                var ghoul = EnemyAsset("Ghoul", d => d.Set("Упырь", Faction.Demon, Shape.Ghoul, 16, 2.4f, 8, 2, 1f, new Color(0.32f, 0.38f, 0.28f)));
                var brute = EnemyAsset("Brute", d => d.Set("Демон-громила", Faction.Demon, Shape.Brute, 70, 1.8f, 15, 6, 1.6f, new Color(0.35f, 0.05f, 0.05f)));
                var hunter = EnemyAsset("Hunter", d => d.Set("Небесный охотник", Faction.Angel, Shape.Hunter, 32, 3.7f, 12, 4, 1.1f, new Color(0.9f, 0.88f, 0.8f)));
                var arch = EnemyAsset("ArchDemon", d => d.Set("Архидемон Велгор", Faction.Demon, Shape.Boss, 2200, 2.3f, 30, 120, 3.2f, new Color(0.5f, 0.02f, 0.02f), true));
                var punisher = EnemyAsset("Punisher", d => d.Set("Каратель Небес", Faction.Angel, Shape.Boss, 3200, 2.8f, 35, 160, 3f, new Color(1f, 0.92f, 0.7f), true));

                foreach (var def in new[] { imp, ghoul, brute, hunter, arch, punisher })
                {
                    string path = $"{EnemyPrefabDir}/{def.name}.prefab";
                    if (forcePrefabs || def.Prefab == null || !File.Exists(path))
                    {
                        def.Prefab = SavePrefab(Enemy.BuildObject(def).gameObject, path).GetComponent<Enemy>();
                        EditorUtility.SetDirty(def);
                    }
                }

                // ---------- Герой, снаряд, осколок души ----------
                var playerPrefab = PrefabOrBuild<PlayerController>(PrefabDir + "/Player.prefab", forcePrefabs, () =>
                {
                    var go = new GameObject("Player");
                    go.AddComponent<PlayerController>().BuildVisual();
                    return go;
                });
                var projPrefab = PrefabOrBuild<Projectile>(PrefabDir + "/HolyBolt.prefab", forcePrefabs, () => Projectile.BuildObject().gameObject);
                var gemPrefab = PrefabOrBuild<XPGem>(PrefabDir + "/SoulShard.prefab", forcePrefabs, () => XPGem.BuildObject().gameObject);

                var postFx = PostFxProfile();
                AssetDatabase.SaveAssets();

                // =============== СЦЕНА ===============
                // Атмосфера: тусклый свет, туман
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.22f, 0.17f, 0.2f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = new Color(0.05f, 0.03f, 0.05f);
                RenderSettings.fogStartDistance = 22f;
                RenderSettings.fogEndDistance = 48f;
                RenderSettings.skybox = null;

                var world = new GameObject("--- WORLD ---").transform;

                var sun = new GameObject("Hell Sun").AddComponent<Light>();
                sun.transform.SetParent(world);
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.55f, 0.45f);
                sun.intensity = 0.7f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

                var vol = new GameObject("PostFX").AddComponent<Volume>();
                vol.transform.SetParent(world);
                vol.isGlobal = true;
                vol.sharedProfile = postFx;

                var ground = Art.Prim(PrimitiveType.Plane, "Ground", world, Vector3.zero, new Vector3(20f, 1f, 20f), groundMat);
                ground.GetComponent<MeshRenderer>().receiveShadows = true;

                var decor = BuildDecor(world);

                // Герой
                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab.gameObject);
                player.transform.position = Vector3.zero;

                // Камера
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
                cam.fieldOfView = 40f;
                cam.farClipPlane = 80f;
                camGo.AddComponent<AudioListener>();
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                var follow = camGo.AddComponent<CameraFollow>();
                follow.Target = player.transform;
                var rot = Quaternion.Euler(follow.Pitch, follow.Yaw, 0f);
                camGo.transform.SetPositionAndRotation(Vector3.up - rot * Vector3.forward * follow.Distance, rot);

                // UI
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

                var uiGo = new GameObject("UI", typeof(RectTransform));
                var ui = uiGo.AddComponent<GameUI>();
                ui.Build();

                // Системы
                var systems = new GameObject("--- SYSTEMS ---").transform;
                var upgrades = new GameObject("Upgrades").AddComponent<UpgradeSystem>();
                upgrades.transform.SetParent(systems);

                var spawner = new GameObject("Spawner").AddComponent<EnemySpawner>();
                spawner.transform.SetParent(systems);
                spawner.Imp = imp; spawner.Ghoul = ghoul; spawner.Brute = brute;
                spawner.Hunter = hunter; spawner.ArchDemon = arch; spawner.Punisher = punisher;

                var gm = new GameObject("GameManager").AddComponent<GameManager>();
                gm.transform.SetParent(systems);
                gm.Player = player.GetComponent<PlayerController>();
                gm.Cam = cam;
                gm.UI = ui;
                gm.Upgrades = upgrades;
                gm.Spawner = spawner;
                gm.Sun = sun;
                gm.ProjectilePrefab = projPrefab;
                gm.GemPrefab = gemPrefab;
                gm.Ground = ground.transform;
                gm.Decor = decor;

                EditorSceneManager.SaveScene(scene, ScenePath);

                var scenes = new List<EditorBuildSettingsScene> { new(ScenePath, true) };
                foreach (var s in EditorBuildSettings.scenes)
                    if (s.path != ScenePath) scenes.Add(s);
                EditorBuildSettings.scenes = scenes.ToArray();

                Selection.activeGameObject = player;
                SceneView.lastActiveSceneView?.FrameSelected();
                Debug.Log("Fallen Grace: сцена и префабы готовы → " + ScenePath + ". Нажми Play!");
            }
            finally
            {
                Art.BakeFolder = null;
            }
        }

        // =====================================================================
        static EnemyDef EnemyAsset(string file, System.Action<EnemyDef> setup)
        {
            string path = $"{EnemyDir}/{file}.asset";
            var d = AssetDatabase.LoadAssetAtPath<EnemyDef>(path);
            if (d != null) return d;   // уже есть — не трогаем твои настройки
            d = ScriptableObject.CreateInstance<EnemyDef>();
            setup(d);
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static T PrefabOrBuild<T>(string path, bool force, System.Func<GameObject> build) where T : Component
        {
            if (!force)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing.GetComponent<T>();
            }
            return SavePrefab(build(), path).GetComponent<T>();
        }

        /// <summary>Сохранить текстуру как PNG-файл (если его ещё нет).</summary>
        static void SavePng(string path, Texture2D tex, bool sprite)
        {
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
                imp.alphaIsTransparency = sprite;
                imp.mipmapEnabled = !sprite;
                imp.wrapMode = sprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                imp.SaveAndReimport();
            }
            Object.DestroyImmediate(tex);
        }

        static Material GroundMaterial(Texture2D tex)
        {
            string path = MatDir + "/Ground.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial) { name = "Ground" };
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", new Vector2(50f, 50f));
            m.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static VolumeProfile PostFxProfile()
        {
            string path = SettingsDir + "/FallenGracePostFX.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(true);           // свечение
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.3f);
            AssetDatabase.AddObjectToAsset(bloom, profile);

            var vignette = profile.Add<Vignette>(true);     // тёмно-красные края экрана
            vignette.intensity.Override(0.38f);
            vignette.color.Override(new Color(0.15f, 0f, 0f));
            AssetDatabase.AddObjectToAsset(vignette, profile);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static Transform[] BuildDecor(Transform world)
        {
            var stone = Art.Mat(new Color(0.2f, 0.18f, 0.2f));
            var bone = Art.Mat(new Color(0.45f, 0.42f, 0.36f));
            var ember = Art.Mat(new Color(1f, 0.35f, 0.1f), 3f);
            var root = new GameObject("Decor").transform;
            root.SetParent(world);

            var rnd = new System.Random(42);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            var list = new Transform[50];
            for (int i = 0; i < list.Length; i++)
            {
                var d = new GameObject("Decor" + i).transform;
                d.SetParent(root);
                switch (i % 3)
                {
                    case 0: // обломок колонны
                        Art.Prim(PrimitiveType.Cylinder, "Pillar", d, new Vector3(0, 0.9f, 0),
                            new Vector3(0.9f, R(0.6f, 1.6f), 0.9f), stone, true);
                        break;
                    case 1: // надгробие
                        Art.Prim(PrimitiveType.Cube, "Grave", d, new Vector3(0, 0.6f, 0),
                            new Vector3(0.8f, 1.2f, 0.25f), bone, true);
                        break;
                    default: // тлеющие угли
                        Art.Prim(PrimitiveType.Sphere, "Ember", d, new Vector3(0, 0.05f, 0),
                            new Vector3(0.5f, 0.1f, 0.5f), ember);
                        break;
                }
                float a = R(0f, Mathf.PI * 2f), r = R(6f, 40f);
                d.SetPositionAndRotation(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), Quaternion.Euler(0, R(0, 360f), 0));
                list[i] = d;
            }
            return list;
        }
    }
}
