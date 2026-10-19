using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DataHeist.EditorTools
{
    /// <summary>
    /// Checks the double-jump unlock (Decorator pattern + PlayerAbilityManager) against the real simulation.
    /// The unlock is per level run: every run starts locked and needs 30 coins. Menu: Data Heist > Debug >
    /// Test Double Jump Unlock. Headless: -executeMethod DataHeist.EditorTools.DoubleJumpTests.RunBatch
    /// (exit code 1 on failure). The player's save is snapshotted first and put back afterwards.
    /// </summary>
    public static class DoubleJumpTests
    {
        const float Dt = DH.SimStep;
        static readonly List<string> Failures = new List<string>();

        [MenuItem("Data Heist/Debug/Test Double Jump Unlock", priority = 43)]
        public static void RunFromMenu() => Run();

        public static void RunBatch() => EditorApplication.Exit(Run() ? 0 : 1);

        static bool Run()
        {
            Failures.Clear();
            string saveBefore = PlayerPrefs.GetString(SaveSystem.PrefsKey, "");
            try
            {
                Scenarios();
            }
            finally
            {
                if (string.IsNullOrEmpty(saveBefore)) PlayerPrefs.DeleteKey(SaveSystem.PrefsKey);
                else PlayerPrefs.SetString(SaveSystem.PrefsKey, saveBefore);
                PlayerPrefs.Save();
                SaveSystem.Reload();
            }
            if (Failures.Count == 0) Debug.Log("[DoubleJumpTests] PASSED: all double-jump scenarios");
            else Debug.LogError($"[DoubleJumpTests] FAILED with {Failures.Count} problem(s):\n" + string.Join("\n", Failures));
            return Failures.Count == 0;
        }

        static void Scenarios()
        {
            var level1 = LevelData.Load("level01");
            var level2 = LevelData.Load("level02");
            var nova = CharacterDefinition.Get("nova");      // no built-in air jump
            var kai = CharacterDefinition.Get("kai");        // Kick Thrusters: a built-in double jump

            // 1. a new run starts locked, whatever happened in earlier runs
            var sim = new RunnerSim(level1, nova);
            var abilities = new PlayerAbilityManager(sim);
            int unlocks = 0;
            abilities.DoubleJumpUnlocked += () => unlocks++;
            Check(!abilities.IsDoubleJumpUnlocked && sim.Jump is SingleJump, $"a new run starts locked ({sim.Jump.Describe()})");

            // 2. fewer than 30 coins: single jump only
            sim.stats.bytes = PlayerAbilityManager.DoubleJumpUnlockPoints - 1;
            abilities.Tick();
            Check(!abilities.IsDoubleJumpUnlocked && unlocks == 0, "29 coins: still locked");
            Check(!PressJumpInMidAir(sim), "29 coins: a mid-air jump press does nothing");

            // 3. exactly 30 coins: activates once, the chain is decorated, and it works
            sim.stats.bytes = PlayerAbilityManager.DoubleJumpUnlockPoints;
            abilities.Tick();
            Check(abilities.IsDoubleJumpUnlocked && unlocks == 1, "30 coins: double jump activates (once)");
            Check(sim.Jump is DoubleJumpDecorator && sim.Jump.HasDoubleJump, $"30 coins: jump is decorated ({sim.Jump.Describe()})");
            Check(sim.stats.bytes == 30, "activating leaves the coins unchanged");
            Check(PressJumpInMidAir(sim), "30 coins: a mid-air jump press double-jumps");
            Check(_lastJumpPointChange == _lastJumpPickups, $"using double jump costs no coins (change {_lastJumpPointChange}, pickups {_lastJumpPickups})");

            // 4. more than 30 coins: still on, never activated or wrapped twice
            sim.stats.bytes = 45;
            abilities.Tick();
            Check(unlocks == 1, "45 coins: no second activation");
            Check(sim.Jump.Describe() == "Single Jump + Double Jump", $"45 coins: wrapped exactly once ({sim.Jump.Describe()})");
            Land(sim);
            Check(PressJumpInMidAir(sim), "45 coins: double jump recharges on landing and works again");

            // 5. dying and retrying from a checkpoint is the same run: it stays on
            sim.Respawn();
            abilities.Tick();
            Check(abilities.IsDoubleJumpUnlocked && PressJumpInMidAir(sim), "after dying and retrying from a checkpoint: still on");

            // 6. restarting the level is a new run: locked again
            var restart = new RunnerSim(level1, nova);
            var restartAbilities = new PlayerAbilityManager(restart);
            Check(!restartAbilities.IsDoubleJumpUnlocked && !PressJumpInMidAir(restart), "restarting the level: locked again");

            // 7. the next level is a new run: locked until 30 coins there
            var next = new RunnerSim(level2, nova);
            var nextAbilities = new PlayerAbilityManager(next);
            Check(!nextAbilities.IsDoubleJumpUnlocked && !PressJumpInMidAir(next), "Level 2: starts locked");
            next.stats.bytes = 30;
            nextAbilities.Tick();
            Check(nextAbilities.IsDoubleJumpUnlocked && PressJumpInMidAir(next), "Level 2: 30 coins activate it there too");

            // 8. nothing is saved: the save data has no unlock to carry over
            SaveSystem.Reload();
            var reopened = new RunnerSim(level1, nova);
            Check(!new PlayerAbilityManager(reopened).IsDoubleJumpUnlocked && !PressJumpInMidAir(reopened), "reopening the game: locked");

            // 9. Kai keeps his built-in double jump; reaching 30 coins adds no second wrap or notification
            var kaiSim = new RunnerSim(level1, kai);
            var kaiAbilities = new PlayerAbilityManager(kaiSim);
            int kaiUnlocks = 0;
            kaiAbilities.DoubleJumpUnlocked += () => kaiUnlocks++;
            Check(PressJumpInMidAir(kaiSim), "Kai double-jumps with his built-in Kick Thrusters before 30 coins");
            kaiSim.stats.bytes = 30;
            kaiAbilities.Tick();
            Check(kaiUnlocks == 0 && kaiSim.Jump.Describe() == "Single Jump + Double Jump", $"Kai at 30 coins: no extra wrap or notification ({kaiSim.Jump.Describe()})");
        }

        /// <summary>Takes off, rises for 0.2 s, then presses jump again in mid-air. True if that press air-jumped.</summary>
        static bool PressJumpInMidAir(RunnerSim sim)
        {
            Land(sim);
            sim.Step(Dt, new RunnerInputFrame { jumpPressed = true, jumpHeld = true });
            for (int i = 0; i < 24; i++) sim.Step(Dt, new RunnerInputFrame { jumpHeld = true });
            if (sim.p.onGround || sim.state != SimState.Running) { Failures.Add("could not get airborne for the test"); return false; }
            sim.events.Clear();
            int bytesBefore = sim.stats.bytes;
            sim.Step(Dt, new RunnerInputFrame { jumpPressed = true, jumpHeld = true });
            // coins may only change by pickups in this same step, never go down for the jump itself
            _lastJumpPointChange = sim.stats.bytes - bytesBefore;
            _lastJumpPickups = sim.events.FindAll(e => e.type == SimEventType.Byte).Count;
            return sim.events.Exists(e => e.type == SimEventType.AirJump);
        }

        static int _lastJumpPointChange, _lastJumpPickups;

        /// <summary>Steps with no input until the runner is back on the ground.</summary>
        static void Land(RunnerSim sim)
        {
            for (int i = 0; i < 600 && !sim.p.onGround && sim.state == SimState.Running; i++) sim.Step(Dt, default);
            sim.events.Clear();
        }

        static void Check(bool ok, string what)
        {
            if (ok) Debug.Log("[DoubleJumpTests] OK   " + what);
            else { Failures.Add(what); Debug.LogError("[DoubleJumpTests] FAIL " + what); }
        }
    }
}
