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
        public static void Pulse(DeviceFeedbackKind kind)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
