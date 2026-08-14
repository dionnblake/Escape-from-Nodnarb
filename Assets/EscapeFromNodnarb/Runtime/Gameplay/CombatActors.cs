using UnityEngine;

namespace EscapeFromNodnarb
{
    public interface IFriendlyDamageable
    {
        Vector3 HitPosition { get; }
        float HitRadius { get; }
        bool IsAlive { get; }
        void ApplyDamage(float damage);
    }

    public enum EnemyKind
    {
        Melee,
        Ranged,
        Armored,
        Boss
    }

    public enum CardKind
    {
        Weapon,
        Recruit
    }

    public enum CardActivationSource
    {
        AutoFire,
        Touch
    }

    public sealed class EnemyAgent : MonoBehaviour, IFriendlyDamageable
    {
        private NodnarbGame game;
        private EnemyKind kind;
        private float health;
        private float speed;
        private float rangedCooldown;
        private float rangedTimer;
        private float contactDamage;
        private int score;
        private int salvage;
        private bool alive;
        private float swayOffset;
        private BossBehavior bossBehavior;
        private bool bossRanged;
        private float bossTracking;
        private float bossSwayAmplitude;
        private float bossSwayFrequency;
        private float bossShotDamage;
        private float laneX;
        private float hitPulse;
        private float healthFeedbackTimer;
        private float visualFlashTimer;
        private bool visualFlashApplied;
        private float maxHealth;
        private GameObject bossTelegraph;
        private Vector3 bossTelegraphBaseScale = Vector3.one;
        private float bossTelegraphTimer;
        private bool bossTelegraphArmed;
        private Transform healthBand;
        private Transform healthFill;
        private Renderer healthFillRenderer;
        private MaterialPropertyBlock healthColorBlock;
        private Vector3 healthFillBaseScale = Vector3.one;
        private Vector3 healthFillBaseLocalPosition;
        private bool visualBuilt;
        private Renderer[] accessibilityRenderers;
        private MaterialPropertyBlock accessibilityColorBlock;
        private Transform rangedMuzzleFlash;
        private float rangedMuzzleFlashTimer;

        public bool AccessibilityVisualApplied { get; private set; }

        public EnemyKind Kind
        {
            get { return kind; }
        }

        public Vector3 HitPosition
        {
            get { return transform.position + Vector3.up * (kind == EnemyKind.Boss ? 1.6f : 0.65f); }
        }

        public float HitRadius
        {
            get { return kind == EnemyKind.Boss ? 1.25f : kind == EnemyKind.Armored ? 0.68f : 0.52f; }
        }

        public bool IsAlive
        {
            get { return alive; }
        }

        public bool IsBoss
        {
            get { return kind == EnemyKind.Boss; }
        }

        public float HealthRatio
        {
            get { return maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth); }
        }

        public float CurrentHealth
        {
            get { return Mathf.Max(0f, health); }
        }

        public float MaxHealth
        {
            get { return maxHealth; }
        }

        public float LaneX
        {
            get { return laneX; }
        }

        public float RangedTimer
        {
            get { return Mathf.Max(0f, rangedTimer); }
        }

        public float Speed
        {
            get { return speed; }
        }

        public BossBehavior BossBehavior
        {
            get { return bossBehavior; }
        }

        public float BossTelegraphTimer
        {
            get { return Mathf.Max(0f, bossTelegraphTimer); }
        }

        public bool BossTelegraphArmed
        {
            get { return bossTelegraphArmed; }
        }

        public bool HasHealthFeedback
        {
            get { return healthBand != null && healthFill != null && healthFillRenderer != null; }
        }

        public Transform HealthFeedback
        {
            get { return healthBand; }
        }

        public void Initialize(NodnarbGame owner, EnemyKind enemyKind, float difficulty, float xPosition)
        {
            Initialize(owner, enemyKind, difficulty, xPosition, BossBehavior.Crusher);
        }

        public void Initialize(NodnarbGame owner, EnemyKind enemyKind, float difficulty, float xPosition, BossBehavior behavior)
        {
            game = owner;
            kind = enemyKind;
            bossBehavior = behavior;
            laneX = xPosition;
            alive = true;
            hitPulse = 0f;
            healthFeedbackTimer = 0f;
            visualFlashTimer = 0f;
            visualFlashApplied = false;
            rangedMuzzleFlashTimer = 0f;
            bossTelegraphTimer = 0f;
            bossTelegraphArmed = false;
            swayOffset = xPosition * 0.73f;
            float spawnZ = GameTheme.SpawnZ + (kind == EnemyKind.Boss ? 1.5f : 0f);
            transform.position = new Vector3(game.RouteWorldX(laneX, spawnZ), 0f, spawnZ);

            switch (kind)
            {
                case EnemyKind.Ranged:
                    health = 4.5f + difficulty * 4f;
                    speed = 1.05f + difficulty * 0.42f;
                    rangedCooldown = Mathf.Lerp(2.65f, 1.45f, difficulty);
                    contactDamage = 15f;
                    score = 28;
                    salvage = 2;
                    break;
                case EnemyKind.Armored:
                    health = 10f + difficulty * 9f;
                    speed = 0.82f + difficulty * 0.35f;
                    contactDamage = 24f;
                    score = 42;
                    salvage = 3;
                    break;
                case EnemyKind.Boss:
                    ConfigureBoss(difficulty);
                    break;
                default:
                    health = 3.2f + difficulty * 3.8f;
                    speed = 1.55f + difficulty * 0.72f;
                    contactDamage = 18f;
                    score = 18;
                    salvage = 1;
                    break;
            }

            maxHealth = health;
            if (!visualBuilt)
            {
                BuildVisual();
                visualBuilt = true;
            }

            ResetVisualState();
            gameObject.SetActive(true);
            game.RegisterEnemy(this);
        }

        public void RestoreCheckpoint(PausedEnemyData checkpoint)
        {
            if (!alive || game == null || checkpoint == null)
            {
                return;
            }

            float positionX = checkpoint.ExactState ? checkpoint.PositionX : game.RouteWorldX(laneX, checkpoint.PositionZ);
            transform.position = new Vector3(positionX, 0f, checkpoint.PositionZ);
            maxHealth = checkpoint.ExactState ? checkpoint.MaxHealth : maxHealth;
            health = checkpoint.ExactState ? checkpoint.Health : maxHealth * Mathf.Clamp01(checkpoint.HealthRatio);
            speed = checkpoint.ExactState ? checkpoint.Speed : speed;
            rangedTimer = Mathf.Max(0f, checkpoint.RangedTimer);
            bossTelegraphTimer = Mathf.Max(0f, checkpoint.BossTelegraphTimer);
            bossTelegraphArmed = checkpoint.BossTelegraphArmed;
            RefreshHealthFeedback();
        }

