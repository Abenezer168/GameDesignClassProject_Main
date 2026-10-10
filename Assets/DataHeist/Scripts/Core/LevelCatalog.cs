using System.Collections.Generic;

namespace DataHeist
{
    /// <summary>The campaign in order: level id, its scene and display name. A level unlocks when the previous one is cleared.</summary>
    public static class LevelCatalog
    {
        public struct Entry
        {
            public string id, scene, name;
            public int number;
        }

        public static readonly IReadOnlyList<Entry> All = new[]
        {
            new Entry { id = "level01", scene = DH.SceneLevel01, name = "NEON ROOFTOPS", number = 1 },
            new Entry { id = "level02", scene = DH.SceneLevel02, name = "CORPORATE DISTRICT", number = 2 },
        };

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].id == id) return i;
            return 0;
        }

        public static Entry Get(string id) => All[IndexOf(id)];

        /// <summary>The level after this one, or null at the end of the campaign.</summary>
        public static Entry? Next(string id)
        {
            int i = IndexOf(id) + 1;
            return i < All.Count ? All[i] : (Entry?)null;
        }

        public static bool IsUnlocked(string id)
        {
            int i = IndexOf(id);
            return i == 0 || SaveSystem.Data.cleared.Contains(All[i - 1].id);
        }
    }
}
