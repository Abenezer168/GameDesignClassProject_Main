using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace DataHeist.EditorTools
{
    /// <summary>
    /// Automated level validation. A beam search over player inputs (decision every 75 ms,
    /// human-like) must find a zero-hit route to the extraction point WITHOUT using abilities,
    /// for every runner. Run it after editing a level: Data Heist > Validate Level 1.
    /// </summary>
    public static class PlaytestBot
    {
        const float Dt = 1f / 120f;
        const int Ticks = 9;
        const int Beam = 24;

        struct Act { public string name; public bool pressJ, holdJ, pressS, holdS, attack; }

        static readonly Act[] Actions =
        {
            new Act { name = "idle" },
            new Act { name = "jump", pressJ = true, holdJ = true },
            new Act { name = "holdJ", holdJ = true },
            new Act { name = "tapJ", pressJ = true },
            new Act { name = "slide", pressS = true, holdS = true },
            new Act { name = "holdS", holdS = true },
            new Act { name = "attack", attack = true },
        };

        public struct Result
        {
            public string character;
            public bool cleared;
            public float time, failX;
            public int bytes, totalBytes, drones;
            public string rank;
        }

        public static Result Run(LevelData level, CharacterDefinition ch)
        {
            var beam = new List<RunnerSim> { new RunnerSim(level, ch) };
            while (true)
            {
                var next = new List<RunnerSim>();
                foreach (var node in beam)
                {
                    foreach (var a in Actions)
                    {
                        var s = node.Clone();
                        bool ok = true;
                        for (int k = 0; k < Ticks; k++)
                        {
                            s.Step(Dt, new RunnerInputFrame
                            {
                                jumpPressed = k == 0 && a.pressJ, jumpHeld = a.holdJ,
                                slidePressed = k == 0 && a.pressS, slideHeld = a.holdS,
                                attackPressed = k == 0 && a.attack,
                            });
                            s.events.Clear();
                            if (s.stats.hits > 0 || s.state == SimState.Dead) { ok = false; break; }
                            if (s.state == SimState.Won) break;
                        }
                        if (ok) next.Add(s);
                    }
                }
                if (next.Count == 0)
                    return new Result { character = ch.id, cleared = false, failX = beam[0].p.x, time = beam[0].time };
                var won = next.Find(n => n.state == SimState.Won);
                if (won != null)
                    return new Result
                    {
                        character = ch.id, cleared = true, time = won.time, bytes = won.stats.bytes,
                        totalBytes = won.totalBytes, drones = won.stats.drones, rank = won.Rank(),
                    };
                next.Sort((a, b) => (b.stats.bytes * 3 + b.p.x).CompareTo(a.stats.bytes * 3 + a.p.x));
                var seen = new HashSet<string>();
                beam = new List<RunnerSim>();
                foreach (var n in next)
                {
                    var p = n.p;
                    string key = $"{p.x:F1}|{p.y:F1}|{p.vy:F0}|{p.sliding}|{p.attackCD > 0}|{n.stats.bytes}";
                    if (!seen.Add(key)) continue;
                    beam.Add(n);
                    if (beam.Count >= Beam) break;
                }
            }
        }

        [MenuItem("Data Heist/Validate Level 1 (Playtest Bot)", priority = 20)]
        public static void ValidateLevel1()
        {
            var level = LevelData.Load("level01");
            if (level == null) return;
            var sw = Stopwatch.StartNew();
            bool all = true;
            var roster = CharacterDefinition.All;
            for (int i = 0; i < roster.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Data Heist Playtest Bot", $"Running {roster[i].displayName}...", i / (float)roster.Count);
                var r = Run(level, roster[i]);
                if (r.cleared)
                    Debug.Log($"[PlaytestBot] {r.character}: CLEARED with 0 hits in {r.time:F1}s, bytes {r.bytes}/{r.totalBytes}, drones {r.drones}, rank {r.rank}");
                else
                {
                    all = false;
                    Debug.LogError($"[PlaytestBot] {r.character}: NO zero-hit route past x = {r.failX:F1} (t = {r.time:F1}s). Re-space the obstacles there.");
                }
            }
            EditorUtility.ClearProgressBar();
            Debug.Log($"[PlaytestBot] {(all ? "All runners can clear Level 1 without taking a hit." : "Level needs fixes.")} ({sw.Elapsed.TotalSeconds:F1}s)");
        }
    }
}
