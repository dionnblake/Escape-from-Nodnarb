using System;

namespace EscapeFromNodnarb
{
    public static class CardChoice
    {
        public const float SideThreshold = 0.45f;

        public static float TargetBias(CardKind kind, float relativeX)
        {
            float normalized = Math.Max(-1f, Math.Min(1f, relativeX / 3.35f));
            float desiredDirection = kind == CardKind.Weapon ? -1f : 1f;
            float alignment = desiredDirection * normalized;
            if (alignment >= 0f)
            {
                return -Math.Min(1.55f, alignment * 1.55f);
            }

            return Math.Min(0.85f, -alignment * 0.55f);
        }

        public static bool IsSideAligned(CardKind kind, float relativeX)
        {
            return kind == CardKind.Weapon
                ? relativeX <= -SideThreshold
                : relativeX >= SideThreshold;
        }

        public static string Hint(float relativeX)
        {
            if (relativeX <= -SideThreshold)
            {
                return "LEFT  //  WEAPON +1";
            }

            if (relativeX >= SideThreshold)
            {
                return "RIGHT  //  CREW +1";
            }

            return "CHOOSE ONE  //  LEFT WEAPON  //  RIGHT CREW";
        }
    }
}
