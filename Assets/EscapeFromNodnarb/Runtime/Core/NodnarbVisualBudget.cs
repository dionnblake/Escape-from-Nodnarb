using System;

namespace EscapeFromNodnarb
{
    /// <summary>
    /// Small, inspectable visual budget for the portrait Android slice.
    /// These limits are intentionally separate from gameplay caps so visual
    /// polish cannot silently change encounter balance.
    /// </summary>
    public static class NodnarbVisualBudget
    {
        public const int MaxRuntimeLights = 2;
        public const int MaxShadowCastingLights = 1;
        public const int MaxWorldRenderers = 320;
        public const int MaxWorldMaterials = 64;
        // The runtime cap includes the full active hierarchy: world, pooled
        // actors, projectiles, feedback, and UI. 768 leaves room for the
        // bounded one-frame world transition overlap while still catching
        // accidental unbounded composition growth.
        public const int MaxRuntimeRenderers = 768;
        public const int MaxRuntimeMaterials = 64;
        public const int MaxUiGraphics = 128;
        public const int MaxCachedMaterials = 192;
        public const int MaxActiveProjectiles = 220;
        public const int MaxActiveCombatFeedback = 64;
        public const int MaxParticleSystems = 0;

        public static bool IsWithinRuntimeBudget(
            int rendererCount,
            int materialCount,
            int lightCount,
            int shadowCastingLightCount,
            int particleSystemCount,
            int uiGraphicCount = 0,
            int cachedMaterialCount = 0)
        {
            return rendererCount >= 0
                && rendererCount <= MaxRuntimeRenderers
                && materialCount >= 0
                && materialCount <= MaxRuntimeMaterials
                && lightCount >= 0
                && lightCount <= MaxRuntimeLights
                && shadowCastingLightCount >= 0
                && shadowCastingLightCount <= MaxShadowCastingLights
                && particleSystemCount >= 0
                && particleSystemCount <= MaxParticleSystems
                && uiGraphicCount >= 0
                && uiGraphicCount <= MaxUiGraphics
                && cachedMaterialCount >= 0
                && cachedMaterialCount <= MaxCachedMaterials;
        }

        public static string FormatReport(
            int rendererCount,
            int materialCount,
            int lightCount,
            int shadowCastingLightCount,
            int particleSystemCount,
            int activeProjectiles,
            int activeFeedback,
            int uiGraphicCount = 0,
            int cachedMaterialCount = 0)
        {
            bool within = IsWithinRuntimeBudget(
                rendererCount,
                materialCount,
                lightCount,
                shadowCastingLightCount,
                particleSystemCount,
                uiGraphicCount,
                cachedMaterialCount)
                && activeProjectiles >= 0
                && activeProjectiles <= MaxActiveProjectiles
                && activeFeedback >= 0
                && activeFeedback <= MaxActiveCombatFeedback;
            return "NODNARB_VISUAL_BUDGET status=" + (within ? "PASS" : "OPEN")
                + " renderers=" + rendererCount + "/" + MaxRuntimeRenderers
                + " materials=" + materialCount + "/" + MaxRuntimeMaterials
                + " lights=" + lightCount + "/" + MaxRuntimeLights
                + " shadow_lights=" + shadowCastingLightCount + "/" + MaxShadowCastingLights
                + " particles=" + particleSystemCount + "/" + MaxParticleSystems
                + " projectiles=" + activeProjectiles + "/" + MaxActiveProjectiles
                + " feedback=" + activeFeedback + "/" + MaxActiveCombatFeedback
                + " ui=" + uiGraphicCount + "/" + MaxUiGraphics
                + " cached_materials=" + cachedMaterialCount + "/" + MaxCachedMaterials;
        }
    }
}
