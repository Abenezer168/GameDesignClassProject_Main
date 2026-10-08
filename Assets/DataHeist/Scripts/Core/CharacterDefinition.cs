using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DataHeist
{
    public enum AbilityType { KickThrusters, PhaseDash, EmpPulse, TimeGlitch }
    public enum UnlockType { Free, ClearLevel, Bytes }

    /// <summary>
    /// Designer-tunable runner. Assets live in Resources/DataHeist/CharacterDefs
    /// (Data Heist > Setup Project creates them). If none exist the built-in roster is used.
    /// </summary>
    [CreateAssetMenu(menuName = "Data Heist/Character Definition", fileName = "Character")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "kai";              // also the sprite folder name
        public string displayName = "KAI OKAFOR";
        public string title = "The Courier";
        [TextArea] public string bio;
        public Color color = Color.cyan;
        public int order;

        [Header("Stats")]
        [Range(1, 6)] public int maxHealth = 3;
        [Range(0.7f, 1.4f)] public float speedMultiplier = 1f;
        [Range(0.7f, 1.4f)] public float jumpMultiplier = 1f;
        [Tooltip("Attack reach in world units in front of the runner")]
        [Range(1f, 3f)] public float attackReach = 1.8f;

        [Header("Ability (W)")]
        public AbilityType ability;
        public string abilityName = "KICK THRUSTERS";
        [TextArea] public string abilityDescription;
        [Min(0)] public float abilityCooldown;

        [Header("Unlock")]
        public UnlockType unlock;
        public int unlockCost;
        public string unlockLevelId = "level01";

        public string UnlockLabel => unlock switch
        {
            UnlockType.Free => "FREE",
            UnlockType.Bytes => $"{unlockCost} BYTES",
            _ => "CLEAR LEVEL 1",
        };

        static List<CharacterDefinition> _all;

        /// <summary>All runners in roster order (assets if present, otherwise the built-in four).</summary>
        public static IReadOnlyList<CharacterDefinition> All
        {
            get
            {
                if (_all != null) return _all;
                var assets = Resources.LoadAll<CharacterDefinition>(DH.CharacterDefsPath);
                _all = (assets != null && assets.Length > 0 ? assets.ToList() : CharacterRoster.CreateDefaults())
                    .OrderBy(c => c.order).ToList();
                return _all;
            }
        }

        public static CharacterDefinition Get(string id) =>
            All.FirstOrDefault(c => c.id == id) ?? All[0];
    }

    /// <summary>The four launch runners. Mirrors CHARACTERS in the browser build's sim.js.</summary>
    public static class CharacterRoster
    {
        public static List<CharacterDefinition> CreateDefaults()
        {
            return new List<CharacterDefinition>
            {
                Make("kai", 0, "KAI OKAFOR", "The Courier", "2ff3ff",
                    "Fastest courier in Sector 7. Never misses a drop.",
                    3, 1.0f, 1.8f, AbilityType.KickThrusters, "KICK THRUSTERS",
                    "Fire boot thrusters for a second jump in mid-air. Recharges every landing.", 0f,
                    UnlockType.Free, 0),
                Make("nova", 1, "NOVA REYES", "The Runner", "ff3fa4",
                    "Ex-corporate parkour champion. Quit the day they put a chip in her.",
                    3, 1.06f, 2.0f, AbilityType.PhaseDash, "PHASE DASH",
                    "Burst forward in a phased sprint. Pass through hazards and clear huge gaps.", 3f,
                    UnlockType.ClearLevel, 0),
                Make("jax", 2, "JAX MERCER", "The Breaker", "38b6ff",
                    "Former security engineer. Built half the drones. Now he breaks them.",
                    4, 0.95f, 2.3f, AbilityType.EmpPulse, "EMP PULSE",
                    "Overload nearby circuits. Fries every drone and shuts down lasers for 4 seconds.", 8f,
                    UnlockType.Bytes, 300),
                Make("lin", 3, "LIN SATO", "The Ghost", "7dff6a",
                    "The hacker who found the shutdown key. Nobody has seen her face.",
                    3, 1.0f, 1.9f, AbilityType.TimeGlitch, "TIME GLITCH",
                    "Hack the city clock. Time slows to half speed and nearby Data Bytes fly to you.", 10f,
                    UnlockType.Bytes, 500),
            };
        }

        static CharacterDefinition Make(string id, int order, string name, string title, string hex, string bio,
            int hp, float speed, float reach, AbilityType ab, string abName, string abText, float cd,
            UnlockType unlock, int cost)
        {
            var c = ScriptableObject.CreateInstance<CharacterDefinition>();
            c.name = id;
            c.id = id; c.order = order; c.displayName = name; c.title = title; c.color = DH.Hex(hex); c.bio = bio;
            c.maxHealth = hp; c.speedMultiplier = speed; c.jumpMultiplier = 1f; c.attackReach = reach;
            c.ability = ab; c.abilityName = abName; c.abilityDescription = abText; c.abilityCooldown = cd;
            c.unlock = unlock; c.unlockCost = cost;
            return c;
        }
    }
}
