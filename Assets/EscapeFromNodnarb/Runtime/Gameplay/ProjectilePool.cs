using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromNodnarb
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        private sealed class Bolt
        {
            public GameObject GameObject;
            public Transform Transform;
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
            public MaterialPropertyBlock ColorBlock = new MaterialPropertyBlock();
            public Vector3 BaseScale;
            public float Age;
            public bool Active;
        }

        private readonly List<Bolt> bolts = new List<Bolt>(220);
        private readonly List<Pulse> pulses = new List<Pulse>(64);
        private NodnarbGame game;

        public int TotalFeedbackEvents { get; private set; }

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

        public void Initialize(NodnarbGame owner)
        {
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

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
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
                    SpawnFeedback(bolt.Transform.position, bolt.Friendly ? GameTheme.SignalBright : GameTheme.ProjectileHostile,
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
                float scale = (1f - normalized) * (1f + normalized * 0.55f);
                pulse.Transform.localScale = pulse.BaseScale * scale;
                pulse.Transform.Rotate(0f, 0f, 260f * deltaTime, Space.Self);
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

            Release(oldest);
            return oldest;
        }

        private Bolt CreateBolt(bool friendly)
        {
            Color color = friendly ? GameTheme.SignalBright : GameTheme.ProjectileHostile;
            GameObject value = PrimitiveFactory.Cube(friendly ? "SquadBolt" : "AlienBolt", transform, Vector3.zero,
                friendly ? new Vector3(0.065f, 0.065f, 0.34f) : new Vector3(0.15f, 0.15f, 0.28f), color);
            value.SetActive(false);
            return new Bolt
            {
                GameObject = value,
                Transform = value.transform,
                Friendly = friendly
            };
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

            if (pulses.Count < 64)
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
            value.SetActive(false);
            return new Pulse
            {
                GameObject = value,
                Transform = value.transform,
                Renderer = value.GetComponent<Renderer>()
            };
        }

        private void SpawnFeedback(Vector3 position, Color color, float scale)
        {
            Pulse pulse = AcquirePulse();
            pulse.Transform.position = position;
            pulse.Transform.localRotation = Quaternion.identity;
            pulse.BaseScale = Vector3.one * scale;
            pulse.Transform.localScale = pulse.BaseScale;
            pulse.Age = 0f;
            pulse.Active = true;
            if (pulse.Renderer != null)
            {
                pulse.ColorBlock.Clear();
                pulse.ColorBlock.SetColor("_Color", color);
                pulse.Renderer.SetPropertyBlock(pulse.ColorBlock);
            }

            pulse.GameObject.SetActive(true);
            TotalFeedbackEvents++;
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
