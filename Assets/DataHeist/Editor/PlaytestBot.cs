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
        const int Ticks = DemoActions.DecisionTicks;
        const int Beam = 24;

        // the action vocabulary lives in DemoActions so recorded routes replay identically at runtime
        public static RunnerInputFrame Frame(int a, int k) => DemoActions.Frame(a, k);

        // persistent linked list of decisions, so beam nodes share their history
        class Path { public int act; public Path prev; public float effort; }

        // "natural" runs pay for every input, so among equally good routes the calm one wins:
        // idle, idle, idle ... jump exactly where needed (idle, jump, holdJ, tapJ, slide, holdS, attack)
        static readonly float[] Effort = { 0f, 0.3f, 0.05f, 0.3f, 0.3f, 0.05f, 0.3f };

        public struct Result
        {
            public string character;
            public bool cleared;
            public float time, failX;
            public int bytes, totalBytes, drones;
            public string rank;
            /// <summary>Winning route as one digit (action index) per decision.</summary>
            public string route;
        }

        public static Result Run(LevelData level, CharacterDefinition ch, bool natural = false)
        {
            _natural = natural;
            var beam = new List<(RunnerSim sim, Path path)> { (new RunnerSim(level, ch), null) };
            while (true)
            {
                var next = new List<(RunnerSim sim, Path path)>();
                foreach (var node in beam)
                {
                    for (int ai = 0; ai < DemoActions.Count; ai++)
                    {
                        var s = node.sim.Clone();
                        bool ok = true;
                        for (int k = 0; k < Ticks; k++)
                        {
                            s.Step(Dt, Frame(ai, k));
                            s.events.Clear();
                            if (s.stats.hits > 0 || s.state == SimState.Dead) { ok = false; break; }
                            if (s.state == SimState.Won) break;
                        }
                        if (ok) next.Add((s, new Path { act = ai, prev = node.path, effort = (node.path?.effort ?? 0) + (natural ? Effort[ai] : 0) }));
                    }
                }
                if (next.Count == 0)
                    return new Result { character = ch.id, cleared = false, failX = beam[0].sim.p.x, time = beam[0].sim.time };
                var won = next.Find(n => n.sim.state == SimState.Won);
                if (won.sim != null)
                {
                    var w = won.sim;
                    return new Result
                    {
                        character = ch.id, cleared = true, time = w.time, bytes = w.stats.bytes,
                        totalBytes = w.totalBytes, drones = w.stats.drones, rank = w.Rank(),
                        route = Route(won.path),
                    };
                }
                next.Sort((a, b) => Score(b).CompareTo(Score(a)));
                var seen = new HashSet<string>();
                beam = new List<(RunnerSim, Path)>();
                foreach (var n in next)
                {
                    var p = n.sim.p;
                    string key = $"{p.x:F1}|{p.y:F1}|{p.vy:F0}|{p.sliding}|{p.attackCD > 0}|{n.sim.stats.bytes}";
                    if (!seen.Add(key)) continue;
                    beam.Add(n);
                    if (beam.Count >= Beam) break;
                }
            }
        }

        static bool _natural;

        // natural runs also take drones down, so the demo shows every verb of the game
        static float Score((RunnerSim sim, Path path) n) =>
            n.sim.stats.bytes * 3 + n.sim.p.x - (n.path?.effort ?? 0) + (_natural ? n.sim.stats.drones * 4 : 0);

        /// <summary>Replays a route and counts what it survived, proving the hazards really engage.</summary>
        static string HazardReport(LevelData level, CharacterDefinition ch, string route)
        {
            var counts = new Dictionary<SimEventType, int>();
            var sim = new RunnerSim(level, ch);
            for (int d = 0; d < route.Length && sim.state == SimState.Running; d++)
                for (int k = 0; k < Ticks && sim.state == SimState.Running; k++)
                {
                    sim.Step(Dt, Frame(route[d] - '0', k));
                    foreach (var ev in sim.events) counts[ev.type] = counts.TryGetValue(ev.type, out var c) ? c + 1 : 1;
                    sim.events.Clear();
                }
            int Get(SimEventType t) => counts.TryGetValue(t, out var c) ? c : 0;
            if (Get(SimEventType.SniperShot) + Get(SimEventType.SignFall) + Get(SimEventType.ShockOn) == 0) return "";
            return $" | survived {Get(SimEventType.SniperShot)} sniper shots ({Get(SimEventType.Deflect)} slashed), " +
                   $"{Get(SimEventType.SignFall)} falling signs ({Get(SimEventType.SignSmash)} smashed), {Get(SimEventType.ShockOn)} live-floor pulses";
        }

        static string Route(Path p)
        {
            var sb = new System.Text.StringBuilder();
            for (; p != null; p = p.prev) sb.Insert(0, (char)('0' + p.act));
            return sb.ToString();
        }

        /// <summary>
        /// Records the bot's clean Level 1 run for every runner into Resources/DataHeist/Demo; the
        /// title screen replays them through the real simulation as its attract-mode background.
        /// </summary>
        [MenuItem("Data Heist/Record Title Screen Demo", priority = 22)]
        public static void RecordDemo()
        {
            var level = LevelData.Load("level01");
            if (level == null) return;
            const string dir = "Assets/DataHeist/Resources/DataHeist/Demo";
            System.IO.Directory.CreateDirectory(dir);
            foreach (var ch in CharacterDefinition.All)
            {
                var r = Run(level, ch, natural: true);
                if (!r.cleared) { Debug.LogError($"[PlaytestBot] {ch.id}: no clean run to record."); continue; }
                // replay exactly the way DemoPlayer will, and refuse a route that doesn't finish clean
                var sim = new RunnerSim(level, ch);
                for (int d = 0; d < r.route.Length && sim.state == SimState.Running; d++)
                    for (int k = 0; k < Ticks && sim.state == SimState.Running; k++) { sim.Step(Dt, Frame(r.route[d] - '0', k)); sim.events.Clear(); }
                if (sim.state != SimState.Won || sim.stats.hits > 0)
                {
                    Debug.LogError($"[PlaytestBot] {ch.id}: recorded route does not replay cleanly ({sim.state}, {sim.stats.hits} hits).");
                    continue;
                }
                System.IO.File.WriteAllText($"{dir}/level01_{ch.id}.txt", r.route);
                Debug.Log($"[PlaytestBot] Recorded {ch.id}: {r.route.Length} decisions, {r.time:F1}s, rank {r.rank}, {r.drones} drones; replay verified");
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Data Heist/Validate Level 1 (Playtest Bot)", priority = 20)]
        public static void ValidateLevel1() => Validate("level01");

        [MenuItem("Data Heist/Validate Level 2 (Playtest Bot)", priority = 21)]
        public static void ValidateLevel2() => Validate("level02");

        /// <summary>Headless entry point: validates every level and exits 0 (all clear) or 1.</summary>
        public static void ValidateAllBatch()
        {
            bool ok = true;
            foreach (var l in LevelCatalog.All) ok &= Validate(l.id);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Every runner must find a zero-hit, no-ability route to the exit.</summary>
        public static bool Validate(string levelId)
        {
            var level = LevelData.Load(levelId);
            if (level == null) return false;
            var sw = Stopwatch.StartNew();
            bool all = true;
            var roster = CharacterDefinition.All;
            for (int i = 0; i < roster.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Data Heist Playtest Bot", $"{level.name}: {roster[i].displayName}...", i / (float)roster.Count);
                var r = Run(level, roster[i]);
                if (r.cleared)
                    Debug.Log($"[PlaytestBot] {levelId} {r.character}: CLEARED with 0 hits in {r.time:F1}s, bytes {r.bytes}/{r.totalBytes}, drones {r.drones}, rank {r.rank}{HazardReport(level, roster[i], r.route)}");
                else
                {
                    all = false;
                    Debug.LogError($"[PlaytestBot] {levelId} {r.character}: NO zero-hit route past x = {r.failX:F1} (t = {r.time:F1}s). Re-space the obstacles there.");
                }
            }
            EditorUtility.ClearProgressBar();
            Debug.Log($"[PlaytestBot] {levelId}: {(all ? "all runners can clear it without taking a hit." : "level needs fixes.")} ({sw.Elapsed.TotalSeconds:F1}s)");
            return all;
        }
    }
}
