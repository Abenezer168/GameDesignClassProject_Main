using UnityEngine;

namespace DataHeist
{
    /// <summary>Project-wide constants, resource paths, palette and sorting orders.</summary>
    public static class DH
    {
        public const float PPU = 32f;
        public const float OrthoSize = 5.625f;          // 640x360 virtual resolution at 32 px/unit
        public const float SimStep = 1f / 120f;          // fixed simulation tick (matches browser build)

        public const string Root = "DataHeist/";
        public const string CharPath = Root + "Characters/";
        public const string LevelPath = Root + "Levels/";
        public const string SfxPath = Root + "Audio/SFX/sfx_";
        public const string MusicPath = Root + "Audio/Music/";
        public const string FontPath = Root + "Fonts/PressStart2P-Regular";
        public const string CharacterDefsPath = Root + "CharacterDefs";

        // Scene names (must be in Build Settings - Data Heist > Setup Project adds them)
        public const string SceneMainMenu = "MainMenu";
        public const string SceneCharacterSelect = "CharacterSelectScene";
        public const string SceneLevel01 = "Level01_NeonRooftops";
        public const string SceneLevel02 = "Level02_CorporateDistrict";
        public const string SceneLevel03 = "Level03_UndergroundVault";
        public const string SceneStory = "StoryScene";
        public const string SceneEnding = "EndingScene";
        public const string SceneCompletion = "CompletionScene";
        public const string SceneCredits = "CreditsScene";

        // Sorting orders (single sorting layer keeps setup zero-config)
        public const int SortSky = -200, SortFar = -190, SortMid = -180, SortNear = -170;
        public const int SortDecor = -40, SortFacade = -30, SortRoofTop = -29, SortPlatform = -25;
        public const int SortProps = 0, SortPickups = 5, SortEnemies = 8, SortBolts = 9;
        public const int SortPlayer = 20, SortFX = 30, SortText = 40;

        // GDD palette
        public static readonly Color Night = Hex("0b0618");
        public static readonly Color Purple = Hex("2a1748");
        public static readonly Color PurpleLine = Hex("3d2466");
        public static readonly Color Panel = Hex("160c2a");
        public static readonly Color Ink = Hex("e8e2ff");
        public static readonly Color Dim = Hex("8a7fb0");
        public static readonly Color Cyan = Hex("2ff3ff");
        public static readonly Color Pink = Hex("ff3fa4");
        public static readonly Color Orange = Hex("ff8a2a");
        public static readonly Color Amber = Hex("ffd23a");
        public static readonly Color Red = Hex("ff2d55");
        public static readonly Color Green = Hex("7dff6a");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out var c);
            return c;
        }

        public static string FormatTime(float s)
        {
            int m = Mathf.FloorToInt(s / 60f);
            float r = s - m * 60f;
            return $"{m}:{r:00.00}";
        }
    }
}
