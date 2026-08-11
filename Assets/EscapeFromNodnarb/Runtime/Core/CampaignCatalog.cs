using System;
using System.Collections.Generic;

namespace EscapeFromNodnarb
{
    public enum BiomeId
    {
        CrashGlass,
        RustCanyon,
        SporeField,
        BoneMarsh,
        Snowline,
        SignalRuins,
        CrystalFault,
        HiveTrench,
        NightShelf,
        BeaconPlain,
        ExtractionRing
    }

    public enum WavePattern
    {
        RushLanes,
        Crossfire,
        ArmorColumns,
        SwarmPulse,
        SiegeMix
    }

    public enum CardPathPattern
    {
        RailLock,
        GentleDrift,
        StaggeredDrift,
        WideSweep
    }

    public enum RouteShape
    {
        WreckInterior,
        CanyonBend,
        UnknownWinding,
        SnowSwitchback,
        RelaySwerve,
        CrystalShelf,
        HiveRun,
        NightSlope,
        BeaconApproach,
        ExtractionRing
    }

    public enum BossBehavior
    {
        Crusher,
        Striker,
        Barrager,
        Sentry,
        Howler,
        Carrier
    }

    [Serializable]
    public sealed class LevelDefinition
    {
        public int Index;
        public string Sector;
        public string Name;
        public string StoryTitle;
        public string RadioMessage;
        public string BossName;
        public float DurationSeconds;
        public float Difficulty;
        public int Seed;
        public BiomeId Biome;
        public WavePattern Pattern;
        public CardPathPattern CardPath;
        public RouteShape Route;
        public BossBehavior BossBehavior;

        public LevelDefinition(
            int index,
            string sector,
            string name,
            string storyTitle,
            string radioMessage,
            string bossName,
            float durationSeconds,
            float difficulty,
            int seed,
            BiomeId biome,
            WavePattern pattern,
            CardPathPattern cardPath,
            RouteShape route,
            BossBehavior bossBehavior)
        {
            Index = index;
            Sector = sector;
            Name = name;
            StoryTitle = storyTitle;
            RadioMessage = radioMessage;
            BossName = bossName;
            DurationSeconds = durationSeconds;
            Difficulty = difficulty;
            Seed = seed;
            Biome = biome;
            Pattern = pattern;
            CardPath = cardPath;
            Route = route;
            BossBehavior = bossBehavior;
        }
    }

