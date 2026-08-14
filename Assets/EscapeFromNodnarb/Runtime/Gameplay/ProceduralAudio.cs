using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromNodnarb
{
    public sealed class ProceduralAudio : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource source;
        private AudioSource musicSource;
        private AudioClip[] musicLayers;
        private string activeMusic;
        private int activeMusicIndex = -1;
        private float activeMusicIntensity = -1f;
        private bool initialized;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            InitializeSources();
            CreateEffectClips();
            initialized = true;
        }

        public IEnumerator InitializeRoutine()
        {
            if (initialized)
            {
                yield break;
            }

            InitializeSources();
            yield return null;
            clips["shot"] = CreateTone("SquadShot", 0.045f, 780f, 420f, 0.17f, false);
            clips["hit"] = CreateTone("AlienHit", 0.055f, 180f, 95f, 0.17f, true);
            clips["recruit"] = CreateTone("Recruit", 0.16f, 420f, 780f, 0.25f, false);
            yield return null;
            clips["upgrade"] = CreateTone("Upgrade", 0.18f, 330f, 910f, 0.25f, false);
            clips["ability"] = CreateTone("RapidFire", 0.24f, 220f, 980f, 0.28f, false);
            clips["boss"] = CreateTone("BossTelegraph", 0.22f, 118f, 52f, 0.30f, true);
            yield return null;
            clips["damage"] = CreateTone("CaptainDamage", 0.18f, 130f, 75f, 0.30f, true);
            clips["victory"] = CreateTone("Extraction", 0.55f, 310f, 720f, 0.32f, false);
            clips["defeat"] = CreateTone("SignalLost", 0.52f, 210f, 62f, 0.34f, true);
            clips["wave"] = CreateTone("CreatureWave", 0.12f, 92f, 168f, 0.19f, true);
            yield return null;
            musicLayers = new AudioClip[4];
            musicLayers[0] = CreateMusic("AlienWind", 8.0f, 74f, 0.045f, 0.22f);
            yield return null;
            musicLayers[1] = CreateMusic("HordePulse", 6.0f, 96f, 0.060f, 0.40f);
            yield return null;
            musicLayers[2] = CreateMusic("BossPressure", 5.0f, 58f, 0.075f, 0.68f);
            yield return null;
            musicLayers[3] = CreateMusic("ExtractionSignal", 7.0f, 132f, 0.055f, 0.85f);
            initialized = true;
        }

        private void InitializeSources()
        {
            if (source != null)
            {
                return;
            }

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.priority = 64;
            source.dopplerLevel = 0f;
            source.volume = NodnarbSettings.SoundEnabled ? 0.48f : 0f;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 128;
            musicSource.dopplerLevel = 0f;
            musicSource.volume = NodnarbSettings.MusicEnabled ? 0.10f : 0f;
        }

        public void RefreshSettings()
        {
            if (source != null)
            {
                source.volume = NodnarbSettings.SoundEnabled ? 0.48f : 0f;
            }

            if (musicSource == null)
            {
                return;
            }

            if (!NodnarbSettings.MusicEnabled)
            {
                musicSource.Stop();
                activeMusic = null;
                activeMusicIndex = -1;
                activeMusicIntensity = -1f;
                musicSource.volume = 0f;
            }
        }

        private void CreateEffectClips()
        {
            clips["shot"] = CreateTone("SquadShot", 0.045f, 780f, 420f, 0.17f, false);
            clips["hit"] = CreateTone("AlienHit", 0.055f, 180f, 95f, 0.17f, true);
            clips["recruit"] = CreateTone("Recruit", 0.16f, 420f, 780f, 0.25f, false);
            clips["upgrade"] = CreateTone("Upgrade", 0.18f, 330f, 910f, 0.25f, false);
            clips["ability"] = CreateTone("RapidFire", 0.24f, 220f, 980f, 0.28f, false);
            clips["boss"] = CreateTone("BossTelegraph", 0.22f, 118f, 52f, 0.30f, true);
            clips["damage"] = CreateTone("CaptainDamage", 0.18f, 130f, 75f, 0.30f, true);
            clips["victory"] = CreateTone("Extraction", 0.55f, 310f, 720f, 0.32f, false);
            clips["defeat"] = CreateTone("SignalLost", 0.52f, 210f, 62f, 0.34f, true);
            clips["wave"] = CreateTone("CreatureWave", 0.12f, 92f, 168f, 0.19f, true);
            musicLayers = new[]
            {
                CreateMusic("AlienWind", 8.0f, 74f, 0.045f, 0.22f),
                CreateMusic("HordePulse", 6.0f, 96f, 0.060f, 0.40f),
                CreateMusic("BossPressure", 5.0f, 58f, 0.075f, 0.68f),
                CreateMusic("ExtractionSignal", 7.0f, 132f, 0.055f, 0.85f)
            };
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

        public void Wave(EcologyProfile ecology, int count)
        {
            float volume = Mathf.Clamp(0.25f + count * 0.06f, 0.25f, 0.52f);
            Play("wave", volume);
        }

        public void SetMusic(string mode, float intensity)
        {
            if (musicSource == null || musicLayers == null || musicLayers.Length == 0)
            {
                return;
            }

            if (!NodnarbSettings.MusicEnabled)
            {
                musicSource.Stop();
                activeMusic = null;
                activeMusicIndex = -1;
                activeMusicIntensity = -1f;
                musicSource.volume = 0f;
                return;
            }

            int index = mode == "boss" ? 2 : mode == "extraction" ? 3 : mode == "combat" ? 1 : 0;
            float safeIntensity = Mathf.Clamp01(intensity);
            float targetVolume = Mathf.Lerp(0.06f, 0.16f, safeIntensity);
            if (activeMusicIndex != index || musicSource.clip != musicLayers[index])
            {
                activeMusic = mode;
                activeMusicIndex = index;
                musicSource.clip = musicLayers[index];
                musicSource.Play();
                activeMusicIntensity = -1f;
            }

            if (!musicSource.isPlaying || Mathf.Abs(activeMusicIntensity - safeIntensity) >= 0.02f
                || Mathf.Abs(musicSource.volume - targetVolume) >= 0.002f)
            {
                musicSource.volume = targetVolume;
                activeMusicIntensity = safeIntensity;
            }
        }

        public string ActiveMusicMode
        {
            get { return activeMusic; }
        }

        public int EffectClipCount
        {
            get { return clips.Count; }
        }

        public int MusicLayerCount
        {
            get { return musicLayers == null ? 0 : musicLayers.Length; }
        }

        private void Play(string key, float volume)
        {
            AudioClip clip;
            if (NodnarbSettings.SoundEnabled && source != null && clips.TryGetValue(key, out clip))
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

        private static AudioClip CreateMusic(string name, float duration, float rootFrequency, float amplitude, float pulse)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(name.GetHashCode());
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)Math.Max(1, sampleCount - 1);
                phase += Mathf.PI * 2f * rootFrequency / sampleRate;
                float slow = Mathf.Sin(t * Mathf.PI * 2f * 2.0f) * 0.5f + 0.5f;
                float low = Mathf.Sin(phase) * 0.62f;
                float fifth = Mathf.Sin(phase * 1.5f + 0.7f) * 0.20f;
                float shimmer = Mathf.Sin(phase * 3.98f + 1.2f) * 0.06f;
                float grit = ((float)random.NextDouble() * 2f - 1f) * 0.012f;
                float envelope = 0.75f + slow * pulse;
                samples[i] = (low + fifth + shimmer + grit) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
