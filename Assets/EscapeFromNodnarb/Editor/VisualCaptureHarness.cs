using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeFromNodnarb.Editor
{
    public static class VisualCaptureHarness
    {
        private const int Width = 720;
        private const int Height = 1280;
        private const string OutputDirectory = "Artifacts/VisualChecks";

        [MenuItem("Escape from Nodnarb/Capture Crash Basin Visual Checks")]
        public static void CaptureCrashBasinVisualChecks()
        {
            string outputRoot = Path.GetFullPath(OutputDirectory);
            Directory.CreateDirectory(outputRoot);
            Capture(outputRoot, "crash-basin-opening", SubjectMode.Opening);
            Capture(outputRoot, "crash-basin-combat", SubjectMode.Combat);
            Capture(outputRoot, "crash-basin-heavy", SubjectMode.Heavy);
            Debug.Log("NODNARB_VISUAL_CAPTURE status=PASS output=" + outputRoot + " captures=3 size=" + Width + "x" + Height);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void Capture(string outputRoot, string fileName, SubjectMode mode)
        {
            GameObject captureRoot = new GameObject("VisualCaptureRoot");
            Camera camera = BuildCamera(captureRoot.transform);
            BuildLighting(captureRoot.transform);
            ProceduralWorld world = new ProceduralWorld(captureRoot.transform);
            world.Build(CampaignCatalog.Get(2), camera);
            BuildSubjects(captureRoot.transform, mode);

            RenderTexture target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.name = "CrashBasinVisualCapture";
            target.filterMode = FilterMode.Bilinear;
            camera.targetTexture = target;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            camera.Render();

            Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0, false);
            image.Apply(false, false);
            File.WriteAllBytes(Path.Combine(outputRoot, fileName + ".png"), image.EncodeToPNG());

            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            world.Clear();
            UnityEngine.Object.DestroyImmediate(captureRoot);
        }

        private static Camera BuildCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("VisualCaptureCamera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(0f, 8.15f, -7.45f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.05f, 4.10f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 60f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GameTheme.Void;
            return camera;
        }

        private static void BuildLighting(Transform parent)
        {
            GameObject keyObject = new GameObject("VisualCaptureKey");
            keyObject.transform.SetParent(parent, false);
            keyObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = Color.Lerp(GameTheme.Text, GameTheme.Signal, 0.10f);
            key.intensity = 1.34f;
            key.shadows = LightShadows.Hard;
            key.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
            key.shadowStrength = 0.72f;
            key.shadowBias = 0.06f;
            key.shadowNormalBias = 0.35f;

            GameObject fillObject = new GameObject("VisualCaptureFill");
            fillObject.transform.SetParent(parent, false);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.20f, 0.40f, 0.50f, 1f);
            fill.intensity = 0.22f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(26f, 148f, 0f);

            GameObject rimObject = new GameObject("VisualCaptureRim");
            rimObject.transform.SetParent(parent, false);
            rimObject.transform.rotation = Quaternion.Euler(32f, 152f, 0f);
            Light rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = Color.Lerp(GameTheme.CanyonHighlight, GameTheme.Text, 0.24f);
            rim.intensity = 0.78f;
            rim.shadows = LightShadows.None;
        }

        private static void BuildSubjects(Transform parent, SubjectMode mode)
        {
            SpawnPrefab("Captain/CaptainVisual", parent, new Vector3(0f, 0f, -0.95f), 1.0f, SubjectMaterial("Captain", new Color(0.78f, 0.30f, 0.12f, 1f), Color.black));
            SpawnPrefab("Enemies/CrewSoldier", parent, new Vector3(-0.78f, 0f, -1.45f), 0.92f, SubjectMaterial("CrewA", new Color(0.32f, 0.48f, 0.42f, 1f), Color.black));
            SpawnPrefab("Enemies/CrewSoldier", parent, new Vector3(0.78f, 0f, -1.45f), 0.92f, SubjectMaterial("CrewB", new Color(0.38f, 0.54f, 0.48f, 1f), Color.black));

            if (mode == SubjectMode.Opening)
            {
                return;
            }

            SpawnPrefab("Enemies/Rusher", parent, new Vector3(-1.80f, 0f, 5.2f), 0.78f, SubjectMaterial("Rusher", new Color(0.28f, 0.58f, 0.38f, 1f), Color.black));
            SpawnPrefab("Enemies/Rusher", parent, new Vector3(1.55f, 0f, 7.0f), 0.82f, SubjectMaterial("Rusher2", new Color(0.34f, 0.66f, 0.40f, 1f), Color.black));
            SpawnCard(parent, new Vector3(-2.15f, 0.24f, 2.7f), GameTheme.CanyonHighlight, "WeaponCard");
            SpawnCard(parent, new Vector3(2.15f, 0.24f, 3.6f), GameTheme.SignalCyan, "CrewCard");
            SpawnBolt(parent, new Vector3(-0.16f, 0.84f, 0.8f), new Vector3(-1.35f, 0.55f, 4.6f));
            SpawnBolt(parent, new Vector3(0.16f, 0.84f, 0.95f), new Vector3(1.15f, 0.55f, 6.2f));

            if (mode != SubjectMode.Heavy)
            {
                return;
            }

            SpawnPrefab("Enemies/Spitter", parent, new Vector3(-2.65f, 0f, 9.0f), 0.90f, SubjectMaterial("Spitter", new Color(0.42f, 0.28f, 0.54f, 1f), GameTheme.AlienGlow));
            SpawnPrefab("Enemies/Blocker", parent, new Vector3(2.55f, 0f, 10.8f), 0.92f, SubjectMaterial("Blocker", new Color(0.18f, 0.24f, 0.25f, 1f), GameTheme.SignalCyan));
            SpawnPrefab("Enemies/Rusher", parent, new Vector3(-1.15f, 0f, 11.8f), 0.84f, SubjectMaterial("Rusher3", new Color(0.34f, 0.66f, 0.40f, 1f), Color.black));
            SpawnPrefab("Enemies/Rusher", parent, new Vector3(1.20f, 0f, 13.1f), 0.86f, SubjectMaterial("Rusher4", new Color(0.34f, 0.66f, 0.40f, 1f), Color.black));
            SpawnBolt(parent, new Vector3(-0.18f, 0.84f, 1.15f), new Vector3(-2.15f, 0.55f, 8.6f));
            SpawnBolt(parent, new Vector3(0.18f, 0.84f, 1.30f), new Vector3(2.15f, 0.55f, 10.4f));
        }

        private static GameObject SpawnPrefab(string resourcePath, Transform parent, Vector3 position, float scale, Material material)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning("NODNARB_VISUAL_CAPTURE missing_prefab=" + resourcePath);
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity, parent);
            instance.name = "Capture_" + resourcePath.Replace('/', '_');
            instance.transform.localScale *= scale;
            ApplyMaterial(instance, material);
            return instance;
        }

        private static void ApplyMaterial(GameObject instance, Material material)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Material[] materials = renderers[index].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = material;
                }
                renderers[index].sharedMaterials = materials;
            }
        }

        private static void SpawnCard(Transform parent, Vector3 position, Color color, string name)
        {
            GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = "Capture_" + name;
            card.transform.SetParent(parent, false);
            card.transform.position = position;
            card.transform.localScale = new Vector3(0.76f, 0.10f, 0.42f);
            ApplyMaterial(card, SubjectMaterial(name, color, color));
        }

        private static void SpawnBolt(Transform parent, Vector3 origin, Vector3 target)
        {
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bolt.name = "Capture_Bolt";
            bolt.transform.SetParent(parent, false);
            Vector3 delta = target - origin;
            bolt.transform.position = origin + delta * 0.45f;
            bolt.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            bolt.transform.localScale = new Vector3(0.055f, delta.magnitude * 0.45f, 0.055f);
            ApplyMaterial(bolt, SubjectMaterial("Bolt", GameTheme.SignalCyan, GameTheme.SignalCyan));
        }

        private static Material SubjectMaterial(string name, Color baseColor, Color emission)
        {
            Shader shader = Shader.Find("EscapeFromNodnarb/ProceduralLit");
            Material material = new Material(shader == null ? Shader.Find("Standard") : shader);
            material.name = "CaptureMaterial_" + name;
            if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_ShadowTint")) material.SetColor("_ShadowTint", Color.Lerp(baseColor, Color.black, 0.55f));
            if (material.HasProperty("_RimColor")) material.SetColor("_RimColor", Color.Lerp(baseColor, GameTheme.SignalCyan, 0.45f));
            if (material.HasProperty("_RimStrength")) material.SetFloat("_RimStrength", 0.24f);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            if (material.HasProperty("_EmissionStrength")) material.SetFloat("_EmissionStrength", emission.maxColorComponent > 0.5f ? 1.4f : 0.15f);
            return material;
        }

        private enum SubjectMode
        {
            Opening,
            Combat,
            Heavy
        }
    }
}