        private void ConfigureBoss(float difficulty)
        {
            switch (bossBehavior)
            {
                case BossBehavior.Crusher:
                    health = 135f + difficulty * 85f;
                    speed = 0.42f + difficulty * 0.08f;
                    rangedCooldown = 0f;
                    contactDamage = 55f;
                    bossRanged = false;
                    bossTracking = 0.58f;
                    bossSwayAmplitude = 0.10f;
                    bossSwayFrequency = 0.8f;
                    bossShotDamage = 0f;
                    break;
                case BossBehavior.Striker:
                    health = 88f + difficulty * 56f;
                    speed = 0.86f + difficulty * 0.18f;
                    rangedCooldown = Mathf.Lerp(2.05f, 1.25f, difficulty);
                    contactDamage = 32f;
                    bossRanged = true;
                    bossTracking = 1.02f;
                    bossSwayAmplitude = 0.58f;
                    bossSwayFrequency = 2.1f;
                    bossShotDamage = 12f;
                    break;
                case BossBehavior.Barrager:
                    health = 112f + difficulty * 72f;
                    speed = 0.46f + difficulty * 0.10f;
                    rangedCooldown = Mathf.Lerp(0.86f, 0.56f, difficulty);
                    contactDamage = 36f;
                    bossRanged = true;
                    bossTracking = 0.18f;
                    bossSwayAmplitude = 0.16f;
                    bossSwayFrequency = 0.9f;
                    bossShotDamage = 10f;
                    break;
                case BossBehavior.Sentry:
                    health = 155f + difficulty * 92f;
                    speed = 0.34f + difficulty * 0.06f;
                    rangedCooldown = Mathf.Lerp(1.24f, 0.78f, difficulty);
                    contactDamage = 46f;
                    bossRanged = true;
                    bossTracking = 0.10f;
                    bossSwayAmplitude = 0.12f;
                    bossSwayFrequency = 0.7f;
                    bossShotDamage = 14f;
                    break;
                case BossBehavior.Howler:
                    health = 106f + difficulty * 68f;
                    speed = 0.58f + difficulty * 0.12f;
                    rangedCooldown = Mathf.Lerp(1.14f, 0.76f, difficulty);
                    contactDamage = 38f;
                    bossRanged = true;
                    bossTracking = 0.46f;
                    bossSwayAmplitude = 1.02f;
                    bossSwayFrequency = 1.3f;
                    bossShotDamage = 10f;
                    break;
                default:
                    health = 105f + difficulty * 70f;
                    speed = 0.55f + difficulty * 0.13f;
                    rangedCooldown = Mathf.Lerp(1.9f, 1.08f, difficulty);
                    contactDamage = 40f;
                    bossRanged = true;
                    bossTracking = 0.72f;
                    bossSwayAmplitude = 0.34f;
                    bossSwayFrequency = 1.2f;
                    bossShotDamage = 15f;
                    break;
            }

            score = 500;
            salvage = 25;
        }

        public void ApplyDamage(float damage)
        {
            if (!alive || damage <= 0f)
            {
                return;
            }

            health -= damage;
            healthFeedbackTimer = 1.25f;
            visualFlashTimer = NodnarbSettings.ReducedMotionEnabled ? 0f : 0.10f;
            if (healthBand != null)
            {
                healthBand.gameObject.SetActive(true);
            }
            hitPulse = Mathf.Max(hitPulse, kind == EnemyKind.Boss ? 0.13f : 0.18f);
            RefreshHealthFeedback();
            if (health <= 0f)
            {
                Die();
            }
        }

        public void Cancel()
        {
            if (!alive)
            {
                return;
            }

            alive = false;
            game.UnregisterEnemy(this);
            game.ReleaseEnemy(this);
        }

        private void Update()
        {
            if (!alive || game == null || !game.CombatActive)
            {
                return;
            }

            float deltaTime = game.SimulationDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }
            hitPulse = NodnarbSettings.ReducedMotionEnabled
                ? 0f
                : Mathf.MoveTowards(hitPulse, 0f, deltaTime * 5.8f);
            visualFlashTimer = Mathf.Max(0f, visualFlashTimer - deltaTime);
            if (kind != EnemyKind.Boss && healthBand != null && healthFeedbackTimer > 0f)
            {
                healthFeedbackTimer = Mathf.Max(0f, healthFeedbackTimer - deltaTime);
                if (healthFeedbackTimer <= 0f)
                {
                    healthBand.gameObject.SetActive(false);
                }
            }
            transform.localScale = NodnarbSettings.ReducedMotionEnabled
                ? Vector3.one
                : Vector3.one * (1f + hitPulse);
            ApplyVisualFlash();
            UpdateRangedMuzzleFlash(deltaTime);
            Vector3 position = transform.position;
            float routeX = game.RouteWorldX(laneX, position.z);
            float targetX = Mathf.Lerp(routeX, game.SquadX, kind == EnemyKind.Boss ? 0.72f : 0.38f);
            float tracking = kind == EnemyKind.Boss ? bossTracking : kind == EnemyKind.Melee ? 0.44f : 0.22f;
            position.x = Mathf.MoveTowards(position.x, targetX, tracking * deltaTime);
            if (kind == EnemyKind.Boss && !NodnarbSettings.ReducedMotionEnabled)
            {
                position.x += Mathf.Sin(Time.time * bossSwayFrequency + swayOffset) * bossSwayAmplitude * deltaTime;
            }

            UpdateBossTelegraph(deltaTime);

            position.z -= speed * deltaTime;
            transform.position = position;

            if ((kind == EnemyKind.Ranged || (kind == EnemyKind.Boss && bossRanged)) && position.z < 16f)
            {
                rangedTimer -= deltaTime;
                if (kind == EnemyKind.Boss && bossRanged && rangedTimer > 0f && rangedTimer <= 0.36f && !bossTelegraphArmed)
                {
                    bossTelegraphArmed = true;
                    bossTelegraphTimer = 0.36f;
                    game.NotifyBossTelegraph();
                }

                if (rangedTimer <= 0f)
                {
                    rangedTimer = rangedCooldown;
                    bossTelegraphArmed = false;
                    bossTelegraphTimer = 0f;
                    float damage = kind == EnemyKind.Boss ? bossShotDamage : 9f;
                    PulseRangedMuzzleFlash();
                    game.FireHostile(HitPosition, game.CaptainPosition + Vector3.up * 0.55f, damage);
                }
            }

