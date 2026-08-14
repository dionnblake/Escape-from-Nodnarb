using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromNodnarb
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        private const int WarmProjectileCount = 16;
        private const int MaxFeedbackPoolCount = 64;

        private sealed class Bolt
        {
            public GameObject GameObject;
            public Transform Transform;
            public Renderer Renderer;
            public Renderer[] Renderers;
            public MaterialPropertyBlock ColorBlock = new MaterialPropertyBlock();
            public Vector3 Velocity;
            public float Damage;
            public float Radius;
            public float Age;
            public bool Friendly;
            public bool Active;
        }

        private sealed class Pulse
        {
            public GameObject GameObject;
            public Transform Transform;
            public Renderer Renderer;
            public Renderer AccentRenderer;
            public Renderer[] Renderers;
            public MaterialPropertyBlock ColorBlock = new MaterialPropertyBlock();
            public Vector3 BaseScale;
            public float Age;
            public bool Friendly;
            public Color OverrideColor;
            public bool UsesOverrideColor;
            public bool Active;
        }

        private readonly List<Bolt> bolts = new List<Bolt>(220);
        private readonly List<Pulse> pulses = new List<Pulse>(64);
        private NodnarbGame game;
        private bool initialized;

        public int TotalFeedbackEvents { get; private set; }

        public bool AccessibilityVisualApplied { get; private set; }

        public int TotalProjectileSlots
        {
            get { return bolts.Count; }
        }

        public int ActiveFriendlyCount
        {
            get { return CountActive(true); }
        }

        public int ActiveHostileCount
        {
            get { return CountActive(false); }
        }

        public int ActiveProjectileCount
        {
            get { return ActiveFriendlyCount + ActiveHostileCount; }
        }

        public int ActiveFeedbackCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < pulses.Count; i++)
                {
                    if (pulses[i].Active)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int TotalFeedbackSlots
        {
            get { return pulses.Count; }
        }

        public void Initialize(NodnarbGame owner)
        {
            if (initialized)
            {
                return;
            }

            game = owner;
            // Keep first-frame startup light. Acquire() grows the pool on demand,
            // so a small warm set covers the opening volley without blocking title load.
            for (int i = 0; i < 12; i++)
            {
                bolts.Add(CreateBolt(true));
            }

            for (int i = 0; i < 4; i++)
            {
                bolts.Add(CreateBolt(false));
            }

            initialized = true;
            ApplyAccessibilitySettings();
        }

        public IEnumerator InitializeRoutine(NodnarbGame owner)
        {
            if (initialized)
            {
                yield break;
            }

            game = owner;
            for (int i = 0; i < 12; i++)
            {
                bolts.Add(CreateBolt(true));
                if ((i + 1) % 4 == 0)
                {
                    yield return null;
                }
            }

            for (int i = 0; i < 4; i++)
            {
                bolts.Add(CreateBolt(false));
            }

            initialized = true;
            ApplyAccessibilitySettings();
        }

        public void ApplyAccessibilitySettings()
        {
            for (int i = 0; i < bolts.Count; i++)
            {
                ApplyBoltColor(bolts[i]);
            }

            for (int i = 0; i < pulses.Count; i++)
            {
                ApplyPulseColor(pulses[i]);
            }

            AccessibilityVisualApplied = true;
        }

        public void FireFriendly(Vector3 origin, float damage)
        {
            FireFriendly(origin, origin + Vector3.forward * 18f, damage);
        }

        public void FireFriendly(Vector3 origin, Vector3 target, float damage)
        {
            Bolt bolt = Acquire(true);
            Vector3 direction = (target - origin).sqrMagnitude > 0.001f
                ? (target - origin).normalized
                : Vector3.forward;
            Activate(bolt, origin, direction * 20.5f, damage, 0.20f);
        }

        public void FireHostile(Vector3 origin, Vector3 target, float damage)
        {
            Vector3 direction = (target - origin).normalized;
            Bolt bolt = Acquire(false);
            Activate(bolt, origin, direction * 7.6f, damage, 0.31f);
        }

        public void SpawnBurst(Vector3 position, Color color, float scale)
        {
            Pulse pulse = AcquirePulse();
            pulse.Transform.position = position;
            pulse.Transform.localRotation = Quaternion.identity;
            pulse.BaseScale = Vector3.one * Mathf.Clamp(scale, 0.12f, 1.15f);
            pulse.Transform.localScale = pulse.BaseScale;
            pulse.Age = 0f;
            pulse.Friendly = true;
            pulse.OverrideColor = color;
            pulse.UsesOverrideColor = true;
            pulse.Active = true;
            ApplyPulseColor(pulse);
            pulse.GameObject.SetActive(true);
            TotalFeedbackEvents++;
        }

        public void ClearActive()
        {
            for (int i = 0; i < bolts.Count; i++)
            {
                Release(bolts[i]);
            }

            for (int i = 0; i < pulses.Count; i++)
            {
                Release(pulses[i]);
            }

            TotalFeedbackEvents = 0;
        }

        public void TrimToWarmSet()
        {
            ClearActive();
            while (bolts.Count > WarmProjectileCount)
            {
                int last = bolts.Count - 1;
                Object.Destroy(bolts[last].GameObject);
                bolts.RemoveAt(last);
            }

            while (pulses.Count > 0)
            {
                int last = pulses.Count - 1;
                Object.Destroy(pulses[last].GameObject);
                pulses.RemoveAt(last);
            }
        }

        private void Update()
        {
            if (game == null || !game.CombatActive)
            {
                return;
            }

            float deltaTime = game.SimulationDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }
            for (int i = 0; i < bolts.Count; i++)
            {
                Bolt bolt = bolts[i];
                if (!bolt.Active)
                {
                    continue;
                }

                bolt.Age += deltaTime;
                bolt.Transform.position += bolt.Velocity * deltaTime;
                bool hit = bolt.Friendly
                    ? game.TryHitFriendly(bolt.Transform.position, bolt.Damage, bolt.Radius)
                    : game.TryHitCaptain(bolt.Transform.position, bolt.Damage, bolt.Radius);
                if (hit)
                {
                    SpawnFeedback(bolt.Transform.position, bolt.Friendly,
                        bolt.Friendly ? 0.24f : 0.36f);
                }

                if (hit || bolt.Age > 4.2f || Mathf.Abs(bolt.Transform.position.x) > 8f || bolt.Transform.position.z > 25f || bolt.Transform.position.z < -6f)
                {
                    Release(bolt);
                }
            }

            for (int i = 0; i < pulses.Count; i++)
            {
                Pulse pulse = pulses[i];
                if (!pulse.Active)
                {
                    continue;
                }

                pulse.Age += deltaTime;
                float normalized = Mathf.Clamp01(pulse.Age / 0.20f);
                if (NodnarbSettings.ReducedMotionEnabled)
                {
                    pulse.Transform.localScale = pulse.BaseScale;
                }
                else
                {
                    float scale = (1f - normalized) * (1f + normalized * 0.55f);
                    pulse.Transform.localScale = pulse.BaseScale * scale;
                    pulse.Transform.Rotate(0f, 0f, 260f * deltaTime, Space.Self);
                }
                if (normalized >= 1f)
                {
                    Release(pulse);
                }
            }
        }

        private Bolt Acquire(bool friendly)
        {
            Bolt oldest = null;
            for (int i = 0; i < bolts.Count; i++)
            {
                if (!bolts[i].Active && bolts[i].Friendly == friendly)
                {
                    return bolts[i];
                }

                if (bolts[i].Friendly == friendly && (oldest == null || bolts[i].Age > oldest.Age))
                {
                    oldest = bolts[i];
                }
            }

            if (bolts.Count < 220)
            {
                Bolt created = CreateBolt(friendly);
                bolts.Add(created);
                return created;
            }

            // At the hard pool cap the opposing side may own every slot. Reuse
            // the oldest slot across both teams instead of returning null and
            // crashing the next Activate call.
            oldest = null;
            for (int i = 0; i < bolts.Count; i++)
            {
                if (oldest == null || bolts[i].Age > oldest.Age)
                {
                    oldest = bolts[i];
                }
            }

            if (oldest == null)
            {
                return null;
            }

            Release(oldest);
            oldest.Friendly = friendly;
            ApplyBoltColor(oldest);
            return oldest;
        }

        private Bolt CreateBolt(bool friendly)
        {
            Color color = friendly ? GameTheme.ProjectileFriendly : GameTheme.ProjectileHostile;
            GameObject value = PrimitiveFactory.Sphere(friendly ? "SquadBolt" : "AlienBolt", transform, Vector3.zero,
                friendly ? new Vector3(0.075f, 0.075f, 0.38f) : new Vector3(0.16f, 0.16f, 0.32f), color);
            GameObject tip = PrimitiveFactory.Sphere(friendly ? "SquadBoltTip" : "AlienBoltTip", value.transform,
                Vector3.zero, Vector3.one * 0.72f,
                friendly ? GameTheme.SignalBright : GameTheme.Danger);
            tip.transform.localPosition = Vector3.forward * 0.72f;
            value.SetActive(false);
            Bolt bolt = new Bolt
            {
                GameObject = value,
                Transform = value.transform,
                Renderer = value.GetComponent<Renderer>(),
                Renderers = value.GetComponentsInChildren<Renderer>(true),
                Friendly = friendly
            };
            ApplyBoltColor(bolt);
            return bolt;
        }

        private Pulse AcquirePulse()
        {
            Pulse oldest = null;
            for (int i = 0; i < pulses.Count; i++)
            {
                Pulse pulse = pulses[i];
                if (!pulse.Active)
                {
                    return pulse;
                }

                if (oldest == null || pulse.Age > oldest.Age)
                {
                    oldest = pulse;
                }
            }

            if (pulses.Count < MaxFeedbackPoolCount)
            {
                Pulse created = CreatePulse();
                pulses.Add(created);
                return created;
            }

            Release(oldest);
            return oldest;
        }

        private Pulse CreatePulse()
        {
            GameObject value = PrimitiveFactory.Sphere("CombatFeedback", transform, Vector3.zero, Vector3.one,
                GameTheme.SignalBright);
            GameObject accent = PrimitiveFactory.Cube("CombatFeedbackCross", value.transform, Vector3.zero,
                new Vector3(1.8f, 0.08f, 0.08f), GameTheme.SignalBright);
            accent.transform.localPosition = Vector3.zero;
            accent.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            PrimitiveFactory.Cylinder("CombatFeedbackRing", value.transform, Vector3.zero,
                new Vector3(0.78f, 0.018f, 0.78f), GameTheme.SignalCyan);
            value.SetActive(false);
            return new Pulse
            {
                GameObject = value,
                Transform = value.transform,
                Renderer = value.GetComponent<Renderer>(),
                AccentRenderer = accent.GetComponent<Renderer>(),
                Renderers = value.GetComponentsInChildren<Renderer>(true),
                Friendly = true
            };
        }

        private void SpawnFeedback(Vector3 position, bool friendly, float scale)
        {
            Pulse pulse = AcquirePulse();
            pulse.Transform.position = position;
            pulse.Transform.localRotation = Quaternion.identity;
            pulse.Transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, friendly ? 45f : 0f);
            pulse.BaseScale = Vector3.one * scale;
            pulse.Transform.localScale = pulse.BaseScale;
            pulse.Age = 0f;
            pulse.Friendly = friendly;
            pulse.UsesOverrideColor = false;
            pulse.Active = true;
            ApplyPulseColor(pulse);

            pulse.GameObject.SetActive(true);
            TotalFeedbackEvents++;
        }

        private static Color BoltColor(bool friendly)
        {
            return friendly
                ? GameTheme.AccessibleSignalBright
                : (NodnarbSettings.HighContrastEnabled ? GameTheme.AccessibleDanger : GameTheme.ProjectileHostile);
        }

        private static void ApplyBoltColor(Bolt bolt)
        {
            if (bolt == null || bolt.Renderers == null)
            {
                return;
            }

            for (int index = 0; index < bolt.Renderers.Length; index++)
            {
                Renderer renderer = bolt.Renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                if (NodnarbSettings.HighContrastEnabled)
                {
                    bolt.ColorBlock.Clear();
                    bolt.ColorBlock.SetColor("_Color", BoltColor(bolt.Friendly));
                    renderer.SetPropertyBlock(bolt.ColorBlock);
                }
                else
                {
                    renderer.SetPropertyBlock(null);
                }
            }
        }

        private static void ApplyPulseColor(Pulse pulse)
        {
            if (pulse == null || pulse.Renderers == null)
            {
                return;
            }

            Color color = pulse.UsesOverrideColor
                ? pulse.OverrideColor
                : NodnarbSettings.HighContrastEnabled
                ? BoltColor(pulse.Friendly)
                : (pulse.Friendly ? GameTheme.SignalBright : GameTheme.ProjectileHostile);
            pulse.ColorBlock.Clear();
            pulse.ColorBlock.SetColor("_Color", color);
            for (int index = 0; index < pulse.Renderers.Length; index++)
            {
                Renderer renderer = pulse.Renderers[index];
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(pulse.ColorBlock);
                }
            }
        }

        private int CountActive(bool friendly)
        {
            int count = 0;
            for (int i = 0; i < bolts.Count; i++)
            {
                if (bolts[i].Active && bolts[i].Friendly == friendly)
                {
                    count++;
                }
            }

            return count;
        }

        private static void Activate(Bolt bolt, Vector3 origin, Vector3 velocity, float damage, float radius)
        {
            bolt.Transform.position = origin;
            bolt.Transform.rotation = velocity.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(velocity.normalized, Vector3.up)
                : Quaternion.identity;
            bolt.Velocity = velocity;
            bolt.Damage = damage;
            bolt.Radius = radius;
            bolt.Age = 0f;
            bolt.Active = true;
            bolt.GameObject.SetActive(true);
        }

        private static void Release(Bolt bolt)
        {
            if (bolt == null)
            {
                return;
            }

            bolt.Active = false;
            bolt.GameObject.SetActive(false);
        }

        private static void Release(Pulse pulse)
        {
            if (pulse == null)
            {
                return;
            }

            pulse.Active = false;
            pulse.GameObject.SetActive(false);
        }
    }
}
