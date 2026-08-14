using UnityEngine;

namespace EscapeFromNodnarb
{
    public static class NodnarbInputPolicy
    {
        public const float BottomActionRailHeight = 0.12f;

        public static bool IsInBottomActionRail(Vector2 pointer, Rect safeArea, int screenHeight)
        {
            Rect effectiveSafeArea = EffectiveSafeArea(safeArea, screenHeight);
            return pointer.y < effectiveSafeArea.yMin + effectiveSafeArea.height * BottomActionRailHeight;
        }

        public static bool CanBeginWorldGesture(Vector2 pointer, Rect safeArea, int screenHeight, bool pointerOverUi)
        {
            return !pointerOverUi && !IsInBottomActionRail(pointer, safeArea, screenHeight);
        }

        private static Rect EffectiveSafeArea(Rect safeArea, int screenHeight)
        {
            float maxHeight = Mathf.Max(0f, screenHeight);
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return new Rect(0f, 0f, 0f, maxHeight);
            }

            float yMin = Mathf.Clamp(safeArea.yMin, 0f, maxHeight);
            float yMax = Mathf.Clamp(safeArea.yMax, yMin, maxHeight);
            return new Rect(safeArea.x, yMin, safeArea.width, yMax - yMin);
        }
    }
}