    public static class CampaignCatalog
    {
        private static readonly LevelDefinition[] Levels =
        {
            new LevelDefinition(1, "N-01", "Wreck Interior", "HULL BREACH",
                "NAV CORE: Hull split on impact. Rescue ping is weak. Hold the shelf while I rebuild the route.",
                "Gravel Maw", 66f, 0.14f, 1103, BiomeId.CrashGlass, WavePattern.RushLanes,
                CardPathPattern.RailLock, RouteShape.WreckInterior, BossBehavior.Crusher),
            new LevelDefinition(2, "N-02", "Crash Basin", "NO WAY BACK",
                "CAPTAIN: The ship is burning behind us. The canyon is the only line toward the beacon.",
                "Canyon Brute", 68f, 0.22f, 2207, BiomeId.RustCanyon, WavePattern.ArmorColumns,
                CardPathPattern.GentleDrift, RouteShape.CanyonBend, BossBehavior.Striker),
            new LevelDefinition(3, "N-03", "Unknown March", "THE PLANET MOVES",
                "NAV CORE: Spores react to heat. Every shot tells the planet exactly where we are.",
                "Bloom Stalker", 70f, 0.30f, 3301, BiomeId.SporeField, WavePattern.Crossfire,
                CardPathPattern.WideSweep, RouteShape.UnknownWinding, BossBehavior.Barrager),
            new LevelDefinition(4, "N-04", "Whiteglass Pass", "COLD SIGNAL",
                "CREW: Those arches are not stone. Keep moving before whatever made them comes home.",
                "Whiteglass Howler", 72f, 0.39f, 4409, BiomeId.Snowline, WavePattern.SwarmPulse,
                CardPathPattern.StaggeredDrift, RouteShape.SnowSwitchback, BossBehavior.Howler),
            new LevelDefinition(5, "N-05", "Silent Ruins", "A SIGNAL UNDER THE STATIC",
                "NAV CORE: I found a relay beneath the ruins. Power it and rescue may finally hear us.",
                "Relay Warden", 74f, 0.48f, 5519, BiomeId.SignalRuins, WavePattern.Crossfire,
                CardPathPattern.RailLock, RouteShape.RelaySwerve, BossBehavior.Sentry),
            new LevelDefinition(6, "N-06", "Crystal Fault", "THE PLANET ANSWERS",
                "CAPTAIN: The relay worked. Something else answered first. Cut through the fault.",
                "Shard Titan", 76f, 0.57f, 6607, BiomeId.CrystalFault, WavePattern.ArmorColumns,
                CardPathPattern.WideSweep, RouteShape.CrystalShelf, BossBehavior.Carrier),
            new LevelDefinition(7, "N-07", "Hive Trench", "TOO MANY HEARTBEATS",
                "NAV CORE: Movement below us. Thousands of contacts. The beacon lies beyond the nest.",
                "Brood Engine", 78f, 0.66f, 7717, BiomeId.HiveTrench, WavePattern.SwarmPulse,
                CardPathPattern.StaggeredDrift, RouteShape.HiveRun, BossBehavior.Barrager),
            new LevelDefinition(8, "N-08", "Night Shelf", "RESCUE WINDOW CLOSING",
                "RESCUE: Unknown crew, your signal is fading. Reach open ground before orbital night locks us out.",
                "Night Howler", 81f, 0.75f, 8803, BiomeId.NightShelf, WavePattern.Crossfire,
                CardPathPattern.GentleDrift, RouteShape.NightSlope, BossBehavior.Howler),
            new LevelDefinition(9, "N-09", "Beacon Plain", "LIGHT THE SKY",
                "CAPTAIN: We can see the beacon. One clean push and this world becomes somebody else's problem.",
                "Signal Eater", 84f, 0.86f, 9901, BiomeId.BeaconPlain, WavePattern.SiegeMix,
                CardPathPattern.WideSweep, RouteShape.BeaconApproach, BossBehavior.Striker),
            new LevelDefinition(10, "N-10", "Extraction Ring", "GET US OFF NODNARB",
                "RESCUE: Landing corridor is open for ninety seconds. Hold the ring. We leave together or not at all.",
                "The Last Carrier", 90f, 1.00f, 10103, BiomeId.ExtractionRing, WavePattern.SiegeMix,
                CardPathPattern.RailLock, RouteShape.ExtractionRing, BossBehavior.Carrier)
        };

        private static readonly BossBehavior[] EndlessBossBehaviors =
        {
            BossBehavior.Barrager,
            BossBehavior.Striker,
            BossBehavior.Howler,
            BossBehavior.Sentry,
            BossBehavior.Carrier,
            BossBehavior.Crusher
        };

        public static IReadOnlyList<LevelDefinition> All
        {
            get { return Levels; }
        }

        public static LevelDefinition Get(int oneBasedIndex)
        {
            int index = Math.Max(1, Math.Min(Levels.Length, oneBasedIndex));
            return Levels[index - 1];
        }

        public static LevelDefinition CreateEndless(int seed)
        {
            return new LevelDefinition(0, "ENDLESS", "Dead Signal", "NO RESCUE VECTOR",
                "NAV CORE: The beacon is gone. Count ammunition, keep the line, and make every second expensive.",
                "Recurring Carrier", 0f, 0.72f, seed, BiomeId.NightShelf, WavePattern.SiegeMix,
                CardPathPattern.WideSweep, RouteShape.NightSlope, BossBehavior.Carrier);
        }

