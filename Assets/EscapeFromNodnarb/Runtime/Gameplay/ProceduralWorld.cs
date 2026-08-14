using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromNodnarb
{
    public sealed class ProceduralWorld
    {
        private readonly Transform owner;
        private GameObject root;

        public int RuntimeRendererCount { get; private set; }

        public int RuntimeMaterialCount { get; private set; }

        public string LastVisualBudgetReport { get; private set; }

        public ProceduralWorld(Transform ownerTransform)
        {
            owner = ownerTransform;
        }

        public void Build(LevelDefinition level, Camera camera)
        {
            Clear();
            root = new GameObject("ProceduralWorld");
            root.transform.SetParent(owner, false);

            BiomePalette palette = GameTheme.GetBiome(level.Biome);
            camera.backgroundColor = palette.Sky;
            camera.clearFlags = CameraClearFlags.SolidColor;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = level.Index == 2
                ? Color.Lerp(palette.Terrain, GameTheme.CanyonHighlight, 0.18f)
                : Color.Lerp(palette.Terrain, GameTheme.Text, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = level.Index == 2 ? Color.Lerp(palette.Sky, GameTheme.Void, 0.12f) : palette.Sky;
            RenderSettings.fogStartDistance = level.Index == 2 ? 14f : 18f;
            RenderSettings.fogEndDistance = level.Index == 2 ? 34f : 38f;

            bool crashBasin = level.Index == 2;
            if (crashBasin)
            {
                BuildCrashBasinGround(level, palette);
            }
            else
            {
                BuildGroundBed(level, palette);
                BuildLane(level, palette);
            }

            BuildRouteCenterline(level, palette);
            BuildRouteSignature(level, palette);
            if (crashBasin)
            {
                BuildCrashBasinCliffs(level, palette);
                BuildCrashBasinHorizon(level, palette);
                BuildCrashBasinEnvironmentIdentity(level, palette);
                BuildCrashBasinBiomeFraming(level, palette);
                BuildCrashBasinNearField(level, palette);
            }
            else
            {
                BuildCanyonWalls(level, palette);
                BuildHorizon(level, palette);
                BuildEnvironmentIdentity(level, palette);
                BuildBiomeFraming(level, palette);
                BuildNearFieldIdentity(level, palette);
                BuildTerrain(level, palette);
            }
            if (level.Biome == BiomeId.Snowline)
            {
                BuildSnowlineAccents(level, palette);
            }
            BuildStageLandmarks(level, palette);
            if (level.Index == 1)
            {
                BuildWreck(palette);
            }

            if (level.Index == 10)
            {
                BuildExtractionRing(palette);
            }
            else
            {
                BuildDistantBeacon(palette);
            }

            RefreshVisualBudget();
        }

        public void Clear()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
                root = null;
            }

            RuntimeRendererCount = 0;
            RuntimeMaterialCount = 0;
            LastVisualBudgetReport = string.Empty;
        }

        private void RefreshVisualBudget()
        {
            if (root == null)
            {
                RuntimeRendererCount = 0;
                RuntimeMaterialCount = 0;
                LastVisualBudgetReport = string.Empty;
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            RuntimeRendererCount = renderers.Length;
            HashSet<Material> materials = new HashSet<Material>();
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] == null)
                {
                    continue;
                }

                Material[] sharedMaterials = renderers[index].sharedMaterials;
                for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    if (sharedMaterials[materialIndex] != null)
                    {
                        materials.Add(sharedMaterials[materialIndex]);
                    }
                }
            }

            RuntimeMaterialCount = materials.Count;
            LastVisualBudgetReport = "NODNARB_WORLD_BUDGET renderers=" + RuntimeRendererCount
                + " materials=" + RuntimeMaterialCount
                + " cached_materials=" + PrimitiveFactory.MaterialCacheCount;
            Debug.Log(LastVisualBudgetReport);
        }

        private void BuildCrashBasinGround(LevelDefinition level, BiomePalette palette)
        {
            System.Random random = new System.Random(level.Seed ^ 0x43524153);
            for (int segment = 0; segment < 10; segment++)
            {
                float z = -4.5f + segment * 2.45f + Range(random, -0.24f, 0.24f);
                float centerX = LaneRoute.CenterX(level.Route, z) + Range(random, -0.12f, 0.12f);
                string asset = segment % 2 == 0 ? "CrashBasinGroundA" : "CrashBasinGroundB";
                string name = segment == 0 ? "AlienGroundPatch" : segment == 1 ? "AlienGroundCrust" : "CrashBasinGround_" + segment.ToString("00");
                PlaceCrashBasinAsset(asset, root.transform, new Vector3(centerX, -0.02f, z),
                    Range(random, 0.94f, 1.12f), new Vector3(Range(random, 1.10f, 1.38f), 1f, Range(random, 0.88f, 1.16f)),
                    LaneRoute.HeadingDegrees(level.Route, z) + Range(random, -12f, 12f), palette, name);

                if (segment % 6 == 0)
                {
                    PlaceCrashBasinAsset(segment % 2 == 0 ? "CrashBasinWreckA" : "CrashBasinWreckB", root.transform,
                        new Vector3(centerX + Range(random, -2.7f, 2.7f), 0f, z + Range(random, -0.55f, 0.55f)),
                        Range(random, 0.36f, 0.52f), new Vector3(1.0f, 1.0f, 1.0f),
                        Range(random, -28f, 28f), palette, "CrashBasinDebris_" + segment.ToString("00"));
                }
            }
        }

        private void BuildCrashBasinCliffs(LevelDefinition level, BiomePalette palette)
        {
            System.Random random = new System.Random(level.Seed ^ 0x434C4946);
            for (int segment = 0; segment < 8; segment++)
            {
                float z = -3.5f + segment * 3.10f;
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    float x = LaneRoute.CenterX(level.Route, z) + side * Range(random, 5.35f, 6.15f);
                    string asset = "CrashBasinCliff" + (char)('A' + (segment + sideIndex) % 3);
                    PlaceCrashBasinAsset(asset, root.transform,
                        new Vector3(x, -0.02f, z + Range(random, -0.38f, 0.38f)),
                        Range(random, 0.72f, 1.04f), new Vector3(Range(random, 0.82f, 1.18f), Range(random, 0.92f, 1.24f), Range(random, 0.88f, 1.16f)),
                        LaneRoute.HeadingDegrees(level.Route, z) + side * Range(random, 8f, 24f), palette,
                        "CanyonWall_" + segment.ToString("00") + (sideIndex == 0 ? "_L" : "_R"));
                }
            }
        }

        private void BuildCrashBasinHorizon(LevelDefinition level, BiomePalette palette)
        {
            System.Random random = new System.Random(level.Seed ^ 0x484F525A);
            for (int index = 0; index < 5; index++)
            {
                float z = 26.4f + Range(random, -0.45f, 0.45f);
                float x = LaneRoute.CenterX(level.Route, z) + (index - 2.5f) * 3.0f + Range(random, -0.35f, 0.35f);
                string asset = index % 3 == 0 ? "CrashBasinSpireA" : "CrashBasinCliff" + (char)('A' + index % 3);
                PlaceCrashBasinAsset(asset, root.transform, new Vector3(x, -0.02f, z),
                    Range(random, 0.66f, 0.92f), new Vector3(1.0f, Range(random, 0.92f, 1.18f), 1.0f),
                    Range(random, -16f, 16f), palette, "DistantRidge_" + index.ToString("00"));
            }
        }

        private void BuildCrashBasinEnvironmentIdentity(LevelDefinition level, BiomePalette palette)
        {
            GameObject identity = new GameObject("EnvironmentIdentity_02");
            identity.transform.SetParent(root.transform, false);
            for (int index = 0; index < 3; index++)
            {
                float z = 5.3f + index * 5.9f;
                float side = index % 2 == 0 ? -1f : 1f;
                Vector3 position = new Vector3(LaneRoute.CenterX(level.Route, z) + side * 4.45f, -0.02f, z);
                PlaceCrashBasinAsset(index % 2 == 0 ? "CrashBasinSpireA" : "CrashBasinSpireB", identity.transform,
                    position, 0.76f + index * 0.08f, new Vector3(1.0f, 1.0f + index * 0.08f, 1.0f),
                    side * (12f + index * 8f), palette, "CanyonSpire_" + index.ToString("00"));
                PlaceCrashBasinAsset("CrashBasinRock" + (char)('A' + index % 3), identity.transform,
                    position + new Vector3(-side * 0.86f, 0f, -0.42f), 0.46f, new Vector3(1.0f, 0.72f, 1.0f),
                    side * 22f, palette, "CanyonSpireBase_" + index.ToString("00"));
            }
            PlaceCrashBasinAsset("CrashBasinRockArch", identity.transform,
                new Vector3(LaneRoute.CenterX(level.Route, 17.8f), -0.02f, 17.8f), 0.54f,
                new Vector3(1.0f, 0.82f, 1.0f), 0f, palette, "CanyonBasinArch");
        }

        private void BuildCrashBasinBiomeFraming(LevelDefinition level, BiomePalette palette)
        {
            GameObject frame = new GameObject("BiomeFrame_02");
            frame.transform.SetParent(root.transform, false);
            System.Random random = new System.Random(level.Seed ^ 0x4652414D);
            for (int index = 0; index < 17; index++)
            {
                float z = 1.1f + (index % 6) * 4.0f + (index / 6) * 0.55f;
                float side = index % 2 == 0 ? -1f : 1f;
                float centerX = LaneRoute.CenterX(level.Route, z);
                string asset = index % 5 == 0 ? "CrashBasinFloraA" : index % 5 == 1 ? "CrashBasinWreckB" : "CrashBasinRock" + (char)('A' + index % 3);
                PlaceCrashBasinAsset(asset, frame.transform,
                    new Vector3(centerX + side * Range(random, 4.15f, 4.82f), -0.02f, z),
                    Range(random, 0.40f, 0.62f), new Vector3(1.0f, Range(random, 0.86f, 1.14f), 1.0f),
                    LaneRoute.HeadingDegrees(level.Route, z) + side * Range(random, -24f, 24f), palette,
                    "CrashBasinFrame_" + index.ToString("00"));
            }
            PlaceCrashBasinAsset("CrashBasinRockArch", frame.transform,
                new Vector3(LaneRoute.CenterX(level.Route, 20.5f), -0.02f, 20.5f), 0.78f,
                new Vector3(1.0f, 0.86f, 1.0f), 0f, palette, "BiomeGateTop");
        }

        private void BuildCrashBasinNearField(LevelDefinition level, BiomePalette palette)
        {
            GameObject anchors = new GameObject("NearFieldIdentity_02");
            anchors.transform.SetParent(root.transform, false);
            for (int index = 0; index < 6; index++)
            {
                float z = -0.15f + index * 1.35f;
                float side = index % 2 == 0 ? -1f : 1f;
                string asset = index % 3 == 0 ? "CrashBasinRockA" : index % 3 == 1 ? "CrashBasinFloraB" : "CrashBasinWreckA";
                PlaceCrashBasinAsset(asset, anchors.transform,
                    new Vector3(LaneRoute.CenterX(level.Route, z) + side * 3.86f, -0.02f, z),
                    0.36f + index * 0.038f, new Vector3(1.0f, 1.04f, 1.0f), side * (12f + index * 6f), palette,
                    "NearFieldCrashBasin_" + index.ToString("00"));
            }
        }

        private GameObject PlaceCrashBasinAsset(string resourceName, Transform parent, Vector3 position, float scale,
            Vector3 stretch, float yaw, BiomePalette palette, string name)
        {
            if (!TryBuildImportedLandmark(resourceName, position, scale, palette, true, name, parent))
            {
                return null;
            }

            Transform placed = parent.Find(name);
            if (placed == null)
            {
                return null;
            }

            placed.localScale = Vector3.Scale(placed.localScale, stretch);
            placed.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return placed.gameObject;
        }

        private void BuildTerrain(LevelDefinition level, BiomePalette palette)
        {
            System.Random random = new System.Random(level.Seed);
            for (int i = 0; i < 18; i++)
            {
                float z = Range(random, -3f, 23f);
                float side = i % 2 == 0 ? -1f : 1f;
                float x = LaneRoute.CenterX(level.Route, z) + side * Range(random, 4.7f, 6.5f);
                float scale = Range(random, 0.55f, 1.45f);
                switch (level.Biome)
                {
                    case BiomeId.SporeField:
                        BuildSpore(new Vector3(x, 0f, z), scale, palette);
                        break;
                    case BiomeId.Snowline:
                        BuildSnow(new Vector3(x, 0f, z), scale, palette);
                        break;
                    case BiomeId.CrystalFault:
                    case BiomeId.BeaconPlain:
                        BuildCrystal(new Vector3(x, 0f, z), scale, palette);
                        break;
                    case BiomeId.SignalRuins:
                    case BiomeId.ExtractionRing:
                        BuildRuin(new Vector3(x, 0f, z), scale, palette);
                        break;
                    case BiomeId.BoneMarsh:
                    case BiomeId.HiveTrench:
                        BuildSpine(new Vector3(x, 0f, z), scale, palette);
                        break;
                    default:
                        BuildRock(new Vector3(x, 0f, z), scale, palette);
                        break;
                }
            }
        }

        private void BuildLane(LevelDefinition level, BiomePalette palette)
        {
            // The playable route is open alien ground, not a manufactured road.
            // Broad, irregular patches overlap just enough to hide the seams while
            // scattered stones and growth break the eye's expectation of lanes.
            Color soil = Color.Lerp(palette.Ground, palette.Sky, 0.16f);
            Color soilLight = Color.Lerp(palette.Ground, palette.Terrain, 0.32f);
            Color fracture = Color.Lerp(palette.Sky, palette.Detail, 0.30f);
            System.Random random = new System.Random(level.Seed ^ 0x47524F);
            for (int segment = 0; segment < 14; segment++)
            {
                float z = -5.0f + segment * 2.35f + Range(random, -0.28f, 0.28f);
                float centerX = LaneRoute.CenterX(level.Route, z);
                Quaternion rotation = Quaternion.Euler(0f, LaneRoute.HeadingDegrees(level.Route, z), 0f);
                Vector3 center = new Vector3(centerX + Range(random, -0.18f, 0.18f), -0.36f, z);
                GameObject ground = PrimitiveFactory.Sphere("AlienGroundPatch", root.transform, center,
                    new Vector3(Range(random, 8.4f, 10.6f), Range(random, 0.30f, 0.46f), Range(random, 2.78f, 3.54f)), soil);
                ground.transform.rotation = rotation * Quaternion.Euler(0f, Range(random, -7f, 7f), Range(random, -3f, 3f));
                AddFacet(ground.transform, new Vector3(0.68f, 0.12f, 0.22f), new Vector3(0.22f, 0.08f, 0.48f),
                    Color.Lerp(soil, palette.Terrain, 0.40f), 0.22f, 24f, -8f);
                AddFacet(ground.transform, new Vector3(-0.58f, 0.09f, -0.28f), new Vector3(0.30f, 0.06f, 0.36f),
                    Color.Lerp(soil, palette.Sky, 0.28f), -0.18f, -18f, 7f);

                GameObject crust = PrimitiveFactory.Cube("AlienGroundCrust", root.transform,
                    center + rotation * Vector3.right * Range(random, -2.2f, 2.2f) + Vector3.up * 0.25f,
                    new Vector3(Range(random, 1.1f, 3.8f), 0.08f, Range(random, 0.30f, 0.72f)), soilLight);
                crust.transform.rotation = rotation * Quaternion.Euler(0f, Range(random, -28f, 28f), Range(random, -10f, 10f));

                if (segment % 2 == 0)
                {
                    float side = segment % 4 == 0 ? -1f : 1f;
                    Vector3 split = center + rotation * Vector3.right * (side * Range(random, 0.8f, 2.6f)) + Vector3.up * 0.31f;
                    GameObject crack = PrimitiveFactory.Cube("AlienGroundCrack", root.transform, split,
                        new Vector3(0.035f, 0.024f, Range(random, 0.52f, 1.40f)), fracture);
                    crack.transform.rotation = rotation * Quaternion.Euler(0f, 0f, side * Range(random, 18f, 42f));
                }

                Vector3 moundPosition = center + rotation * Vector3.right * Range(random, -2.6f, 2.6f) + Vector3.up * 0.30f;
                GameObject mound = PrimitiveFactory.Cube("AlienGroundMound", root.transform, moundPosition,
                    new Vector3(Range(random, 0.28f, 0.72f), Range(random, 0.08f, 0.24f), Range(random, 0.34f, 0.82f)),
                    segment % 3 == 0 ? palette.Detail : soilLight);
                mound.transform.rotation = rotation * Quaternion.Euler(0f, Range(random, -45f, 45f), Range(random, -18f, 18f));
            }
        }

        private void BuildGroundBed(LevelDefinition level, BiomePalette palette)
        {
            // A continuous, low profile bed keeps the route from reading as a
            // stack of floating plates while the smaller patches still provide
            // authored breakup and movement landmarks above it.
            Color bedColor = Color.Lerp(palette.Ground, palette.Sky, 0.12f);
            GameObject bed = PrimitiveFactory.Sphere("AlienGroundBed", root.transform,
                new Vector3(LaneRoute.CenterX(level.Route, 10f), -0.66f, 10f),
                new Vector3(15.5f, 0.42f, 34f), bedColor);
            bed.transform.rotation = Quaternion.Euler(0f, LaneRoute.HeadingDegrees(level.Route, 10f) * 0.18f, 0f);

            Color edgeColor = Color.Lerp(palette.Terrain, palette.Ground, 0.34f);
            for (int index = 0; index < 8; index++)
            {
                float z = -3.0f + index * 3.85f;
                float side = index % 2 == 0 ? -1f : 1f;
                float x = LaneRoute.CenterX(level.Route, z) + side * (4.8f + (index % 3) * 0.42f);
                GameObject edge = PrimitiveFactory.Cube("GroundEdgeShard_" + index.ToString("00"), root.transform,
                    new Vector3(x, -0.02f, z), new Vector3(0.46f + (index % 2) * 0.24f, 0.18f, 0.82f), edgeColor);
                edge.transform.rotation = Quaternion.Euler(0f, LaneRoute.HeadingDegrees(level.Route, z) + side * (22f + index * 3f), side * 12f);
            }
        }

        private void BuildRouteCenterline(LevelDefinition level, BiomePalette palette)
        {
            GameObject centerline = new GameObject("RouteCenterline_" + level.Index.ToString("00"));
            centerline.transform.SetParent(root.transform, false);
            Color cueColor = Color.Lerp(palette.Detail, GameTheme.SignalBright, 0.42f);
            System.Random random = new System.Random(level.Seed ^ 0x50415448);
            for (int segment = 0; segment < 12; segment++)
            {
                float z = -3.25f + segment * 2.45f;
                float centerX = LaneRoute.CenterX(level.Route, z);
                Quaternion rotation = Quaternion.Euler(0f, LaneRoute.HeadingDegrees(level.Route, z), 0f);
                float side = segment % 2 == 0 ? -1f : 1f;
                Vector3 position = new Vector3(centerX + side * Range(random, 0.36f, 0.86f), 0.075f, z);
                GameObject marker = PrimitiveFactory.Sphere("OrganicPathMarker", centerline.transform, position,
                    new Vector3(Range(random, 0.11f, 0.20f), Range(random, 0.05f, 0.10f), Range(random, 0.16f, 0.30f)), cueColor);
                marker.transform.rotation = rotation * Quaternion.Euler(0f, Range(random, -25f, 25f), 0f);

                if (segment % 2 == 0)
                {
                    float branchSide = side * -1f;
                    Vector3 forkPosition = new Vector3(centerX + branchSide * Range(random, 0.88f, 1.55f), 0.10f, z + 0.42f);
                    GameObject fork = PrimitiveFactory.Cube("OrganicPathFork", centerline.transform, forkPosition,
                        new Vector3(0.10f, 0.045f, 0.34f), GameTheme.SignalBright);
                    fork.transform.rotation = rotation * Quaternion.Euler(0f, branchSide * Range(random, 22f, 44f), 0f);
                }
            }
        }

        private void BuildCanyonWalls(LevelDefinition level, BiomePalette palette)
        {
            System.Random random = new System.Random(level.Seed ^ 0x4E4F44);
            Color shadow = Color.Lerp(palette.Terrain, palette.Sky, 0.22f);
            for (int segment = 0; segment < 11; segment++)
            {
                float z = -3.5f + segment * 2.85f;
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    float height = Range(random, 1.55f, 3.65f);
                    float x = LaneRoute.CenterX(level.Route, z) + side * Range(random, 5.55f, 6.35f);
                    float width = Range(random, 0.95f, 1.75f);
                    GameObject cliff = PrimitiveFactory.Sphere("CanyonWall", root.transform,
                        new Vector3(x, height * 0.48f - 0.1f, z + Range(random, -0.35f, 0.35f)),
                        new Vector3(width * 1.22f, height * 0.88f, Range(random, 2.45f, 3.85f)),
                        segment % 3 == 0 ? shadow : palette.Terrain);
                    Quaternion rotation = Quaternion.Euler(Range(random, -5f, 5f), Range(random, -18f, 18f), side * Range(random, 4f, 13f));
                    cliff.transform.rotation = rotation;
                    AddFacet(cliff.transform, new Vector3(-side * 0.28f, 0.22f, -0.36f),
                        new Vector3(width * 0.46f, height * 0.18f, 0.12f), palette.Detail, 0.18f, side * 18f, side * 6f);
                    if (segment % 2 == 0)
                    {
                        GameObject facet = PrimitiveFactory.Sphere("CanyonFacet", root.transform,
                            new Vector3(x - side * width * 0.22f, height * 0.56f, z - 0.84f),
                            new Vector3(width * 0.48f, height * 0.22f, 0.20f), palette.Detail);
                        facet.transform.rotation = rotation;
                    }
                }
            }
        }

        private void BuildRouteSignature(LevelDefinition level, BiomePalette palette)
        {
            GameObject markers = new GameObject("RouteSignature_" + level.Route);
            markers.transform.SetParent(root.transform, false);
            Color accent = RouteAccent(level.Route, palette);
            for (int index = 0; index < 5; index++)
            {
                float z = 0.9f + index * 4.35f;
                float side = ((index + (int)level.Route) % 2 == 0) ? -1f : 1f;
                float centerX = LaneRoute.CenterX(level.Route, z);
                Quaternion rotation = Quaternion.Euler(0f, LaneRoute.HeadingDegrees(level.Route, z), 0f);
                Vector3 position = new Vector3(centerX + side * 4.62f, 0f, z);
                GameObject marker;
                switch (level.Route)
                {
                    case RouteShape.WreckInterior:
                        marker = PrimitiveFactory.Cube("HullRib", markers.transform, position + Vector3.up * 0.34f,
                            new Vector3(0.18f, 0.68f, 1.06f), accent);
                        break;
                    case RouteShape.CanyonBend:
                        marker = PrimitiveFactory.Cube("BendRock", markers.transform, position + Vector3.up * 0.38f,
                            new Vector3(0.72f, 0.76f, 0.58f), accent);
                        break;
                    case RouteShape.UnknownWinding:
                        marker = PrimitiveFactory.Sphere("SporeNode", markers.transform, position + Vector3.up * 0.48f,
                            new Vector3(0.62f, 0.36f, 0.62f), accent);
                        break;
                    case RouteShape.SnowSwitchback:
                        marker = PrimitiveFactory.Cube("SwitchbackIce", markers.transform, position + Vector3.up * 0.46f,
                            new Vector3(0.22f, 0.92f, 0.32f), accent);
                        break;
                    case RouteShape.RelaySwerve:
                        marker = PrimitiveFactory.Cube("RelayPost", markers.transform, position + Vector3.up * 0.50f,
                            new Vector3(0.24f, 1.0f, 0.24f), accent);
                        break;
                    case RouteShape.CrystalShelf:
                        marker = PrimitiveFactory.Cube("FaultShard", markers.transform, position + Vector3.up * 0.66f,
                            new Vector3(0.24f, 1.32f, 0.24f), accent);
                        break;
                    case RouteShape.HiveRun:
                        marker = PrimitiveFactory.Sphere("HiveNode", markers.transform, position + Vector3.up * 0.42f,
                            new Vector3(0.72f, 0.48f, 0.72f), accent);
                        break;
                    case RouteShape.NightSlope:
                        marker = PrimitiveFactory.Cube("NightFlag", markers.transform, position + Vector3.up * 0.54f,
                            new Vector3(0.10f, 1.08f, 0.54f), accent);
                        break;
                    case RouteShape.BeaconApproach:
                        marker = PrimitiveFactory.Cylinder("BeaconGuide", markers.transform, position + Vector3.up * 0.48f,
                            new Vector3(0.28f, 0.48f, 0.28f), accent);
                        break;
                    default:
                        marker = PrimitiveFactory.Cube("ExtractionGuide", markers.transform, position + Vector3.up * 0.46f,
                            new Vector3(0.34f, 0.92f, 0.34f), accent);
                        break;
                }

                marker.transform.rotation = rotation * Quaternion.Euler(0f, 0f, side * (6f + index * 2f));
            }
        }

        private static Color RouteAccent(RouteShape route, BiomePalette palette)
        {
            switch (route)
            {
                case RouteShape.WreckInterior:
                case RouteShape.CanyonBend:
                    return GameTheme.WeaponUpgrade;
                case RouteShape.UnknownWinding:
                case RouteShape.HiveRun:
                    return GameTheme.AlienGlow;
                case RouteShape.SnowSwitchback:
                case RouteShape.NightSlope:
                    return palette.Detail;
                case RouteShape.RelaySwerve:
                    return GameTheme.Signal;
                case RouteShape.CrystalShelf:
                    return Color.Lerp(palette.Detail, GameTheme.AlienViolet, 0.25f);
                case RouteShape.BeaconApproach:
                case RouteShape.ExtractionRing:
                    return GameTheme.SignalBright;
                default:
                    return palette.Detail;
            }
        }

        private void BuildHorizon(LevelDefinition level, BiomePalette palette)
        {
            System.Random random = new System.Random(level.Seed ^ 0x484F52);
            Color farColor = Color.Lerp(palette.Sky, palette.Terrain, 0.28f);
            for (int index = 0; index < 7; index++)
            {
                float z = 27.4f + Range(random, -0.35f, 0.35f);
                float centerX = LaneRoute.CenterX(level.Route, z);
                float x = centerX + (index - 3) * 3.15f + Range(random, -0.42f, 0.42f);
                float height = Range(random, 1.45f, 3.15f);
                GameObject ridge = PrimitiveFactory.Sphere("DistantRidge", root.transform,
                    new Vector3(x, height * 0.48f - 0.1f, z),
                    new Vector3(Range(random, 1.05f, 1.85f), height * 0.92f, 0.82f), farColor);
                ridge.transform.rotation = Quaternion.Euler(Range(random, -8f, 8f), Range(random, -16f, 16f), Range(random, -10f, 10f));
                AddFacet(ridge.transform, new Vector3(0f, 0.22f, -0.34f), new Vector3(0.42f, height * 0.20f, 0.08f),
                    Color.Lerp(farColor, palette.Detail, 0.22f), 0.10f, 0f, 0f);
            }

            Color moonColor = Color.Lerp(palette.Detail, palette.Sky, 0.42f);
            PrimitiveFactory.Sphere("DistantMoon", root.transform,
                new Vector3(LaneRoute.CenterX(level.Route, 27.8f) + 4.8f, 6.55f, 27.8f),
                new Vector3(1.18f, 1.18f, 0.42f), moonColor);
        }

        private void BuildEnvironmentIdentity(LevelDefinition level, BiomePalette palette)
        {
            GameObject identity = new GameObject("EnvironmentIdentity_" + level.Index.ToString("00"));
            identity.transform.SetParent(root.transform, false);
            float centerAt = 0f;

            switch (level.Index)
            {
                case 1:
                    for (int index = 0; index < 3; index++)
                    {
                        float z = 4.4f + index * 5.2f;
                        float side = index % 2 == 0 ? -1f : 1f;
                        Vector3 position = new Vector3(LaneRoute.CenterX(level.Route, z) + side * 4.35f, 1.35f, z);
                        GameObject rib = PrimitiveFactory.Cube("CrashHullRib_" + index.ToString("00"), identity.transform,
                            position, new Vector3(0.26f, 2.6f, 0.52f), Color.Lerp(palette.Terrain, GameTheme.Weapon, 0.25f));
                        rib.transform.rotation = Quaternion.Euler(0f, side * 18f, side * 11f);
                        PrimitiveFactory.Cube("CrashWarning_" + index.ToString("00"), identity.transform,
                            position + new Vector3(-side * 0.23f, 0.15f, -0.22f), new Vector3(0.06f, 0.34f, 0.08f),
                            GameTheme.WeaponUpgrade);
                    }
                    break;
                case 2:
                    for (int index = 0; index < 3; index++)
                    {
                        float z = 5.3f + index * 5.9f;
                        float side = index % 2 == 0 ? -1f : 1f;
                        Vector3 position = new Vector3(LaneRoute.CenterX(level.Route, z) + side * 4.45f, 1.72f, z);
                        GameObject spire = PrimitiveFactory.Cube("CanyonSpire_" + index.ToString("00"), identity.transform,
                            position, new Vector3(1.05f, 2.7f + index * 0.22f, 1.18f), palette.Terrain);
                        spire.transform.rotation = Quaternion.Euler(0f, side * (12f + index * 8f), side * 8f);
                        PrimitiveFactory.Cube("CanyonFacet_Identity_" + index.ToString("00"), identity.transform,
                            position + new Vector3(-side * 0.16f, 0.74f, -0.54f), new Vector3(0.48f, 0.24f, 0.10f),
                            palette.Detail);
                    }
                    break;
                case 3:
                    for (int index = 0; index < 4; index++)
                    {
                        float z = 4.2f + index * 4.9f;
                        float side = index % 2 == 0 ? -1f : 1f;
                        Vector3 position = new Vector3(LaneRoute.CenterX(level.Route, z) + side * 4.05f, 0f, z);
                        PrimitiveFactory.Cylinder("SporeLanternStem_" + index.ToString("00"), identity.transform,
                            position + Vector3.up * 0.82f, new Vector3(0.18f, 0.82f, 0.18f), palette.Terrain);
                        PrimitiveFactory.Sphere("SporeLanternCore_" + index.ToString("00"), identity.transform,
                            position + Vector3.up * 1.72f, new Vector3(0.72f, 0.28f, 0.72f),
                            index % 2 == 0 ? GameTheme.AlienViolet : GameTheme.AlienGlow);
                    }
                    break;
                case 4:
                    for (int index = 0; index < 3; index++)
                    {
                        float z = 5.0f + index * 6.1f;
                        float side = index % 2 == 0 ? -1f : 1f;
                        Vector3 position = new Vector3(LaneRoute.CenterX(level.Route, z) + side * 4.25f, 1.55f, z);
                        GameObject pillar = PrimitiveFactory.Cube("SnowPillar_" + index.ToString("00"), identity.transform,
                            position, new Vector3(0.72f, 2.5f + index * 0.18f, 0.72f), palette.Terrain);
                        pillar.transform.rotation = Quaternion.Euler(0f, side * 16f, side * 7f);
                        PrimitiveFactory.Cube("SnowSignal_Identity_" + index.ToString("00"), identity.transform,
                            position + Vector3.up * 0.94f + new Vector3(-side * 0.38f, 0f, -0.38f),
                            new Vector3(0.12f, 0.10f, 0.34f), GameTheme.SignalBright);
                    }
                    PrimitiveFactory.Cube("SnowPassLintel", identity.transform,
                        new Vector3(centerAt, 2.78f, 16.6f), new Vector3(7.4f, 0.22f, 0.34f), palette.Detail);
                    break;
                case 5:
                    for (int index = 0; index < 3; index++)
                    {
                        float z = 5.2f + index * 6.0f;
                        float side = index % 2 == 0 ? -1f : 1f;
                        Vector3 position = new Vector3(LaneRoute.CenterX(level.Route, z) + side * 4.25f, 0f, z);
                        PrimitiveFactory.Cylinder("RelayPylon_" + index.ToString("00"), identity.transform,
                            position + Vector3.up * 0.86f, new Vector3(0.38f, 0.86f, 0.38f), palette.Terrain);
                        PrimitiveFactory.Cube("RelayPylonLight_" + index.ToString("00"), identity.transform,
                            position + Vector3.up * 1.72f, new Vector3(0.10f, 0.42f, 0.10f), GameTheme.SignalBright);
                    }
                    PrimitiveFactory.Cube("RelayRuinCrossbar", identity.transform,
                        new Vector3(centerAt, 2.35f, 14.8f), new Vector3(6.9f, 0.18f, 0.24f), palette.Detail);
                    break;
            }
        }

        private void BuildBiomeFraming(LevelDefinition level, BiomePalette palette)
        {
            // Wilderness framing uses natural shelves, growth, and crystals. The
            // legacy object names remain stable for runtime tests and tooling.
            GameObject frame = new GameObject("BiomeFrame_" + level.Index.ToString("00"));
            frame.transform.SetParent(root.transform, false);
            Color accent = RouteAccent(level.Route, palette);
            Color frameColor = Color.Lerp(palette.Terrain, palette.Detail, 0.26f);
            System.Random random = new System.Random(level.Seed ^ 0x4652414D);

            for (int index = 0; index < 5; index++)
            {
                float z = 1.25f + index * 4.7f;
                float side = index % 2 == 0 ? -1f : 1f;
                float centerX = LaneRoute.CenterX(level.Route, z);
                Quaternion rotation = Quaternion.Euler(0f, LaneRoute.HeadingDegrees(level.Route, z), 0f);
                Vector3 rockPosition = new Vector3(centerX + side * Range(random, 4.25f, 4.85f), Range(random, 1.18f, 1.45f), z);
                GameObject rock = PrimitiveFactory.Sphere("RouteLamp_" + index.ToString("00"), frame.transform,
                    rockPosition, new Vector3(Range(random, 0.72f, 1.30f), Range(random, 1.10f, 1.85f), Range(random, 0.78f, 1.32f)), frameColor);
                rock.transform.rotation = rotation * Quaternion.Euler(0f, Range(random, -28f, 28f), side * Range(random, 5f, 16f));
                PrimitiveFactory.Sphere("RouteLampCore_" + index.ToString("00"), frame.transform,
                    rockPosition + new Vector3(-side * 0.20f, 0.55f, -0.28f), new Vector3(0.22f, 0.16f, 0.18f), accent);
                GameObject growth = PrimitiveFactory.Cube("BiomeBrace_" + index.ToString("00"), frame.transform,
                    rockPosition + new Vector3(side * 0.25f, -0.08f, 0.32f), new Vector3(0.36f, 0.22f, 0.30f), palette.Terrain);
                growth.transform.rotation = rotation * Quaternion.Euler(0f, side * 22f, side * 12f);
                PrimitiveFactory.Cube("RouteLampFacet_" + index.ToString("00"), frame.transform,
                    rockPosition + new Vector3(-side * 0.18f, 0.30f, -0.26f),
                    new Vector3(0.34f, 0.10f, 0.18f), Color.Lerp(frameColor, palette.Detail, 0.28f)).transform.rotation = rotation;
            }

            float gateZ = 20.5f;
            float gateCenter = LaneRoute.CenterX(level.Route, gateZ);
            // Capsule meshes are two world units tall before scaling, so keep
            // their base above the continuous ground bed even when the organic
            // lean below applies a small roll.
            const float gatePostY = 1.50f;
            GameObject gateLeft = PrimitiveFactory.Capsule("BiomeGateLeft", frame.transform,
                new Vector3(gateCenter - 4.75f, gatePostY, gateZ), new Vector3(1.05f, 1.30f, 0.88f), frameColor);
            GameObject gateRight = PrimitiveFactory.Capsule("BiomeGateRight", frame.transform,
                new Vector3(gateCenter + 4.75f, gatePostY, gateZ), new Vector3(1.05f, 1.30f, 0.88f), frameColor);
            Quaternion gateRotation = Quaternion.Euler(0f, LaneRoute.HeadingDegrees(level.Route, gateZ), 0f);
            gateLeft.transform.rotation = gateRotation * Quaternion.Euler(0f, -18f, -10f);
            gateRight.transform.rotation = gateRotation * Quaternion.Euler(0f, 18f, 10f);
            float gateTopZ = level.Biome == BiomeId.NightShelf ? gateZ + 8.0f : gateZ + 4.50f;
            float gateTopHeight = level.Biome == BiomeId.NightShelf ? 3.80f : 2.58f;
            GameObject gateTop = PrimitiveFactory.Sphere("BiomeGateTop", frame.transform,
                new Vector3(gateCenter, gateTopHeight, gateTopZ), new Vector3(5.8f, 0.72f, 1.06f),
                Color.Lerp(frameColor, accent, 0.34f));
            gateTop.transform.rotation = gateRotation * Quaternion.Euler(0f, 0f, 4f);
        }

        private void BuildNearFieldIdentity(LevelDefinition level, BiomePalette palette)
        {
            GameObject anchors = new GameObject("NearFieldIdentity_" + level.Index.ToString("00"));
            anchors.transform.SetParent(root.transform, false);
            Color accent = RouteAccent(level.Route, palette);
            for (int index = 0; index < 3; index++)
            {
                float z = -0.15f + index * 1.85f;
                float side = index % 2 == 0 ? -1f : 1f;
                float centerX = LaneRoute.CenterX(level.Route, z);
                Vector3 position = new Vector3(centerX + side * 3.85f, 0f, z);
                float scale = 0.72f + index * 0.10f;
                GameObject anchor;
                switch (level.Route)
                {
                    case RouteShape.WreckInterior:
                        anchor = PrimitiveFactory.Cube("NearFieldHull_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.55f, new Vector3(scale * 0.34f, scale * 1.10f, scale * 0.72f), GameTheme.Weapon);
                        anchor.transform.rotation = Quaternion.Euler(0f, side * 18f, side * 12f);
                        PrimitiveFactory.Cube("NearFieldWarning_" + index.ToString("00"), anchors.transform,
                            position + new Vector3(-side * 0.18f, scale * 0.62f, -0.38f),
                            new Vector3(0.08f, scale * 0.16f, 0.10f), GameTheme.WeaponUpgrade);
                        break;
                    case RouteShape.CanyonBend:
                        anchor = PrimitiveFactory.Cube("NearFieldRock_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.50f, new Vector3(scale * 1.05f, scale * 0.76f, scale * 0.82f), palette.Terrain);
                        anchor.transform.rotation = Quaternion.Euler(0f, z * 17f, side * 10f);
                        PrimitiveFactory.Cube("NearFieldRockFacet_" + index.ToString("00"), anchors.transform,
                            position + new Vector3(0f, scale * 0.68f, -scale * 0.22f),
                            new Vector3(scale * 0.48f, scale * 0.18f, scale * 0.14f), palette.Detail);
                        break;
                    case RouteShape.UnknownWinding:
                        anchor = PrimitiveFactory.Cylinder("NearFieldSporeStem_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.52f, new Vector3(scale * 0.18f, scale * 0.52f, scale * 0.18f), palette.Terrain);
                        PrimitiveFactory.Sphere("NearFieldSporeCap_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 1.08f, new Vector3(scale * 0.72f, scale * 0.24f, scale * 0.72f), accent);
                        break;
                    case RouteShape.SnowSwitchback:
                        anchor = PrimitiveFactory.Cube("NearFieldIce_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.64f, new Vector3(scale * 0.26f, scale * 1.28f, scale * 0.38f), palette.Detail);
                        anchor.transform.rotation = Quaternion.Euler(0f, side * 14f, side * 6f);
                        PrimitiveFactory.Cube("NearFieldSnowBase_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.16f, new Vector3(scale * 0.92f, scale * 0.24f, scale * 0.72f), palette.Terrain);
                        break;
                    case RouteShape.RelaySwerve:
                        anchor = PrimitiveFactory.Cube("NearFieldRelay_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.58f, new Vector3(scale * 0.34f, scale * 1.16f, scale * 0.34f), palette.Detail);
                        PrimitiveFactory.Cube("NearFieldRelayLight_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 1.10f, new Vector3(scale * 0.16f, scale * 0.16f, scale * 0.16f), GameTheme.SignalBright);
                        break;
                    case RouteShape.CrystalShelf:
                        anchor = PrimitiveFactory.Cube("NearFieldFaultBase_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.20f, new Vector3(scale * 0.96f, scale * 0.40f, scale * 0.72f), palette.Terrain);
                        GameObject shard = PrimitiveFactory.Cube("NearFieldFaultShard_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.72f, new Vector3(scale * 0.30f, scale * 1.44f, scale * 0.30f), accent);
                        shard.transform.rotation = Quaternion.Euler(0f, side * 22f, side * 8f);
                        break;
                    case RouteShape.HiveRun:
                        anchor = PrimitiveFactory.Sphere("NearFieldHiveNode_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.40f, new Vector3(scale * 0.86f, scale * 0.80f, scale * 0.86f), accent);
                        PrimitiveFactory.Cube("NearFieldHiveRoot_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.16f, new Vector3(scale * 1.10f, scale * 0.32f, scale * 0.62f), palette.Terrain);
                        break;
                    case RouteShape.NightSlope:
                        anchor = PrimitiveFactory.Cube("NearFieldNightMarker_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.38f, new Vector3(scale * 0.24f, scale * 0.76f, scale * 0.24f), palette.Detail);
                        PrimitiveFactory.Cube("NearFieldNightSignal_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.82f, new Vector3(scale * 0.46f, scale * 0.16f, scale * 0.12f), accent);
                        break;
                    case RouteShape.BeaconApproach:
                        anchor = PrimitiveFactory.Cylinder("NearFieldBeaconBase_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.40f, new Vector3(scale * 0.72f, scale * 0.36f, scale * 0.72f), palette.Terrain);
                        PrimitiveFactory.Cube("NearFieldBeaconMast_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.74f, new Vector3(scale * 0.14f, scale * 1.12f, scale * 0.14f), GameTheme.SignalBright);
                        break;
                    case RouteShape.ExtractionRing:
                        anchor = PrimitiveFactory.Cube("NearFieldRingSegment_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.24f, new Vector3(scale * 1.12f, scale * 0.48f, scale * 0.34f), palette.Detail);
                        anchor.transform.rotation = Quaternion.Euler(0f, side * 18f, 0f);
                        PrimitiveFactory.Cube("NearFieldRingSignal_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.68f, new Vector3(scale * 0.44f, scale * 0.18f, scale * 0.12f), GameTheme.SignalBright);
                        break;
                    default:
                        anchor = PrimitiveFactory.Cube("NearFieldGuide_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.42f, new Vector3(scale * 0.40f, scale * 0.84f, scale * 0.40f), accent);
                        PrimitiveFactory.Cube("NearFieldGuideLight_" + index.ToString("00"), anchors.transform,
                            position + Vector3.up * scale * 0.90f, new Vector3(scale * 0.18f, scale * 0.12f, scale * 0.18f), GameTheme.SignalBright);
                        break;
                }

                anchor.transform.SetSiblingIndex(index * 3);
            }
        }

        private void BuildRock(Vector3 position, float scale, BiomePalette palette)
        {
            GameObject rock = PrimitiveFactory.Cube("AlienRock", root.transform, position + Vector3.up * scale * 0.35f,
                new Vector3(scale, scale * 0.72f, scale * 0.9f), palette.Terrain);
            rock.transform.rotation = Quaternion.Euler(0f, position.z * 19f, 18f);
            if (((int)(position.z * 10f)) % 4 == 0)
            {
                PrimitiveFactory.Cube("RockFacet", root.transform, position + new Vector3(0f, scale * 0.48f, -scale * 0.32f),
                    new Vector3(scale * 0.46f, scale * 0.26f, scale * 0.12f), Color.Lerp(palette.Terrain, GameTheme.CanyonHighlight, 0.24f));
            }
        }

        private void BuildSpore(Vector3 position, float scale, BiomePalette palette)
        {
            PrimitiveFactory.Cylinder("SporeStem", root.transform, position + Vector3.up * scale * 0.65f,
                new Vector3(scale * 0.15f, scale * 0.65f, scale * 0.15f), Color.Lerp(palette.Terrain, GameTheme.AlienViolet, 0.22f));
            GameObject cap = PrimitiveFactory.Sphere("SporeCap", root.transform, position + Vector3.up * scale * 1.3f,
                new Vector3(scale, scale * 0.28f, scale), palette.Detail);
            cap.transform.rotation = Quaternion.Euler(0f, position.z * 13f, 0f);
            AddFacet(cap.transform, new Vector3(0f, 0.06f, 0.12f), new Vector3(scale * 0.52f, scale * 0.05f, scale * 0.18f),
                GameTheme.AlienGlow, 0.06f, position.z * 9f, 0f);
        }

        private void BuildSnow(Vector3 position, float scale, BiomePalette palette)
        {
            GameObject drift = PrimitiveFactory.Cube("SnowDrift", root.transform,
                position + Vector3.up * scale * 0.30f,
                new Vector3(scale * 1.15f, scale * 0.60f, scale * 0.72f), palette.Terrain);
            drift.transform.rotation = Quaternion.Euler(0f, position.z * 11f, 8f);
            GameObject cap = PrimitiveFactory.Cube("SnowCap", root.transform,
                position + new Vector3(scale * 0.10f, scale * 0.72f, -scale * 0.12f),
                new Vector3(scale * 0.88f, scale * 0.16f, scale * 0.54f), palette.Detail);
            cap.transform.rotation = Quaternion.Euler(0f, position.z * 11f, 5f);
        }

        private void BuildSnowlineAccents(LevelDefinition level, BiomePalette palette)
        {
            GameObject markers = new GameObject("SnowlineMarkers");
            markers.transform.SetParent(root.transform, false);
            for (int index = 0; index < 5; index++)
            {
                float z = 1.0f + index * 4.45f;
                float side = index % 2 == 0 ? -1f : 1f;
                float centerX = LaneRoute.CenterX(level.Route, z);
                Quaternion rotation = Quaternion.Euler(0f, LaneRoute.HeadingDegrees(level.Route, z), 0f);
                Vector3 position = new Vector3(centerX + side * 4.62f, 0f, z);
                PrimitiveFactory.Cube("SnowShelf_" + index.ToString("00"), markers.transform,
                    position + Vector3.up * 0.16f, new Vector3(1.12f, 0.26f, 0.62f),
                    Color.Lerp(palette.Terrain, palette.Detail, 0.42f)).transform.rotation = rotation;
                GameObject shard = PrimitiveFactory.Cube("IceShard_" + index.ToString("00"), markers.transform,
                    position + Vector3.up * 0.70f, new Vector3(0.18f, 1.12f, 0.24f), palette.Detail);
                shard.transform.rotation = rotation * Quaternion.Euler(0f, side * 18f, side * 18f);
                PrimitiveFactory.Cube("SnowSignal_" + index.ToString("00"), markers.transform,
                    position + Vector3.up * 0.31f + rotation * Vector3.right * (side * -0.34f),
                    new Vector3(0.28f, 0.045f, 0.08f), GameTheme.SignalBright).transform.rotation = rotation;
            }
        }

        private void BuildCrystal(Vector3 position, float scale, BiomePalette palette)
        {
            GameObject crystal = PrimitiveFactory.Cube("Crystal", root.transform, position + Vector3.up * scale,
                new Vector3(scale * 0.42f, scale * 2f, scale * 0.42f), palette.Detail);
            crystal.transform.rotation = Quaternion.Euler(8f, position.z * 17f, 18f);
        }

        private void BuildRuin(Vector3 position, float scale, BiomePalette palette)
        {
            PrimitiveFactory.Cube("RuinPost", root.transform, position + Vector3.up * scale,
                new Vector3(scale * 0.5f, scale * 2f, scale * 0.5f), palette.Terrain);
            PrimitiveFactory.Cube("RuinArm", root.transform, position + new Vector3(scale * 0.45f, scale * 1.65f, 0f),
                new Vector3(scale * 1.2f, scale * 0.24f, scale * 0.42f), palette.Detail);
        }

        private void BuildSpine(Vector3 position, float scale, BiomePalette palette)
        {
            for (int joint = 0; joint < 4; joint++)
            {
                GameObject bone = PrimitiveFactory.Cube("Spine", root.transform,
                    position + new Vector3(0f, scale * (0.35f + joint * 0.42f), joint * scale * 0.08f),
                    new Vector3(scale * (0.8f - joint * 0.12f), scale * 0.18f, scale * 0.22f), palette.Detail);
                bone.transform.rotation = Quaternion.Euler(0f, position.z * 7f, joint % 2 == 0 ? 14f : -14f);
            }
        }

        private void BuildStageLandmarks(LevelDefinition level, BiomePalette palette)
        {
            if (level.Index == 1)
            {
                TryBuildImportedLandmark("CrashedEngine", new Vector3(
                    LaneRoute.CenterX(level.Route, 13.5f) + 4.7f, 0f, 13.5f), 0.86f, palette);
            }

            if (level.Index == 2)
            {
                TryBuildImportedLandmark("CanyonDebris", new Vector3(
                    LaneRoute.CenterX(level.Route, 14.2f) + 4.6f, 0f, 14.2f), 0.88f, palette);
            }

            if (level.Biome == BiomeId.SporeField || level.Biome == BiomeId.HiveTrench)
            {
                TryBuildImportedLandmark("HiveGrowth", new Vector3(
                    LaneRoute.CenterX(level.Route, 15.4f) - 4.55f, 0f, 15.4f), 0.82f, palette);
            }

            if (level.Index == 3)
            {
                TryBuildImportedLandmark("SporeArch", new Vector3(
                    LaneRoute.CenterX(level.Route, 9.8f) + 4.72f, 0f, 9.8f), 0.68f, palette);
            }

            if (level.Biome == BiomeId.CrystalFault || level.Biome == BiomeId.BeaconPlain)
            {
                TryBuildImportedLandmark("CrystalCluster", new Vector3(
                    LaneRoute.CenterX(level.Route, 14.6f) + 4.45f, 0f, 14.6f), 0.88f, palette);
            }

            if (level.Biome == BiomeId.Snowline)
            {
                TryBuildImportedLandmark("SnowArch", new Vector3(
                    LaneRoute.CenterX(level.Route, 15.5f), 0f, 15.5f), 0.82f, palette);
            }

            if (level.Biome == BiomeId.SignalRuins || level.Index == 10)
            {
                TryBuildImportedLandmark("RelayBeacon", new Vector3(
                    LaneRoute.CenterX(level.Route, 17.5f) - 4.4f, 0f, 17.5f), 0.82f, palette);
            }

            if (level.Biome == BiomeId.SignalRuins || level.Biome == BiomeId.NightShelf)
            {
                TryBuildImportedLandmark("RuinGate", new Vector3(
                    LaneRoute.CenterX(level.Route, 12.4f) + 4.45f, 0f, 12.4f), 0.72f, palette);
            }

            if (level.Biome == BiomeId.HiveTrench)
            {
                TryBuildImportedLandmark("HiveObelisk", new Vector3(
                    LaneRoute.CenterX(level.Route, 12.8f) + 4.45f, 0f, 12.8f), 0.78f, palette);
            }

            if (level.Biome == BiomeId.BeaconPlain || level.Index == 10)
            {
                TryBuildImportedLandmark("ExtractionBeacon", new Vector3(
                    LaneRoute.CenterX(level.Route, 16.2f) + 4.40f, 0f, 16.2f), 0.74f, palette);
            }
        }

        private void BuildWreck(BiomePalette palette)
        {
            GameObject hull = PrimitiveFactory.Cube("CrashedHull", root.transform, new Vector3(-5.55f, 1.25f, 7.6f),
                new Vector3(4.9f, 1.55f, 2.15f), Color.Lerp(palette.Terrain, GameTheme.Weapon, 0.22f));
            hull.transform.rotation = Quaternion.Euler(0f, 24f, -12f);
            PrimitiveFactory.Cube("HullBreach", root.transform, new Vector3(-4.82f, 1.38f, 6.92f),
                new Vector3(1.65f, 0.78f, 0.20f), GameTheme.Void).transform.rotation = hull.transform.rotation;
            PrimitiveFactory.Cube("BrokenWing", root.transform, new Vector3(-3.9f, 0.44f, 8.25f),
                new Vector3(4.2f, 0.20f, 1.25f), GameTheme.Rule).transform.rotation = Quaternion.Euler(0f, -26f, 9f);
            PrimitiveFactory.Cylinder("Engine", root.transform, new Vector3(-6.75f, 1.05f, 7.18f),
                new Vector3(0.82f, 0.62f, 0.82f), GameTheme.Rule).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            PrimitiveFactory.Cube("WreckSignal", root.transform, new Vector3(-5.18f, 2.48f, 7.42f),
                new Vector3(0.11f, 1.25f, 0.11f), GameTheme.SignalBright);
            PrimitiveFactory.Cube("WreckOrangePanel", root.transform, new Vector3(-5.00f, 1.82f, 7.92f),
                new Vector3(1.24f, 0.12f, 0.34f), GameTheme.CanyonHighlight).transform.rotation = hull.transform.rotation;
            PrimitiveFactory.Cube("WreckSignalWindow", root.transform, new Vector3(-4.38f, 1.56f, 7.38f),
                new Vector3(0.18f, 0.22f, 0.06f), GameTheme.SignalBright).transform.rotation = hull.transform.rotation;
        }

        private void BuildDistantBeacon(BiomePalette palette)
        {
            PrimitiveFactory.Cylinder("DistantBeaconBase", root.transform, new Vector3(0f, 0.42f, 25.2f),
                new Vector3(0.72f, 0.42f, 0.72f), Color.Lerp(palette.Terrain, GameTheme.Rule, 0.50f));
            PrimitiveFactory.Cube("DistantBeaconMast", root.transform, new Vector3(0f, 1.72f, 25.2f),
                new Vector3(0.12f, 2.4f, 0.12f), GameTheme.Signal);
            PrimitiveFactory.Sphere("DistantBeaconPulse", root.transform, new Vector3(0f, 3.05f, 25.2f),
                new Vector3(0.34f, 0.34f, 0.34f), GameTheme.SignalBright);
        }

        private void BuildExtractionRing(BiomePalette palette)
        {
            for (int part = 0; part < 8; part++)
            {
                float angle = part * Mathf.PI * 0.25f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 6.3f, 2f + Mathf.Sin(angle) * 1.6f, 18f);
                GameObject segment = PrimitiveFactory.Cube("BeaconRing", root.transform, position,
                    new Vector3(0.35f, 1.5f, 0.35f), palette.Detail);
                segment.transform.rotation = Quaternion.Euler(0f, 0f, -part * 45f);
            }

            PrimitiveFactory.Cylinder("BeaconCore", root.transform, new Vector3(0f, 1.2f, 18f),
                new Vector3(0.7f, 1.2f, 0.7f), GameTheme.Signal);
        }

        private bool TryBuildImportedLandmark(string resourceName, Vector3 position, float scale, BiomePalette palette,
            bool stylized = false, string overrideName = null, Transform parent = null)
        {
            GameObject prefab = Resources.Load<GameObject>("World/" + resourceName);
            if (prefab == null)
            {
                return false;
            }

            Transform targetParent = parent == null ? root.transform : parent;
            GameObject landmark = UnityEngine.Object.Instantiate(prefab, targetParent);
            landmark.name = string.IsNullOrEmpty(overrideName) ? resourceName + "Landmark" : overrideName;
            landmark.transform.localPosition = position;
            landmark.transform.localRotation = Quaternion.Euler(0f, position.z * 7f, 0f);
            // Blender exports these world props in centimetre file units. Unity's
            // runtime FBX root keeps the file-unit scale, so restore playable metres
            // here while keeping the authored per-landmark scale readable on mobile.
            const float importedWorldUnitScale = 100f;
            Vector3 authoredScale = resourceName == "SnowArch"
                ? new Vector3(scale, scale * 1.55f, scale)
                : Vector3.one * scale;
            landmark.transform.localScale = authoredScale * importedWorldUnitScale;

            Renderer[] renderers = landmark.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(landmark.transform.position, Vector3.zero);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (rendererIndex > 0)
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                Material[] sourceMaterials = renderer.sharedMaterials;
                if (sourceMaterials == null || sourceMaterials.Length == 0)
                {
                    renderer.sharedMaterial = PrimitiveFactory.Material(LandmarkColor(resourceName, string.Empty, palette));
                    continue;
                }

                Material[] runtimeMaterials = new Material[sourceMaterials.Length];
                for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
                {
                    Material sourceMaterial = sourceMaterials[materialIndex];
                    string materialName = sourceMaterial == null ? string.Empty : sourceMaterial.name;
                    Color materialColor = LandmarkColor(resourceName, materialName, palette);
                    if (sourceMaterial != null && stylized)
                    {
                        runtimeMaterials[materialIndex] = PrimitiveFactory.AuthoredMaterial(
                            sourceMaterial, materialColor, Color.Lerp(palette.Sky, GameTheme.Void, 0.58f),
                            Color.Lerp(materialColor, GameTheme.SignalCyan, 0.22f),
                            materialName.ToLowerInvariant().Contains("signal") || materialName.ToLowerInvariant().Contains("accent") ? 0.30f : 0.08f);
                    }
                    else if (sourceMaterial != null)
                    {
                        // Preserve authored FBX materials for legacy landmarks.
                        runtimeMaterials[materialIndex] = sourceMaterial;
                    }
                    else
                    {
                        runtimeMaterials[materialIndex] = PrimitiveFactory.Material(materialColor);
                    }
                }

                renderer.sharedMaterials = runtimeMaterials;
            }

            if (renderers.Length > 0)
            {
                LODGroup lodGroup = landmark.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = landmark.AddComponent<LODGroup>();
                }

                lodGroup.SetLODs(new[]
                {
                    new LOD(0.18f, renderers),
                    new LOD(0.025f, new Renderer[0])
                });
                lodGroup.RecalculateBounds();
            }

            if (renderers.Length > 0 && bounds.min.y < 0.02f)
            {
                landmark.transform.position += Vector3.up * (0.02f - bounds.min.y);
            }

            return renderers.Length > 0;
        }

        private static void AddFacet(Transform parent, Vector3 localPosition, Vector3 scale, Color color,
            float verticalOffset, float yaw, float roll)
        {
            GameObject facet = PrimitiveFactory.Cube("AuthoredFacet", parent, parent.position + localPosition,
                scale, color);
            facet.transform.SetParent(parent, true);
            facet.transform.localPosition = localPosition + Vector3.up * verticalOffset;
            facet.transform.localRotation = Quaternion.Euler(0f, yaw, roll);
        }

        private static Color LandmarkColor(string resourceName, string materialName, BiomePalette palette)
        {
            string name = materialName.ToLowerInvariant();
            if (name.Contains("signal"))
            {
                return name.Contains("crashsignal") ? GameTheme.SignalCyan : GameTheme.SignalBright;
            }

            if (name.Contains("crashaccent"))
            {
                return GameTheme.CanyonHighlight;
            }

            if (name.Contains("crashorganic"))
            {
                return Color.Lerp(palette.Terrain, GameTheme.Signal, 0.24f);
            }

            if (name.Contains("shadow") || name.Contains("dark"))
            {
                return Color.Lerp(palette.Sky, GameTheme.Void, 0.56f);
            }

            if (name.Contains("snow"))
            {
                return palette.Detail;
            }

            if (name.Contains("crystal"))
            {
                return Color.Lerp(palette.Detail, GameTheme.AlienViolet, 0.30f);
            }

            if (name.Contains("core"))
            {
                return GameTheme.AlienGlow;
            }

            if (name.Contains("organic"))
            {
                return Color.Lerp(palette.Terrain, GameTheme.AlienViolet, 0.34f);
            }

            if (name.Contains("flesh"))
            {
                return Color.Lerp(palette.Detail, GameTheme.AlienGlow, 0.24f);
            }

            if (name.Contains("dark"))
            {
                return Color.Lerp(palette.Sky, GameTheme.Void, 0.56f);
            }

            if (name.Contains("metal") || name.Contains("trim"))
            {
                return Color.Lerp(palette.Terrain, GameTheme.Weapon, 0.36f);
            }

            if (name.Contains("rock"))
            {
                return palette.Terrain;
            }

            return resourceName == "SnowArch" ? palette.Detail : palette.Terrain;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }

    public static class PrimitiveFactory
    {
        private static readonly Dictionary<int, Material> Materials = new Dictionary<int, Material>();
        private static readonly Dictionary<string, Material> AuthoredMaterials = new Dictionary<string, Material>();

        public static int MaterialCacheCount
        {
            get { return Materials.Count + AuthoredMaterials.Count; }
        }

        public static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Cube, name, parent, position, scale, color);
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Sphere, name, parent, position, scale, color);
        }

        public static GameObject Capsule(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Capsule, name, parent, position, scale, color);
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Cylinder, name, parent, position, scale, color);
        }

        public static Material Material(Color color)
        {
            Color32 keyColor = color;
            int key = keyColor.r | keyColor.g << 8 | keyColor.b << 16 | keyColor.a << 24;
            Material material;
            if (Materials.TryGetValue(key, out material) && material != null)
            {
                return material;
            }

            Shader shader = Resources.Load<Shader>("NodnarbProceduralLit");
            if (shader == null)
            {
                shader = Shader.Find("EscapeFromNodnarb/ProceduralLit");
            }

            if (shader == null)
            {
                throw new System.InvalidOperationException("Nodnarb procedural shader was not included in the player build.");
            }

            material = new Material(shader) { color = color, name = "Procedural_" + key.ToString("X8") };
            material.enableInstancing = true;
            material.SetColor("_RimColor", Color.Lerp(color, GameTheme.Signal, 0.22f));
            material.SetFloat("_RimPower", 2.4f);
            material.SetFloat("_RimStrength", 0.18f);
            material.SetColor("_ShadowColor", Color.Lerp(GameTheme.Void, color, 0.42f));
            material.SetFloat("_DirectionalStrength", 0.82f);
            material.SetFloat("_AmbientStrength", 0.38f);
            material.SetColor("_SpecularColor", Color.Lerp(Color.white, color, 0.34f));
            material.SetFloat("_SpecularStrength", 0.09f);

            bool signalLike = color.g > color.r * 1.12f && color.g > color.b * 1.05f;
            bool hostileLike = color.r > color.g * 1.45f && color.r > color.b * 1.20f;
            material.SetColor("_EmissionColor", signalLike || hostileLike ? color : Color.black);
            material.SetFloat("_EmissionStrength", signalLike || hostileLike ? 0.22f : 0f);
            Materials[key] = material;
            return material;
        }

        public static Material AuthoredMaterial(Material source, Color tint, Color shadowColor, Color rimColor,
            float emissionStrength)
        {
            Texture mainTexture = source != null && source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
            Texture emissionTexture = source != null && source.HasProperty("_EmissionMap") ? source.GetTexture("_EmissionMap") : null;
            bool hasTexture = (mainTexture != null && mainTexture != Texture2D.whiteTexture)
                || (emissionTexture != null && emissionTexture != Texture2D.blackTexture);
            Color32 tintKey = tint;
            Color32 shadowKey = shadowColor;
            Color32 rimKey = rimColor;
            string key = (hasTexture && source != null ? source.GetInstanceID() : 0).ToString()
                + "_" + ((int)tintKey.r | (int)tintKey.g << 8 | (int)tintKey.b << 16).ToString("X6")
                + "_" + ((int)shadowKey.r | (int)shadowKey.g << 8 | (int)shadowKey.b << 16).ToString("X6")
                + "_" + ((int)rimKey.r | (int)rimKey.g << 8 | (int)rimKey.b << 16).ToString("X6")
                + "_" + emissionStrength.ToString("0.00");
            Material material;
            if (AuthoredMaterials.TryGetValue(key, out material) && material != null)
            {
                return material;
            }

            Shader shader = Resources.Load<Shader>("NodnarbProceduralLit");
            if (shader == null)
            {
                shader = Shader.Find("EscapeFromNodnarb/ProceduralLit");
            }

            if (shader == null)
            {
                throw new System.InvalidOperationException("Nodnarb procedural shader was not included in the player build.");
            }

            material = source == null ? new Material(shader) : new Material(source);
            material.shader = shader;
            material.name = "CrashBasin_" + key;
            material.enableInstancing = true;
            material.color = source != null && source.HasProperty("_Color")
                ? Multiply(source.color, tint)
                : tint;
            if (mainTexture != null)
            {
                material.SetTexture("_MainTex", mainTexture);
            }
            if (emissionTexture != null)
            {
                material.SetTexture("_EmissionMap", emissionTexture);
            }
            material.SetColor("_ShadowColor", shadowColor);
            material.SetFloat("_DirectionalStrength", 0.88f);
            material.SetFloat("_AmbientStrength", 0.34f);
            material.SetColor("_RimColor", rimColor);
            material.SetFloat("_RimPower", 2.2f);
            material.SetFloat("_RimStrength", 0.16f);
            material.SetColor("_SpecularColor", Color.Lerp(Color.white, tint, 0.42f));
            material.SetFloat("_SpecularStrength", 0.06f);
            material.SetColor("_EmissionColor", Color.Lerp(tint, rimColor, 0.24f));
            material.SetFloat("_EmissionStrength", emissionStrength);
            AuthoredMaterials[key] = material;
            return material;
        }

        private static Color Multiply(Color left, Color right)
        {
            return new Color(left.r * right.r, left.g * right.g, left.b * right.b, left.a * right.a);
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.position = position;
            value.transform.localScale = scale;
            Renderer renderer = value.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = Material(color);
            }

            Collider collider = value.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            return value;
        }
    }
}
