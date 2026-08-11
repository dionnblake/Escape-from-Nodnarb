using UnityEngine;

namespace EscapeFromNodnarb
{
    public static class LaneRoute
    {
        public static float CenterX(RouteShape route, float z)
        {
            float progress = Mathf.InverseLerp(-4f, 26f, z);
            switch (route)
            {
                case RouteShape.CanyonBend:
                    return Mathf.Sin(progress * Mathf.PI * 1.05f - 0.4f) * 0.72f;
                case RouteShape.UnknownWinding:
                    return Mathf.Sin(progress * Mathf.PI * 1.85f + 0.2f) * 0.88f;
                case RouteShape.SnowSwitchback:
                    return Mathf.Sin(progress * Mathf.PI * 2.6f) * 1.02f;
                case RouteShape.RelaySwerve:
                    return Mathf.Sin(progress * Mathf.PI * 1.55f - 0.8f) * 0.58f
                        + Mathf.Sin(progress * Mathf.PI * 3.1f) * 0.20f;
                case RouteShape.CrystalShelf:
                    return Mathf.Sin(progress * Mathf.PI * 0.92f + 0.8f) * 0.48f;
                case RouteShape.HiveRun:
                    return Mathf.Sin(progress * Mathf.PI * 2.1f + 0.55f) * 0.66f;
                case RouteShape.NightSlope:
                    return Mathf.Sin(progress * Mathf.PI * 1.2f - 0.3f) * 0.68f + progress * 0.28f;
                case RouteShape.BeaconApproach:
                    return Mathf.Sin(progress * Mathf.PI * 0.8f) * 0.40f;
                case RouteShape.ExtractionRing:
                    return Mathf.Sin(progress * Mathf.PI * 1.25f) * 0.24f;
                default:
                    return 0f;
            }
        }

        public static float HeadingDegrees(RouteShape route, float z)
        {
            const float sample = 0.18f;
            float slope = (CenterX(route, z + sample) - CenterX(route, z - sample)) / (sample * 2f);
            return Mathf.Atan(slope) * Mathf.Rad2Deg;
        }
    }
}
