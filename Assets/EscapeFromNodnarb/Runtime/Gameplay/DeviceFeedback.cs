using UnityEngine;

namespace EscapeFromNodnarb
{
    public enum DeviceFeedbackKind
    {
        Pickup,
        Ability,
        Damage,
        Result
    }

    public static class DeviceFeedback
    {
        private static float lastPulseTime = -100f;

        public static void Pulse(DeviceFeedbackKind kind)
        {
            if (!NodnarbSettings.HapticsEnabled)
            {
                return;
            }

            float minimumInterval = kind == DeviceFeedbackKind.Pickup ? 0.10f : 0.16f;
            if (Time.unscaledTime - lastPulseTime < minimumInterval)
            {
                return;
            }

            lastPulseTime = Time.unscaledTime;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