        public static BossBehavior GetEndlessBossBehavior(int cycle)
        {
            int index = Math.Max(0, cycle - 1) % EndlessBossBehaviors.Length;
            return EndlessBossBehaviors[index];
        }

        public static string RouteBrief(RouteShape route)
        {
            switch (route)
            {
                case RouteShape.WreckInterior: return "SHIP DECK // TIGHT SHELF";
                case RouteShape.CanyonBend: return "CRASH BASIN // BENT LANE";
                case RouteShape.UnknownWinding: return "SPORE FIELD // WINDING LANE";
                case RouteShape.SnowSwitchback: return "WHITEGLASS // SWITCHBACK";
                case RouteShape.RelaySwerve: return "SILENT RUINS // RELAY SWERVE";
                case RouteShape.CrystalShelf: return "CRYSTAL FAULT // SPLIT SHELF";
                case RouteShape.HiveRun: return "HIVE TRENCH // RUNNING LINE";
                case RouteShape.NightSlope: return "NIGHT SHELF // DESCENT";
                case RouteShape.BeaconApproach: return "BEACON PLAIN // LONG APPROACH";
                case RouteShape.ExtractionRing: return "EXTRACTION RING // HOLD THE LOOP";
                default: return "UNKNOWN ROUTE // HOLD CENTER";
            }
        }

        public static float RouteMapY(RouteShape route)
        {
            switch (route)
            {
                case RouteShape.WreckInterior: return 0.48f;
                case RouteShape.CanyonBend: return 0.62f;
                case RouteShape.UnknownWinding: return 0.38f;
                case RouteShape.SnowSwitchback: return 0.58f;
                case RouteShape.RelaySwerve: return 0.45f;
                case RouteShape.CrystalShelf: return 0.64f;
                case RouteShape.HiveRun: return 0.34f;
                case RouteShape.NightSlope: return 0.52f;
                case RouteShape.BeaconApproach: return 0.42f;
                case RouteShape.ExtractionRing: return 0.60f;
                default: return 0.48f;
            }
        }

        public static string RouteBeat(LevelDefinition level)
        {
            if (level == null || level.Index <= 0)
            {
                return "KEEP THE LINE // NO FIXED ROUTE";
            }

            switch (level.Index)
            {
                case 1: return "BROKEN DECK // FOLLOW THE HULL RIBS";
                case 2: return "BENT BASIN // SHIFT WITH THE CANYON";
                case 3: return "SPORE WIND // WATCH THE SAFE GAPS";
                case 4: return "SWITCHBACK // CLIMB BETWEEN THE ARCHES";
                case 5: return "RELAY SWERVE // KEEP THE BEACON IN SIGHT";
                case 6: return "SPLIT SHELF // TAKE THE OPEN SIDE";
                case 7: return "HIVE RUN // DO NOT STOP IN THE TRENCH";
                case 8: return "NIGHT DESCENT // HOLD THE LOW LINE";
                case 9: return "LONG APPROACH // SAVE ROOM FOR THE PUSH";
                case 10: return "RING LOOP // HOLD UNTIL RESCUE LOCKS";
                default: return "UNKNOWN ROUTE // HOLD CENTER";
            }
        }

        public static string BossBrief(LevelDefinition level)
        {
            if (level == null)
            {
                return "UNKNOWN THREAT // HOLD THE LINE";
            }

            string behavior;
            switch (level.BossBehavior)
            {
                case BossBehavior.Crusher: behavior = "CLOSE-RANGE PUSH"; break;
                case BossBehavior.Striker: behavior = "TRACKING FIRE"; break;
                case BossBehavior.Barrager: behavior = "RAPID VOLLEYS"; break;
                case BossBehavior.Sentry: behavior = "ARMORED ANCHOR"; break;
                case BossBehavior.Howler: behavior = "WIDE SWEEP"; break;
                case BossBehavior.Carrier: behavior = "HEAVY CARRIER"; break;
                default: behavior = "UNKNOWN THREAT"; break;
            }

            return "BOSS // " + level.BossName.ToUpperInvariant() + " // " + behavior;
        }

