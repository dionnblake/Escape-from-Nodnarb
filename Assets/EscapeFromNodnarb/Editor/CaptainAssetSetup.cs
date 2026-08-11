using System;
using UnityEditor;
using UnityEngine;

namespace EscapeFromNodnarb.Editor
{
    public static class CaptainAssetSetup
    {
        private const string ModelPath = "Assets/EscapeFromNodnarb/Runtime/Resources/Captain/Captain_Unity.fbx";
        private const string AtlasPath = "Assets/EscapeFromNodnarb/Runtime/Resources/Captain/captain_color_atlas.png";
        private const string EmissionMaskPath = "Assets/EscapeFromNodnarb/Runtime/Resources/Captain/captain_emission_mask.png";
        private const string MaterialPath = "Assets/EscapeFromNodnarb/Runtime/Resources/Captain/M_Captain_Atlas.mat";
        private const string PrefabPath = "Assets/EscapeFromNodnarb/Runtime/Resources/Captain/CaptainVisual.prefab";

        [MenuItem("Escape from Nodnarb/Configure Captain Asset")]
        public static void ConfigureCaptainAsset()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);

            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Captain FBX importer was not found: " + ModelPath);
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                throw new InvalidOperationException("Captain FBX model could not be loaded: " + ModelPath);
            }

            Material material = CreateCaptainMaterial();
            GameObject instance = UnityEngine.Object.Instantiate(model);
            instance.name = "CaptainVisual";
            AssignMaterial(instance, material);
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            UnityEngine.Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NODNARB_CAPTAIN_ASSET_OK model=" + ModelPath + " prefab=" + PrefabPath + " material=" + MaterialPath);
        }

        private static Material CreateCaptainMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Unity Standard shader was not found.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            Texture2D emissionMask = AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionMaskPath);
            material.mainTexture = atlas;
            material.SetTexture("_EmissionMap", emissionMask);
            material.SetColor("_EmissionColor", GameTheme.Signal * 1.8f);
            material.SetFloat("_Metallic", 0.03f);
            material.SetFloat("_Glossiness", 0.16f);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignMaterial(GameObject root, Material material)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (int slot = 0; slot < materials.Length; slot++)
                {
                    materials[slot] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }
    }
}
