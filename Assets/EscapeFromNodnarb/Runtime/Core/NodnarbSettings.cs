using UnityEngine;

namespace EscapeFromNodnarb
{
    public static class NodnarbSettings
    {
        public const string SoundKey = "EscapeFromNodnarb.Settings.Sound.v1";
        public const string MusicKey = "EscapeFromNodnarb.Settings.Music.v1";
        public const string HapticsKey = "EscapeFromNodnarb.Settings.Haptics.v1";
        public const string CaptionsKey = "EscapeFromNodnarb.Settings.Captions.v1";
        public const string HighContrastKey = "EscapeFromNodnarb.Settings.HighContrast.v1";
        public const string ReducedMotionKey = "EscapeFromNodnarb.Settings.ReducedMotion.v1";

        public static bool SoundEnabled
        {
            get { return Read(SoundKey); }
            set { Write(SoundKey, value); }
        }

        public static bool MusicEnabled
        {
            get { return Read(MusicKey); }
            set { Write(MusicKey, value); }
        }

        public static bool HapticsEnabled
        {
            get { return Read(HapticsKey); }
            set { Write(HapticsKey, value); }
        }

        public static bool CaptionsEnabled
        {
            get { return Read(CaptionsKey); }
            set { Write(CaptionsKey, value); }
        }

        public static bool HighContrastEnabled
        {
            get { return Read(HighContrastKey, false); }
            set { Write(HighContrastKey, value); }
        }

        public static bool ReducedMotionEnabled
        {
            get { return Read(ReducedMotionKey, false); }
            set { Write(ReducedMotionKey, value); }
        }

        // Test and accessibility callers use the persisted setting name in
        // both the UI and runtime contracts. Keep this alias side-effect free
        // apart from the existing PlayerPrefs write.
        public static bool ReducedMotion
        {
            get { return ReducedMotionEnabled; }
            set { ReducedMotionEnabled = value; }
        }

        public static string Indicator(string label, bool enabled)
        {
            return label + " // " + (enabled ? "ON" : "OFF");
        }

        private static bool Read(string key)
        {
            return Read(key, true);
        }

        private static bool Read(string key, bool defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        private static void Write(string key, bool enabled)
        {
            PlayerPrefs.SetInt(key, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
