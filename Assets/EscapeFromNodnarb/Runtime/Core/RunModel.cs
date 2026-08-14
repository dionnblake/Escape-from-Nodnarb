using System;

namespace EscapeFromNodnarb
{
    public enum RunEndReason
    {
        None,
        CaptainDown,
        FrontLineBreached,
        ExtractionSecured
    }

    public sealed class RunModel
    {
        public const int StartingSoldiers = 2;
        public const int VisibleSoldierCap = 12;
        public const float MaxCaptainHealth = 100f;
        public const float RapidFireDuration = 4f;
        public const float RapidFireCooldown = 18f;
        public const float RapidFireRateBoost = 2.6f;
        public const float BaseFireInterval = 0.34f;
        public const float WeaponUpgradeDamageStep = 0.30f;
        public const float OverflowDamageStep = 0.08f;
        public const float WeaponUpgradeRateStep = 0.12f;
        public const float OverflowRateStep = 0.025f;
        public const int MaxWeaponLevel = 64;
        public const int MaxOverflowRecruits = 64;

        public float CaptainHealth { get; private set; }
        public int SoldierCount { get; private set; }
        public int OverflowRecruits { get; private set; }
        public int WeaponLevel { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Salvage { get; private set; }
        public float RapidFireRemaining { get; private set; }
        public float RapidFireCooldownRemaining { get; private set; }
        public bool ComebackAssistUsed { get; private set; }
        public int SelectedWeapon { get; private set; }
        public RunEndReason EndReason { get; private set; }

        public RunModel(int startingSoldiers, int selectedWeapon)
        {
            CaptainHealth = MaxCaptainHealth;
            SoldierCount = Math.Max(0, Math.Min(VisibleSoldierCap, startingSoldiers));
            SelectedWeapon = LoadoutCatalog.ClampWeapon(selectedWeapon);
        }

        public bool IsEnded
        {
            get { return EndReason != RunEndReason.None; }
        }

        public bool RapidFireActive
        {
            get { return RapidFireRemaining > 0f; }
        }

        public bool RapidFireReady
        {
            get { return !IsEnded && RapidFireCooldownRemaining <= 0f; }
        }

        public bool ComebackActive
        {
            get { return !IsEnded && CaptainHealth <= 35f; }
        }

        public bool TryConsumeComebackAssist()
        {
            if (!ComebackActive || ComebackAssistUsed)
            {
                return false;
            }

            ComebackAssistUsed = true;
            return true;
        }

        public int VisibleShooterCount
        {
            get { return 1 + SoldierCount; }
        }

        public float ShotDamage
        {
            get
            {
                WeaponDefinition weapon = LoadoutCatalog.Weapons[SelectedWeapon];
                float weaponPower = 1f + (WeaponLevel * WeaponUpgradeDamageStep);
                float overflowPower = 1f + (OverflowRecruits * OverflowDamageStep);
                return weapon.BaseDamage * weaponPower * overflowPower;
            }
        }

        public float FireInterval
        {
            get
            {
                WeaponDefinition weapon = LoadoutCatalog.Weapons[SelectedWeapon];
                float rate = weapon.FireRateMultiplier * (1f + WeaponLevel * WeaponUpgradeRateStep
                    + OverflowRecruits * OverflowRateStep);
                if (RapidFireActive)
                {
                    rate *= RapidFireRateBoost;
                }

                return BaseFireInterval / Math.Max(0.2f, rate);
            }
        }

        public float ShotsPerSecond
        {
            get { return 1f / Math.Max(0.01f, FireInterval); }
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            RapidFireRemaining = Math.Max(0f, RapidFireRemaining - deltaTime);
            RapidFireCooldownRemaining = Math.Max(0f, RapidFireCooldownRemaining - deltaTime);
        }

        public bool ActivateRapidFire()
        {
            if (!RapidFireReady)
            {
                return false;
            }

            RapidFireRemaining = RapidFireDuration;
            RapidFireCooldownRemaining = RapidFireCooldown;
            return true;
        }

        public void DamageCaptain(float damage)
        {
            if (IsEnded || damage <= 0f)
            {
                return;
            }

            CaptainHealth = Math.Max(0f, CaptainHealth - damage);
            if (CaptainHealth <= 0f)
            {
                EndReason = RunEndReason.CaptainDown;
            }
        }

        public void Recruit()
        {
            if (IsEnded)
            {
                return;
            }

            if (SoldierCount < VisibleSoldierCap)
            {
                SoldierCount++;
            }
            else
            {
                OverflowRecruits = Math.Min(MaxOverflowRecruits, OverflowRecruits + 1);
            }
        }

        public void UpgradeWeapon()
        {
            if (!IsEnded && WeaponLevel < MaxWeaponLevel)
            {
                WeaponLevel++;
            }
        }

        public void RegisterKill(int points, int salvage)
        {
            if (IsEnded)
            {
                return;
            }

            Kills++;
            Score += Math.Max(0, points);
            Salvage += Math.Max(0, salvage);
        }

        public void AddScore(int points)
        {
            if (!IsEnded)
            {
                Score += Math.Max(0, points);
            }
        }

        public void BreachFrontLine()
        {
            if (!IsEnded)
            {
                EndReason = RunEndReason.FrontLineBreached;
            }
        }

        public void SecureExtraction()
        {
            if (!IsEnded)
            {
                EndReason = RunEndReason.ExtractionSecured;
            }
        }

        public PausedRunData CreateSnapshot(int levelIndex, bool endless, float elapsed)
        {
            return new PausedRunData
            {
                LevelIndex = levelIndex,
                Endless = endless,
                Elapsed = Math.Max(0f, elapsed),
                CaptainHealth = CaptainHealth,
                SoldierCount = SoldierCount,
                OverflowRecruits = OverflowRecruits,
                WeaponLevel = WeaponLevel,
                SelectedWeapon = SelectedWeapon,
                Score = Score,
                Kills = Kills,
                Salvage = Salvage,
                RapidFireRemaining = RapidFireRemaining,
                RapidFireCooldownRemaining = RapidFireCooldownRemaining,
                ComebackAssistUsed = ComebackAssistUsed
            };
        }

        public bool Restore(PausedRunData snapshot)
        {
            if (snapshot == null || !snapshot.IsValid())
            {
                return false;
            }

            CaptainHealth = Math.Max(0f, Math.Min(MaxCaptainHealth, snapshot.CaptainHealth));
            SoldierCount = Math.Max(0, Math.Min(VisibleSoldierCap, snapshot.SoldierCount));
            OverflowRecruits = Math.Max(0, Math.Min(MaxOverflowRecruits, snapshot.OverflowRecruits));
            WeaponLevel = Math.Max(0, Math.Min(MaxWeaponLevel, snapshot.WeaponLevel));
            SelectedWeapon = LoadoutCatalog.ClampWeapon(snapshot.SelectedWeapon);
            Score = Math.Max(0, snapshot.Score);
            Kills = Math.Max(0, snapshot.Kills);
            Salvage = Math.Max(0, snapshot.Salvage);
            RapidFireRemaining = Math.Max(0f, snapshot.RapidFireRemaining);
            RapidFireCooldownRemaining = Math.Max(0f, snapshot.RapidFireCooldownRemaining);
            ComebackAssistUsed = snapshot.ComebackAssistUsed;
            EndReason = RunEndReason.None;
            return true;
        }
    }
}
