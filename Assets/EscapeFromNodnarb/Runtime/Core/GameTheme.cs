using UnityEngine;

namespace EscapeFromNodnarb
{
    // Hallmark · pre-emit critique: P4 H5 E4 S5 R4 V4
    // Hallmark · macrostructure: Workbench · theme: custom dark terminal
    // Tokens mirror /tokens.css. Runtime UI must reference these names instead of local color literals.
    public static class GameTheme
    {
        public static readonly Color Void = Hex("0B0E0B");
        public static readonly Color Surface = Hex("141914");
        public static readonly Color SurfaceRaised = Hex("1D241D");
        public static readonly Color Rule = Hex("465046");
        public static readonly Color Text = Hex("F2F5ED");
        public static readonly Color Muted = Hex("8C9588");
        public static readonly Color Signal = Hex("84CC16");
        public static readonly Color SignalBright = Hex("B7FF3C");
        public static readonly Color Danger = Hex("D04B4B");
        public static readonly Color Weapon = Hex("D9D5C7");
        public static readonly Color WeaponUpgrade = Hex("FF9B43");
        public static readonly Color CaptainOrange = Hex("C66A32");
        public static readonly Color CanyonRust = Hex("9A452B");
        public static readonly Color CanyonHighlight = Hex("D17642");
        public static readonly Color AlienViolet = Hex("6C3A78");
        public static readonly Color AlienGlow = Hex("F05A9D");
        public static readonly Color AlienArmored = Hex("41545C");
        public static readonly Color AlienRanged = Hex("8A455D");
        public static readonly Color ProjectileFriendly = Hex("C8FF65");
        public static readonly Color ProjectileHostile = Hex("FF6A5F");

        public const float ArenaHalfWidth = 4.15f;
        public const float CaptainZ = -2.4f;
        public const float FrontLineZ = -0.35f;
        public const float SpawnZ = 21f;

        public static Color SuitColor(int suitIndex)
        {
            switch (LoadoutCatalog.ClampSuit(suitIndex))
            {
                case 1:
                    return Signal;
                case 2:
                    return Text;
                default:
                    return CaptainOrange;
            }
        }

        public static BiomePalette GetBiome(BiomeId biome)
        {
            switch (biome)
            {
                case BiomeId.RustCanyon:
                    return new BiomePalette(Hex("4C2F26"), Hex("8E4F31"), Hex("231815"), Hex("D8894F"));
                case BiomeId.SporeField:
                    return new BiomePalette(Hex("323B2D"), Hex("667D56"), Hex("172019"), Hex("A5D06F"));
                case BiomeId.BoneMarsh:
                    return new BiomePalette(Hex("46463F"), Hex("7C7464"), Hex("1A1D1C"), Hex("D3C394"));
                case BiomeId.Snowline:
                    return new BiomePalette(Hex("294556"), Hex("5E879E"), Hex("101D2C"), Hex("DDF4FF"));
                case BiomeId.SignalRuins:
                    return new BiomePalette(Hex("314347"), Hex("56777B"), Hex("141C20"), Hex("7BC5C7"));
                case BiomeId.CrystalFault:
                    return new BiomePalette(Hex("39334C"), Hex("665A8C"), Hex("171426"), Hex("B9A9FF"));
                case BiomeId.HiveTrench:
                    return new BiomePalette(Hex("452B33"), Hex("7F4453"), Hex("1E1017"), Hex("C77B8C"));
                case BiomeId.NightShelf:
                    return new BiomePalette(Hex("283B4B"), Hex("476C83"), Hex("0F1823"), Hex("71A9C4"));
                case BiomeId.BeaconPlain:
                    return new BiomePalette(Hex("2F4940"), Hex("5D7B6E"), Hex("121C18"), Signal);
                case BiomeId.ExtractionRing:
                    return new BiomePalette(Hex("3C4140"), Hex("6B746F"), Hex("141817"), Signal);
                default:
                    return new BiomePalette(Hex("3F2922"), Hex("A25D38"), Hex("111619"), Hex("E1A05D"));
            }
        }

        public static Color Hex(string value)
        {
            Color parsed;
            return ColorUtility.TryParseHtmlString("#" + value, out parsed) ? parsed : Color.magenta;
        }
    }

    public struct BiomePalette
    {
        public readonly Color Ground;
        public readonly Color Terrain;
        public readonly Color Sky;
        public readonly Color Detail;

        public BiomePalette(Color ground, Color terrain, Color sky, Color detail)
        {
            Ground = ground;
            Terrain = terrain;
            Sky = sky;
            Detail = detail;
        }
    }
}