        public static string BiomeBrief(BiomeId biome)
        {
            switch (biome)
            {
                case BiomeId.CrashGlass: return "WRECK INTERIOR // BROKEN DECK";
                case BiomeId.RustCanyon: return "CRASH BASIN // RUST CLIFFS";
                case BiomeId.SporeField: return "UNKNOWN MARCH // SPORE GROWTH";
                case BiomeId.BoneMarsh: return "BONE MARSH // DEAD TERRAIN";
                case BiomeId.Snowline: return "WHITEGLASS PASS // ICE ARCHES";
                case BiomeId.SignalRuins: return "SILENT RUINS // DEAD RELAY";
                case BiomeId.CrystalFault: return "CRYSTAL FAULT // SHARD FIELD";
                case BiomeId.HiveTrench: return "HIVE TRENCH // BROOD GROUND";
                case BiomeId.NightShelf: return "NIGHT SHELF // LOW ORBIT";
                case BiomeId.BeaconPlain: return "BEACON PLAIN // OPEN GROUND";
                case BiomeId.ExtractionRing: return "EXTRACTION RING // RESCUE ZONE";
                default: return "UNKNOWN TERRAIN // HOLD CENTER";
            }
        }

        public static string ObjectiveBrief(LevelDefinition level)
        {
            if (level == null || level.Index <= 0)
            {
                return "SURVIVE THE SIGNAL // KEEP THE SQUAD MOVING";
            }

            switch (level.Index)
            {
                case 1: return "SECURE THE WRECK // RESTORE NAV CORE";
                case 2: return "CROSS THE CRASH BASIN // LEAVE THE HULL";
                case 3: return "FIND THE SAFE LINE // CROSS UNKNOWN TERRAIN";
                case 4: return "CLIMB WHITEGLASS PASS // REACH THE RIDGE";
                case 5: return "POWER THE RELAY // SEND THE RESCUE PING";
                case 6: return "CROSS THE CRYSTAL FAULT // FOLLOW THE ANSWER";
                case 7: return "BREAK THE HIVE TRENCH // PROTECT THE BEACON PATH";
                case 8: return "DESCEND THE NIGHT SHELF // REACH OPEN GROUND";
                case 9: return "HOLD THE BEACON APPROACH // LIGHT THE SKY";
                case 10: return "HOLD THE EXTRACTION RING // LEAVE NODNARB";
                default: return "SURVIVE THE SIGNAL // KEEP THE SQUAD MOVING";
            }
        }

        public static string MilestoneBrief(LevelDefinition level)
        {
            if (level == null || level.Index <= 0)
            {
                return "NO RESCUE VECTOR // ENDLESS SIGNAL";
            }

            switch (level.Index)
            {
                case 1: return "WRECK SEALED // CREW ACCOUNTED FOR";
                case 2: return "SHIP BEHIND US // BEACON STILL VISIBLE";
                case 3: return "MAP UPDATED // THE PLANET IS LISTENING";
                case 4: return "RIDGE CLEARED // SIGNAL HAS A PATH";
                case 5: return "RELAY ONLINE // RESCUE HEARS US";
                case 6: return "FAULT CROSSED // SOMETHING ANSWERED";
                case 7: return "HIVE BROKEN // BEACON ROUTE OPEN";
                case 8: return "NIGHT PASSED // OPEN GROUND AHEAD";
                case 9: return "SKY LIT // LANDING CORRIDOR LOCKED";
                case 10: return "EXTRACTION SECURED // LEAVE TOGETHER";
                default: return "SIGNAL HELD // KEEP MOVING";
            }
        }
    }
}
