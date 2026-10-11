using System.Collections.Generic;

namespace DataHeist
{
    /// <summary>
    /// The campaign in order: level id, its scene, display name and the one-line mission briefing shown during
    /// the first countdown (it carries the story between the intro and the ending). A level unlocks when the
    /// previous one is cleared.
    /// </summary>
    public static class LevelCatalog
    {
        public struct Entry
        {
            public string id, scene, name, briefing;
            public int number;
        }

        public static readonly IReadOnlyList<Entry> All = new[]
        {
            new Entry { id = "level01", scene = DH.SceneLevel01, name = "NEON ROOFTOPS", number = 1,
                briefing = "The first shard is up on the rooftops. Grab it and reach extraction before the drones lock on." },
            new Entry { id = "level02", scene = DH.SceneLevel02, name = "CORPORATE DISTRICT", number = 2,
                briefing = "Shard 1 named a payroll of bribed officials. Shard 2 is in the towers that paid them. Watch for snipers." },
            new Entry { id = "level03", scene = DH.SceneLevel03, name = "UNDERGROUND VAULT", number = 3,
                briefing = "The shards unlock Halcyon's archive. Find 3 fragments, open the vault and take the evidence." },
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