            if (position.z <= GameTheme.FrontLineZ)
            {
                alive = false;
                game.UnregisterEnemy(this);
                game.OnFrontLineBreached(contactDamage);
                game.ReleaseEnemy(this);
            }
        }

        private void Die()
        {
            alive = false;
            game.UnregisterEnemy(this);
            game.OnEnemyKilled(this, score, salvage);
            game.ReleaseEnemy(this);
        }

        private void ApplyVisualFlash()
        {
            if (accessibilityRenderers == null)
            {
                return;
            }

            bool flash = visualFlashTimer > 0f && !NodnarbSettings.ReducedMotionEnabled;
            if (!flash)
            {
                if (visualFlashApplied)
                {
                    ApplyAccessibilitySettings();
                }
                return;
            }

            if (visualFlashApplied)
            {
                return;
            }

            Color flashColor = kind == EnemyKind.Boss ? GameTheme.AccessibleDanger : GameTheme.AccessibleSignalBright;
            for (int index = 0; index < accessibilityRenderers.Length; index++)
            {
                Renderer renderer = accessibilityRenderers[index];
                if (!ShouldApplyAccessibilityColor(renderer))
                {
                    continue;
                }

                MaterialPropertyBlock block = accessibilityColorBlock ?? new MaterialPropertyBlock();
                block.Clear();
                block.SetColor("_Color", Color.Lerp(Color.white, flashColor, 0.42f));
                renderer.SetPropertyBlock(block);
            }

            visualFlashApplied = true;
        }

        private void OnDestroy()
        {
            if (alive && game != null)
            {
                alive = false;
                game.UnregisterEnemy(this);
            }
        }

        private void BuildMelee()
        {
            if (TryBuildImportedVisual("Rusher"))
            {
                return;
            }

            Transform root = transform;
            PrimitiveFactory.Sphere("RusherCarapace", root, transform.position + Vector3.up * 0.57f,
                new Vector3(0.72f, 0.48f, 0.86f), GameTheme.AlienViolet);
            PrimitiveFactory.Sphere("RusherHead", root, transform.position + new Vector3(0f, 0.64f, -0.63f),
                new Vector3(0.48f, 0.34f, 0.48f), Color.Lerp(GameTheme.AlienViolet, GameTheme.AlienGlow, 0.18f));
            PrimitiveFactory.Cube("RusherJaw", root, transform.position + new Vector3(0f, 0.47f, -0.93f),
                new Vector3(0.50f, 0.13f, 0.28f), GameTheme.Weapon);
            for (int leg = 0; leg < 6; leg++)
            {
                float side = leg % 2 == 0 ? -1f : 1f;
                float row = leg / 2;
                GameObject limb = PrimitiveFactory.Cube("RusherLeg", root,
                    transform.position + new Vector3(side * (0.58f + row * 0.08f), 0.28f, -0.38f + row * 0.42f),
                    new Vector3(0.48f, 0.12f, 0.18f), GameTheme.AlienViolet);
                limb.transform.rotation = Quaternion.Euler(0f, side * (28f + row * 10f), side * 16f);
            }

            PrimitiveFactory.Sphere("RusherEye", root, transform.position + new Vector3(0f, 0.76f, -0.93f),
                new Vector3(0.12f, 0.12f, 0.08f), GameTheme.AlienGlow);
        }

        private void BuildVisual()
        {
            BuildGroundShadow();
            switch (kind)
            {
                case EnemyKind.Ranged:
                    BuildRanged();
                    break;
                case EnemyKind.Armored:
                    BuildArmored();
                    break;
                case EnemyKind.Boss:
                    BuildBoss();
                    break;
                default:
                    BuildMelee();
                    break;
            }

            // Imported meshes provide the base silhouette. These authored
            // runtime accents give each enemy a readable face, role marker,
            // and color signal without replacing the licensed-safe source
            // geometry or changing the actor root contract.
            BuildEnemyAccents();

            if (kind == EnemyKind.Ranged || kind == EnemyKind.Boss)
            {
                BuildRangedMuzzleFlash();
            }

            BuildHealthFeedback();
            accessibilityRenderers = GetComponentsInChildren<Renderer>(true);
            accessibilityColorBlock = new MaterialPropertyBlock();
            ApplyAccessibilitySettings();
        }

        private void BuildEnemyAccents()
        {
            GameObject accentObject = new GameObject("EnemyAccentRig");
            Transform root = accentObject.transform;
            root.SetParent(transform, false);

            switch (kind)
            {
                case EnemyKind.Ranged:
                    PrimitiveFactory.Sphere("SpitterVisorAccent", root,
                        transform.position + new Vector3(0f, 0.92f, -0.72f),
                        new Vector3(0.27f, 0.16f, 0.09f), GameTheme.AlienGlow);
                    PrimitiveFactory.Cylinder("SpitterNeedleAccent", root,
                        transform.position + new Vector3(0f, 0.62f, -0.73f),
                        new Vector3(0.08f, 0.26f, 0.08f), GameTheme.SignalCyan);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        PrimitiveFactory.Cube("SpitterFinAccent", root,
                            transform.position + new Vector3(side * 0.58f, 0.95f, -0.10f),
                            new Vector3(0.14f, 0.28f, 0.22f), GameTheme.AlienRanged)
                            .transform.rotation = Quaternion.Euler(0f, 0f, side * 34f);
                    }
                    break;
                case EnemyKind.Armored:
                    PrimitiveFactory.Cube("BlockerShieldInsetAccent", root,
                        transform.position + new Vector3(0f, 0.70f, -0.78f),
                        new Vector3(0.58f, 0.48f, 0.07f), GameTheme.SurfaceGlassDeep);
                    PrimitiveFactory.Cube("BlockerShieldCoreAccent", root,
                        transform.position + new Vector3(0f, 0.70f, -0.83f),
                        new Vector3(0.13f, 0.34f, 0.08f), GameTheme.SignalCyan);
                    PrimitiveFactory.Cube("BlockerVisorFrameAccent", root,
                        transform.position + new Vector3(0f, 1.08f, -0.78f),
                        new Vector3(0.58f, 0.18f, 0.06f), GameTheme.SurfaceGlassDeep);
                    PrimitiveFactory.Cube("BlockerVisorAccent", root,
                        transform.position + new Vector3(0f, 1.08f, -0.83f),
                        new Vector3(0.40f, 0.07f, 0.07f), GameTheme.SignalCyan);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        PrimitiveFactory.Cube("BlockerShoulderAccent", root,
                            transform.position + new Vector3(side * 0.60f, 0.96f, -0.20f),
                            new Vector3(0.18f, 0.30f, 0.34f), GameTheme.AlienArmored)
                            .transform.rotation = Quaternion.Euler(0f, 0f, side * 18f);
                    }
                    PrimitiveFactory.Capsule("BlockerCrestAccent", root,
                        transform.position + new Vector3(0f, 1.35f, -0.06f),
                        new Vector3(0.20f, 0.34f, 0.16f), GameTheme.AlienArmored);
                    break;
                case EnemyKind.Boss:
                    PrimitiveFactory.Sphere("CarrierCoreAccent", root,
                        transform.position + new Vector3(0f, 1.56f, -1.14f),
                        new Vector3(0.42f, 0.42f, 0.12f), GameTheme.AlienGlow);
                    PrimitiveFactory.Cylinder("CarrierSignalAccent", root,
                        transform.position + new Vector3(0f, 2.31f, -0.26f),
                        new Vector3(0.12f, 0.28f, 0.12f), GameTheme.SignalCyan);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        PrimitiveFactory.Cube("CarrierHornAccent", root,
                            transform.position + new Vector3(side * 0.78f, 2.20f, -0.22f),
                            new Vector3(0.16f, 0.48f, 0.22f), GameTheme.AlienViolet)
                            .transform.rotation = Quaternion.Euler(0f, 0f, side * 28f);
                    }
                    break;
                default:
                    PrimitiveFactory.Sphere("RusherEyeAccent", root,
                        transform.position + new Vector3(0f, 0.80f, -0.94f),
                        new Vector3(0.18f, 0.12f, 0.08f), GameTheme.AlienGlow);
                    PrimitiveFactory.Capsule("RusherJawAccent", root,
                        transform.position + new Vector3(0f, 0.48f, -0.88f),
                        new Vector3(0.13f, 0.22f, 0.10f), GameTheme.Weapon);
                    break;
            }
        }

        private void ResetVisualState()
        {
            transform.localScale = Vector3.one;
            if (healthBand != null)
            {
                healthBand.gameObject.SetActive(kind == EnemyKind.Boss);
            }

            if (bossTelegraph != null)
            {
                bossTelegraph.SetActive(false);
                bossTelegraph.transform.localScale = bossTelegraphBaseScale;
            }

            if (healthFill != null)
            {
                healthFill.localScale = healthFillBaseScale;
                healthFill.localPosition = healthFillBaseLocalPosition;
            }

            // Ranged enemies are pooled. Reset the flash object itself as well
            // as its timer so a recycled enemy cannot inherit a stuck muzzle
            // flash from the previous encounter.
            rangedMuzzleFlashTimer = 0f;
            if (rangedMuzzleFlash != null)
            {
                rangedMuzzleFlash.localScale = Vector3.one;
                rangedMuzzleFlash.gameObject.SetActive(false);
            }

            RefreshHealthFeedback();
            ApplyAccessibilitySettings();
        }

        public void ApplyAccessibilitySettings()
        {
            visualFlashApplied = false;
            bool highContrast = NodnarbSettings.HighContrastEnabled;
            Color accessibleColor = kind == EnemyKind.Boss
                ? GameTheme.AccessibleDanger
                : GameTheme.AccessibleSignalBright;

            if (accessibilityRenderers != null)
            {
                for (int index = 0; index < accessibilityRenderers.Length; index++)
                {
                    Renderer renderer = accessibilityRenderers[index];
                    if (!ShouldApplyAccessibilityColor(renderer))
                    {
                        continue;
                    }

                    if (highContrast)
                    {
                        accessibilityColorBlock.Clear();
                        accessibilityColorBlock.SetColor("_Color", accessibleColor);
                        renderer.SetPropertyBlock(accessibilityColorBlock);
                    }
                    else
                    {
                        renderer.SetPropertyBlock(null);
                    }
                }
            }

            AccessibilityVisualApplied = highContrast;
            RefreshHealthFeedback();
        }

        private bool ShouldApplyAccessibilityColor(Renderer renderer)
        {
            if (renderer == null || renderer == healthFillRenderer)
            {
                return false;
            }

            if (healthBand != null && renderer.transform.IsChildOf(healthBand))
            {
                return false;
            }

            string rendererName = renderer.gameObject.name;
            return rendererName != "EnemyShadow" && rendererName != "BossTelegraph";
        }

        private void BuildRanged()
        {
            if (TryBuildImportedVisual("Spitter"))
            {
                return;
            }

            Transform root = transform;
            PrimitiveFactory.Sphere("SpitterCore", root, transform.position + Vector3.up * 0.78f,
                new Vector3(0.74f, 0.64f, 0.74f), GameTheme.AlienRanged);
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveFactory.Cube("SpitterArm", root, transform.position + new Vector3(side * 0.72f, 0.86f, -0.02f),
                    new Vector3(0.82f, 0.12f, 0.16f), GameTheme.AlienRanged).transform.rotation = Quaternion.Euler(0f, 0f, side * 42f);
                PrimitiveFactory.Cube("SpitterClaw", root, transform.position + new Vector3(side * 1.03f, 0.34f, -0.05f),
                    new Vector3(0.16f, 0.62f, 0.18f), Color.Lerp(GameTheme.AlienRanged, GameTheme.AlienGlow, 0.20f))
                    .transform.rotation = Quaternion.Euler(0f, 0f, side * 16f);
            }

            PrimitiveFactory.Sphere("SpitterEye", root, transform.position + new Vector3(0f, 0.80f, -0.63f),
                new Vector3(0.32f, 0.32f, 0.18f), GameTheme.AlienGlow);
        }

        private void BuildRangedMuzzleFlash()
        {
            float height = kind == EnemyKind.Boss ? 1.92f : 1.04f;
            GameObject flash = PrimitiveFactory.Sphere("RangedMuzzleFlash", transform,
                transform.position + new Vector3(0f, height, -0.56f),
                new Vector3(0.18f, 0.18f, 0.34f), GameTheme.ProjectileHostile);
            rangedMuzzleFlash = flash.transform;
            rangedMuzzleFlash.gameObject.SetActive(false);
        }

        private void PulseRangedMuzzleFlash()
        {
            if (rangedMuzzleFlash == null)
            {
                return;
            }

            rangedMuzzleFlashTimer = 0.10f;
            rangedMuzzleFlash.gameObject.SetActive(true);
        }

        private void UpdateRangedMuzzleFlash(float deltaTime)
        {
            if (rangedMuzzleFlash == null || rangedMuzzleFlashTimer <= 0f)
            {
                return;
            }

            rangedMuzzleFlashTimer = Mathf.Max(0f, rangedMuzzleFlashTimer - deltaTime);
            float normalized = Mathf.Clamp01(rangedMuzzleFlashTimer / 0.10f);
            float scale = NodnarbSettings.ReducedMotionEnabled ? 1f : 0.72f + normalized * 0.46f;
            rangedMuzzleFlash.localScale = Vector3.one * scale;
            rangedMuzzleFlash.gameObject.SetActive(rangedMuzzleFlashTimer > 0f);
        }

        private void BuildArmored()
        {
            if (TryBuildImportedVisual("Blocker"))
            {
                return;
            }

            Transform root = transform;
            PrimitiveFactory.Capsule("BlockerBody", root, transform.position + Vector3.up * 0.68f,
                new Vector3(0.82f, 0.62f, 0.78f), GameTheme.AlienArmored);
            PrimitiveFactory.Cube("BlockerShield", root, transform.position + new Vector3(0f, 0.66f, -0.48f),
                new Vector3(1.34f, 1.28f, 0.24f), Color.Lerp(GameTheme.AlienArmored, GameTheme.Weapon, 0.22f));
            PrimitiveFactory.Cube("ShieldMark", root, transform.position + new Vector3(0f, 0.66f, -0.62f),
                new Vector3(0.15f, 0.72f, 0.08f), GameTheme.CanyonHighlight);
            PrimitiveFactory.Cube("ShieldCross", root, transform.position + new Vector3(0f, 0.66f, -0.63f),
                new Vector3(0.70f, 0.15f, 0.08f), GameTheme.Rule);
        }

        private void BuildBoss()
        {
            if (TryBuildImportedVisual("Carrier"))
            {
                BuildBossTelegraph();
                return;
            }

            Transform root = transform;
            PrimitiveFactory.Capsule("CarrierBody", root, transform.position + Vector3.up * 1.45f,
                new Vector3(1.72f, 1.32f, 1.82f), GameTheme.AlienViolet);
            PrimitiveFactory.Cube("CarrierPlate", root, transform.position + new Vector3(0f, 1.55f, -0.94f),
                new Vector3(2.55f, 1.42f, 0.32f), GameTheme.AlienArmored);
            PrimitiveFactory.Sphere("CarrierCore", root, transform.position + new Vector3(0f, 1.55f, -1.13f),
                new Vector3(0.62f, 0.62f, 0.24f), GameTheme.AlienGlow);
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveFactory.Cube("CarrierClaw", root, transform.position + new Vector3(side * 1.35f, 0.72f, -0.32f),
                    new Vector3(0.42f, 1.45f, 0.42f), GameTheme.AlienViolet).transform.rotation = Quaternion.Euler(18f, 0f, side * 22f);
            }

            for (int plate = -1; plate <= 1; plate++)
            {
                PrimitiveFactory.Cube("CarrierDorsalPlate", root, transform.position + new Vector3(plate * 0.82f, 2.28f, 0.14f),
                    new Vector3(0.66f, 0.26f, 1.08f), Color.Lerp(GameTheme.AlienArmored, GameTheme.AlienViolet, 0.16f));
            }

            BuildBossTelegraph();
        }

        private void BuildBossTelegraph()
        {
            bossTelegraph = PrimitiveFactory.Cylinder("BossTelegraph", transform,
                transform.position + Vector3.up * 0.045f, new Vector3(2.18f, 0.018f, 2.18f), GameTheme.ProjectileHostile);
            bossTelegraphBaseScale = bossTelegraph.transform.localScale;
            bossTelegraph.SetActive(false);
        }

        private void BuildHealthFeedback()
        {
            healthColorBlock = new MaterialPropertyBlock();
            float width = kind == EnemyKind.Boss ? 2.30f : kind == EnemyKind.Armored ? 1.28f : 1.02f;
            float height = kind == EnemyKind.Boss ? 3.42f : kind == EnemyKind.Armored ? 1.84f : 1.46f;
            healthBand = new GameObject("HealthBand").transform;
            healthBand.transform.SetParent(transform, false);
            healthBand.transform.position = transform.position + Vector3.up * height;
            healthBand.gameObject.SetActive(kind == EnemyKind.Boss);

            GameObject back = PrimitiveFactory.Cube("HealthBack", healthBand,
                healthBand.position, new Vector3(width, 0.10f, 0.06f), GameTheme.Void);
            RemoveCollider(back);
            GameObject fill = PrimitiveFactory.Cube("HealthFill", healthBand,
                healthBand.position + Vector3.back * 0.04f, new Vector3(width, 0.07f, 0.07f), GameTheme.SignalBright);
            RemoveCollider(fill);
            healthFill = fill.transform;
            healthFillRenderer = fill.GetComponent<Renderer>();
            healthFillBaseScale = healthFill.localScale;
            healthFillBaseLocalPosition = healthFill.localPosition;
            RefreshHealthFeedback();
        }

        private void RefreshHealthFeedback()
        {
            if (!HasHealthFeedback)
            {
                return;
            }

            float ratio = HealthRatio;
            healthFill.localScale = new Vector3(healthFillBaseScale.x * ratio, healthFillBaseScale.y, healthFillBaseScale.z);
            healthFill.localPosition = new Vector3(-(healthFillBaseScale.x - healthFill.localScale.x) * 0.5f, 0f, -0.04f);
            Color fillColor = kind == EnemyKind.Boss ? GameTheme.AccessibleWeapon : GameTheme.AccessibleSignalBright;
            if (ratio < 0.34f)
            {
                fillColor = GameTheme.AccessibleDanger;
            }
            healthColorBlock.Clear();
            healthColorBlock.SetColor("_Color", fillColor);
            healthFillRenderer.SetPropertyBlock(healthColorBlock);
        }

        private static void RemoveCollider(GameObject value)
        {
            Collider collider = value.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        private void UpdateBossTelegraph(float deltaTime)
        {
            if (bossTelegraph == null)
            {
                return;
            }

            if (bossTelegraphTimer <= 0f)
            {
                if (bossTelegraph.activeSelf)
                {
                    bossTelegraph.SetActive(false);
                }

                return;
            }

            bossTelegraphTimer = Mathf.Max(0f, bossTelegraphTimer - deltaTime);
            bossTelegraph.SetActive(true);
            float pulse = NodnarbSettings.ReducedMotionEnabled
                ? 1f
                : 0.90f + Mathf.Sin(Time.time * 24f) * 0.12f;
            bossTelegraph.transform.localScale = bossTelegraphBaseScale * pulse;
        }

        private void BuildGroundShadow()
        {
            float size = kind == EnemyKind.Boss ? 2.6f : kind == EnemyKind.Armored ? 1.2f : 0.92f;
            PrimitiveFactory.Sphere("EnemyShadow", transform, transform.position + new Vector3(0f, 0.025f, 0f),
                new Vector3(size, 0.035f, size * 0.72f), Color.Lerp(GameTheme.Void, GameTheme.Rule, 0.34f));
        }

        private bool TryBuildImportedVisual(string resourceName)
        {
            GameObject prefab = Resources.Load<GameObject>("Enemies/" + resourceName);
            if (prefab == null)
            {
                return false;
            }

            GameObject visual = Instantiate(prefab, transform);
            visual.name = resourceName + "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale *= ImportedScale(resourceName);
            MeshFilter[] meshFilters = visual.GetComponentsInChildren<MeshFilter>(true);
            for (int meshIndex = 0; meshIndex < meshFilters.Length; meshIndex++)
            {
                Mesh mesh = meshFilters[meshIndex].sharedMesh;
                if (mesh != null && mesh.isReadable)
                {
                    mesh.RecalculateBounds();
                }
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(transform.position, Vector3.zero);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (index > 0)
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                Material[] sourceMaterials = renderer.sharedMaterials;
                Material[] runtimeMaterials = new Material[sourceMaterials.Length];
                for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
                {
                    Material source = sourceMaterials[materialIndex];
                    if (source != null)
                    {
                        // Keep the imported material's authored texture, emission, and
                        // surface response. Only fill genuinely missing material slots.
                        runtimeMaterials[materialIndex] = source;
                        continue;
                    }

                    runtimeMaterials[materialIndex] = PrimitiveFactory.Material(
                        ImportedColor(resourceName, string.Empty, ImportedBaseColor(resourceName)));
                }

                if (runtimeMaterials.Length > 0)
                {
                    renderer.sharedMaterials = runtimeMaterials;
                }
                else
                {
                    renderer.sharedMaterial = PrimitiveFactory.Material(ImportedBaseColor(resourceName));
                }
            }

            if (renderers.Length > 0)
            {
                visual.transform.localPosition = Vector3.up * Mathf.Max(ImportedLift(resourceName), -bounds.min.y + 0.02f);
            }

            bool readableGeometry = bounds.size.x > 0.20f && bounds.size.y > 0.20f && bounds.size.z > 0.20f;
            if (!readableGeometry)
            {
                Destroy(visual);
                return false;
            }

            return true;
        }

        private static float ImportedScale(string resourceName)
        {
            switch (resourceName)
            {
                case "Carrier": return 0.64f;
                case "CrewSoldier": return 0.82f;
                case "Rusher": return 0.58f;
                case "Spitter": return 0.62f;
                case "Blocker": return 0.66f;
                default: return 1f;
            }
        }

        private static float ImportedLift(string resourceName)
        {
            switch (resourceName)
            {
                case "Carrier": return 1.02f;
                case "CrewSoldier": return 0.74f;
                case "Rusher": return 0.60f;
                case "Spitter": return 0.42f;
                case "Blocker": return 0.54f;
                default: return 0.02f;
            }
        }

        private static Color ImportedBaseColor(string resourceName)
        {
            switch (resourceName)
            {
                case "CrewSoldier": return GameTheme.Weapon;
                case "Spitter": return GameTheme.AlienRanged;
                case "Blocker": return GameTheme.AlienArmored;
                default: return GameTheme.AlienViolet;
            }
        }

        private static Color ImportedColor(string resourceName, string materialName, Color fallback)
        {
            string name = materialName.ToLowerInvariant();
            if (resourceName == "CrewSoldier")
            {
                if (name.Contains("armorwhite")) return GameTheme.Weapon;
                if (name.Contains("rescuegreen")) return GameTheme.SignalBright;
                if (name.Contains("gunmetal")) return GameTheme.Rule;
                if (name.Contains("undersuit")) return GameTheme.SurfaceRaised;
                if (name.Contains("skin")) return GameTheme.CaptainOrange;
                return ImportedBaseColor(resourceName);
            }

            if (name.Contains("coreglow")) return GameTheme.AlienGlow;
            if (name.Contains("rangedrose")) return GameTheme.AlienGlow;
            if (name.Contains("rangedpink")) return GameTheme.AlienRanged;
            if (name.Contains("violetlight")) return GameTheme.AlienGlow;
            if (name.Contains("violetdark")) return Color.Lerp(GameTheme.AlienViolet, GameTheme.Void, 0.58f);
            if (name.Contains("violet")) return GameTheme.AlienViolet;
            if (name.Contains("armored")) return GameTheme.AlienArmored;
            if (name.Contains("slate")) return Color.Lerp(GameTheme.AlienArmored, GameTheme.Text, 0.22f);
            if (name.Contains("trim")) return GameTheme.CanyonHighlight;
            if (name.Contains("deepjoint")) return GameTheme.Void;
            if (resourceName == "Spitter") return GameTheme.AlienRanged;
            if (resourceName == "Blocker") return GameTheme.AlienArmored;
            if (resourceName == "Carrier" || resourceName == "Rusher") return GameTheme.AlienViolet;
            return fallback;
        }
    }

    public sealed class TargetCard : MonoBehaviour, IFriendlyDamageable
    {
        private const float VisualGeometryScale = 0.82f;
        private NodnarbGame game;
        private CardKind kind;
        private float health;
        private float maxHealth;
        private float speed;
        private bool alive;
        private Transform panel;
        private Transform signalGlow;
        private Transform visualRoot;
        private Vector3 panelBaseScale = Vector3.one;
        private Vector3 signalGlowBaseScale = Vector3.one;
        private float phase;
        private float lane;
        private CardPathPattern pathPattern;
        private Color accentColor;
        private float visualFlashTimer;
        private bool visualFlashApplied;
        private bool visualBuilt;
        private Renderer[] accessibilityRenderers;
        private MaterialPropertyBlock accessibilityColorBlock;
        private TextMesh label;

        public bool AccessibilityVisualApplied { get; private set; }

        public Color AccentColor
        {
            get { return accentColor; }
        }

        public string DisplayLabel
        {
            get { return kind == CardKind.Recruit ? "+1\nCREW" : "WEAPON\n+1"; }
        }

        public int PairId { get; private set; }

        public CardKind Kind
        {
            get { return kind; }
        }

        public Vector3 HitPosition
        {
            get { return transform.position + Vector3.up * 0.9f; }
        }

        public float HitRadius
        {
            get { return 0.82f; }
        }

        public bool IsAlive
        {
            get { return alive; }
        }

        public float HealthRatio
        {
            get { return maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth); }
        }

        public float CurrentHealth
        {
            get { return Mathf.Max(0f, health); }
        }

        public float MaxHealth
        {
            get { return maxHealth; }
        }

        public float PositionZ
        {
            get { return transform.position.z; }
        }

        public float PositionX
        {
            get { return transform.position.x; }
        }

        public float Speed
        {
            get { return speed; }
        }

        public float VisualScale
        {
            get { return visualRoot == null ? 1f : visualRoot.localScale.x; }
        }

        public bool IsScreenTarget(Camera camera, Vector2 screenPosition, float paddingPixels)
        {
            if (!alive || camera == null)
            {
                return false;
            }

            Vector3 projected = camera.WorldToScreenPoint(HitPosition);
            if (projected.z <= 0f)
            {
                return false;
            }

            Vector3 projectedRight = camera.WorldToScreenPoint(HitPosition + Vector3.right * HitRadius);
            Vector3 projectedUp = camera.WorldToScreenPoint(HitPosition + Vector3.up * HitRadius);
            float horizontalRadius = Mathf.Abs(projectedRight.x - projected.x);
            float verticalRadius = Mathf.Abs(projectedUp.y - projected.y);
            float radius = Mathf.Max(horizontalRadius, verticalRadius) + Mathf.Max(0f, paddingPixels);
            Vector2 projectedPosition = new Vector2(projected.x, projected.y);
            return (screenPosition - projectedPosition).sqrMagnitude <= radius * radius;
        }

        public bool SelectByPlayer()
        {
            if (!alive || game == null || !game.CombatActive
                || !CardChoice.IsSideAligned(kind, game.SquadRelativeX))
            {
                return false;
            }

            game.OnCardActivated(this, CardActivationSource.Touch);
            return true;
        }

        public void Initialize(NodnarbGame owner, int pairId, CardKind cardKind, float difficulty)
        {
            Initialize(owner, pairId, cardKind, difficulty, CardPathPattern.RailLock);
        }

        public void Initialize(NodnarbGame owner, int pairId, CardKind cardKind, float difficulty, CardPathPattern cardPath)
        {
            game = owner;
            PairId = pairId;
            kind = cardKind;
            pathPattern = cardPath;
            alive = true;
            phase = pairId * 0.71f + (kind == CardKind.Weapon ? 0f : 1.8f);
            maxHealth = 3.8f + difficulty * 4.2f;
            health = maxHealth;
            speed = 2.0f + difficulty * 0.22f;
            // Keep the card body inside the portrait camera at the side rails.
            // The previous 2.25 lateral offset put the 1.48-unit panel beyond
            // the camera on the physical 1440x3120 S24 frame, clipping the
            // interactive card and making the left/right choice look broken.
            lane = kind == CardKind.Weapon ? -1.75f : 1.75f;
            accentColor = kind == CardKind.Recruit ? GameTheme.SignalBright : GameTheme.WeaponUpgrade;
            visualFlashTimer = 0f;
            visualFlashApplied = false;
            transform.position = new Vector3(game.RouteWorldX(lane, 18.8f), 0f, 18.8f);
            if (!visualBuilt)
            {
                BuildVisual();
                visualBuilt = true;
            }

            transform.localScale = Vector3.one;
            if (panel != null)
            {
                panel.localScale = panelBaseScale;
            }

            if (signalGlow != null)
            {
                signalGlow.localScale = signalGlowBaseScale;
            }

            ApplyAccessibilitySettings();
            gameObject.SetActive(true);
            game.RegisterCard(this);
        }

        public void RestoreCheckpoint(PausedCardData checkpoint)
        {
            if (!alive || game == null || checkpoint == null)
            {
                return;
            }

            float positionX = checkpoint.ExactState ? checkpoint.PositionX : game.RouteWorldX(lane, checkpoint.PositionZ);
            transform.position = new Vector3(positionX, 0f, checkpoint.PositionZ);
            maxHealth = checkpoint.ExactState ? checkpoint.MaxHealth : maxHealth;
            health = checkpoint.ExactState ? checkpoint.Health : maxHealth * Mathf.Clamp01(checkpoint.HealthRatio);
            speed = checkpoint.ExactState ? checkpoint.Speed : speed;
        }

        public void ApplyDamage(float damage)
        {
            if (!alive || damage <= 0f)
            {
                return;
            }

            health -= damage;
            visualFlashTimer = NodnarbSettings.ReducedMotionEnabled ? 0f : 0.10f;
            if (panel != null)
            {
                panel.localScale = NodnarbSettings.ReducedMotionEnabled
                    ? panelBaseScale
                    : panelBaseScale * 1.08f;
            }

            if (health <= 0f)
            {
                alive = false;
                game.UnregisterCard(this);
                game.OnCardActivated(this);
                game.ReleaseCard(this);
            }
        }

        public void Cancel()
        {
            if (!alive)
            {
                return;
            }

            alive = false;
            game.UnregisterCard(this);
            game.ReleaseCard(this);
        }

        private void Update()
        {
            if (!alive || game == null || !game.CombatActive)
            {
                return;
            }

            float deltaTime = game.SimulationDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 position = transform.position;
            position.z -= speed * deltaTime;
            position.y = NodnarbSettings.ReducedMotionEnabled
                ? 0f
                : Mathf.Sin(Time.time * 3.2f + phase) * 0.08f;
            float lateralX = Mathf.Clamp(lane + PathOffset(), kind == CardKind.Weapon ? -1.95f : 1.05f,
                kind == CardKind.Weapon ? -1.05f : 1.95f);
            position.x = game.RouteWorldX(lateralX, position.z);
            transform.position = position;
            if (panel != null)
            {
                panel.localScale = NodnarbSettings.ReducedMotionEnabled
                    ? panelBaseScale
                    : Vector3.Lerp(panel.localScale, panelBaseScale,
                        1f - Mathf.Exp(-14f * deltaTime));
            }
            if (signalGlow != null)
            {
                if (NodnarbSettings.ReducedMotionEnabled)
                {
                    signalGlow.localScale = signalGlowBaseScale;
                }
                else
                {
                    float pulse = 0.95f + Mathf.Sin(Time.time * 4.4f + phase) * 0.05f;
                    signalGlow.localScale = Vector3.Lerp(signalGlow.localScale,
                        signalGlowBaseScale * pulse,
                        1f - Mathf.Exp(-12f * deltaTime));
                }
            }
            ApplyCardFlash(deltaTime);

            if (position.z < GameTheme.CaptainZ - 1.1f)
            {
                alive = false;
                game.UnregisterCard(this);
                game.ReleaseCard(this);
            }
        }

        private float PathOffset()
        {
            if (NodnarbSettings.ReducedMotionEnabled)
            {
                return 0f;
            }

            switch (pathPattern)
            {
                case CardPathPattern.GentleDrift:
                    return Mathf.Sin(Time.time * 1.4f + phase) * 0.42f;
                case CardPathPattern.StaggeredDrift:
                    return Mathf.Sin(Time.time * 1.15f + phase) * 0.66f + Mathf.Sin(Time.time * 2.4f + phase) * 0.16f;
                case CardPathPattern.WideSweep:
                    return Mathf.Sin(Time.time * 1.9f + phase) * 0.98f;
                default:
                    return 0f;
            }
        }

        private void ApplyCardFlash(float deltaTime)
        {
            if (accessibilityRenderers == null)
            {
                return;
            }

            if (visualFlashTimer <= 0f || NodnarbSettings.ReducedMotionEnabled)
            {
                if (visualFlashApplied)
                {
                    ApplyAccessibilitySettings();
                }
                return;
            }

            if (visualFlashApplied)
            {
                visualFlashTimer = Mathf.Max(0f, visualFlashTimer - deltaTime);
                if (visualFlashTimer <= 0f)
                {
                    ApplyAccessibilitySettings();
                }
                return;
            }

            Color flashColor = kind == CardKind.Recruit ? GameTheme.AccessibleSignalBright : GameTheme.AccessibleWeapon;
            for (int index = 0; index < accessibilityRenderers.Length; index++)
            {
                Renderer renderer = accessibilityRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                MaterialPropertyBlock block = accessibilityColorBlock ?? new MaterialPropertyBlock();
                block.Clear();
                block.SetColor("_Color", Color.Lerp(Color.white, flashColor, 0.35f));
                renderer.SetPropertyBlock(block);
            }

            visualFlashApplied = true;

            visualFlashTimer = Mathf.Max(0f, visualFlashTimer - deltaTime);
            if (visualFlashTimer <= 0f)
            {
                ApplyAccessibilitySettings();
            }
        }

        private void OnDestroy()
        {
            if (alive && game != null)
            {
                alive = false;
                game.UnregisterCard(this);
            }
        }

        private void BuildVisual()
        {
            Color cardColor = accentColor;
            GameObject glowObject = PrimitiveFactory.Cube("CardSignalGlow", transform,
                transform.position + new Vector3(0f, 0.92f, -0.08f), new Vector3(1.62f, 1.86f, 0.04f),
                Color.Lerp(cardColor, GameTheme.SurfaceGlassDeep, 0.38f));
            signalGlow = glowObject.transform;
            GameObject panelObject = PrimitiveFactory.Cube(kind + "Card", transform, transform.position + Vector3.up * 0.92f,
                new Vector3(1.48f, 1.72f, 0.18f), GameTheme.SurfaceGlassDeep);
            panel = panelObject.transform;
            PrimitiveFactory.Cube("CardInset", transform, transform.position + new Vector3(0f, 0.92f, -0.13f),
                new Vector3(1.18f, 1.38f, 0.08f), GameTheme.Surface);
            PrimitiveFactory.Cube("CardTopSignal", transform, transform.position + new Vector3(0f, 1.69f, -0.20f),
                new Vector3(0.82f, 0.09f, 0.05f), cardColor);
            PrimitiveFactory.Cube("CardBottomSignal", transform, transform.position + new Vector3(0f, 0.15f, -0.20f),
                new Vector3(0.82f, 0.09f, 0.05f), cardColor);
            PrimitiveFactory.Cube("CardSideSignalLeft", transform, transform.position + new Vector3(-0.66f, 0.92f, -0.20f),
                new Vector3(0.07f, 0.86f, 0.05f), cardColor);
            PrimitiveFactory.Cube("CardSideSignalRight", transform, transform.position + new Vector3(0.66f, 0.92f, -0.20f),
                new Vector3(0.07f, 0.86f, 0.05f), cardColor);
            PrimitiveFactory.Cube("CardCenterRule", transform, transform.position + new Vector3(0f, 0.92f, -0.22f),
                new Vector3(0.90f, 0.025f, 0.04f), Color.Lerp(cardColor, GameTheme.Rule, 0.42f));
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject corner = PrimitiveFactory.Cube("CardCornerSlash" + side, transform,
                    transform.position + new Vector3(side * 0.48f, 1.54f, -0.23f),
                    new Vector3(0.10f, 0.28f, 0.05f), cardColor);
                corner.transform.rotation = Quaternion.Euler(0f, 0f, side * 45f);
            }

            if (kind == CardKind.Weapon)
            {
                PrimitiveFactory.Cube("WeaponIconBody", transform, transform.position + new Vector3(0f, 1.25f, -0.24f),
                    new Vector3(0.62f, 0.12f, 0.08f), cardColor);
                PrimitiveFactory.Cube("WeaponIconGrip", transform, transform.position + new Vector3(-0.18f, 1.10f, -0.24f),
                    new Vector3(0.12f, 0.28f, 0.08f), cardColor);
                PrimitiveFactory.Cube("WeaponIconMuzzle", transform, transform.position + new Vector3(0.38f, 1.25f, -0.24f),
                    new Vector3(0.16f, 0.08f, 0.08f), cardColor);
                PrimitiveFactory.Cube("WeaponIconScope", transform, transform.position + new Vector3(0.04f, 1.40f, -0.24f),
                    new Vector3(0.22f, 0.06f, 0.07f), GameTheme.SignalCyan);
                PrimitiveFactory.Cylinder("WeaponIconNode", transform, transform.position + new Vector3(0.52f, 1.25f, -0.24f),
                    new Vector3(0.09f, 0.06f, 0.09f), GameTheme.SignalBright);
            }
            else
            {
                for (int crewIndex = 0; crewIndex < 3; crewIndex++)
                {
                    float iconX = (crewIndex - 1) * 0.25f;
                    PrimitiveFactory.Sphere("CrewIconHead" + crewIndex, transform,
                        transform.position + new Vector3(iconX, 1.35f, -0.24f),
                        new Vector3(0.12f, 0.12f, 0.08f), cardColor);
                    PrimitiveFactory.Capsule("CrewIconBody" + crewIndex, transform,
                        transform.position + new Vector3(iconX, 1.16f, -0.24f),
                        new Vector3(0.16f, 0.26f, 0.09f), cardColor);
                }
                PrimitiveFactory.Cylinder("CrewIconBeacon", transform,
                    transform.position + new Vector3(0f, 1.20f, -0.26f),
                    new Vector3(0.07f, 0.22f, 0.07f), GameTheme.SignalCyan);
                PrimitiveFactory.Sphere("CrewIconBeaconCore", transform,
                    transform.position + new Vector3(0f, 1.38f, -0.26f),
                    new Vector3(0.10f, 0.10f, 0.07f), GameTheme.SignalBright);
            }

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.63f, -0.22f);
            labelObject.transform.localRotation = Quaternion.identity;
            label = labelObject.AddComponent<TextMesh>();
            label.text = DisplayLabel;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 50;
            label.characterSize = 0.047f;
            label.fontStyle = FontStyle.Bold;
            label.color = cardColor;

            visualRoot = new GameObject("CardVisualRoot").transform;
            visualRoot.SetParent(transform, false);
            for (int index = 0; index < transform.childCount; index++)
            {
                Transform child = transform.GetChild(index);
                if (child != visualRoot)
                {
                    child.localScale *= VisualGeometryScale;
                }
            }

            visualRoot.localScale = Vector3.one * VisualGeometryScale;
            panelBaseScale = panel == null ? Vector3.one : panel.localScale;
            signalGlowBaseScale = signalGlow == null ? Vector3.one : signalGlow.localScale;
            accessibilityRenderers = GetComponentsInChildren<Renderer>(true);
            accessibilityColorBlock = new MaterialPropertyBlock();
            ApplyAccessibilitySettings();
        }

        public void ApplyAccessibilitySettings()
        {
            visualFlashApplied = false;
            bool highContrast = NodnarbSettings.HighContrastEnabled;
            Color accessibleColor = kind == CardKind.Weapon
                ? GameTheme.AccessibleWeapon
                : GameTheme.AccessibleSignalBright;

            if (accessibilityRenderers != null)
            {
                for (int index = 0; index < accessibilityRenderers.Length; index++)
                {
                    Renderer renderer = accessibilityRenderers[index];
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (highContrast)
                    {
                        accessibilityColorBlock.Clear();
                        accessibilityColorBlock.SetColor("_Color", accessibleColor);
                        renderer.SetPropertyBlock(accessibilityColorBlock);
                    }
                    else
                    {
                        renderer.SetPropertyBlock(null);
                    }
                }
            }

            if (label != null)
            {
                label.color = highContrast ? accessibleColor : accentColor;
            }

            AccessibilityVisualApplied = highContrast;
        }
    }
}
