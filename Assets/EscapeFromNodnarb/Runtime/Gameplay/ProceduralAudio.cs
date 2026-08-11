using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromNodnarb
{
    public sealed class ProceduralAudio : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource source;

        public void Initialize()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.48f;

            clips["shot"] = CreateTone("SquadShot", 0.045f, 780f, 420f, 0.17f, false);
            clips["hit"] = CreateTone("AlienHit", 0.055f, 180f, 95f, 0.17f, true);
            clips["recruit"] = CreateTone("Recruit", 0.16f, 420f, 780f, 0.25f, false);
            clips["upgrade"] = CreateTone("Upgrade", 0.18f, 330f, 910f, 0.25f, false);
            clips["ability"] = CreateTone("RapidFire", 0.24f, 220f, 980f, 0.28f, false);
            clips["boss"] = CreateTone("BossTelegraph", 0.22f, 118f, 52f, 0.30f, true);
            clips["damage"] = CreateTone("CaptainDamage", 0.18f, 130f, 75f, 0.30f, true);
            clips["victory"] = CreateTone("Extraction", 0.55f, 310f, 720f, 0.32f, false);
            clips["defeat"] = CreateTone("SignalLost", 0.52f, 210f, 62f, 0.34f, true);
        }

        public void Shot()
        {
            Play("shot", 0.56f);
        }

        public void Hit()
        {
            Play("hit", 0.42f);
        }

        public void Recruit()
        {
            Play("recruit", 0.85f);
        }

        public void Upgrade()
        {
            Play("upgrade", 0.82f);
        }

        public void Ability()
        {
            Play("ability", 0.92f);
        }

        public void BossTelegraph()
        {
            Play("boss", 0.88f);
        }

        public void Damage()
        {
            Play("damage", 0.90f);
        }

        public void Victory()
        {
            Play("victory", 1f);
        }

        public void Defeat()
        {
            Play("defeat", 1f);
        }

        private void Play(string key, float volume)
        {
            AudioClip clip;
            if (source != null && clips.TryGetValue(key, out clip))
            {
                source.PlayOneShot(clip, volume);
            }
        }

        private static AudioClip CreateTone(string name, float duration, float startFrequency, float endFrequency, float amplitude, bool noisy)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(name.GetHashCode());
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)Math.Max(1, sampleCount - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += Mathf.PI * 2f * frequency / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * (1f - t * 0.42f);
                float noise = noisy ? ((float)random.NextDouble() * 2f - 1f) * 0.36f : 0f;
                samples[i] = (Mathf.Sin(phase) + noise) * envelope * amplitude;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
