using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DataHeist.EditorTools
{
    /// <summary>
    /// Checks the double-jump unlock (Decorator pattern + PlayerAbilityManager) against the real simulation.
    /// Menu: Data Heist > Debug > Test Double Jump Unlock. Headless: -executeMethod
    /// DataHeist.EditorTools.DoubleJumpTests.RunBatch (exit code 1 on failure). The player's save is
    /// snapshotted first and put back afterwards.
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

            // start of a brand-new game
            SaveSystem.ResetAll();
            Check(!PlayerAbilityManager.IsDoubleJumpUnlocked, "a new game starts with double jump locked");

            // 1. fewer than 30 points: single jump only
            var sim = new RunnerSim(level1, nova);
            var abilities = new PlayerAbilityManager(sim);
            int unlocks = 0;
            abilities.DoubleJumpUnlocked += () => unlocks++;
            Check(sim.Jump is SingleJump, $"starts as a plain single jump ({sim.Jump.Describe()})");
            sim.stats.bytes = PlayerAbilityManager.DoubleJumpUnlockPoints - 1;
            abilities.Tick();
            Check(!PlayerAbilityManager.IsDoubleJumpUnlocked && unlocks == 0, "29 points: still locked");
            Check(!PressJumpInMidAir(sim), "29 points: a mid-air jump press does nothing");

            // 2. exactly 30 points: unlocks, the chain is decorated, and it works
            sim = new RunnerSim(level1, nova);
            abilities = new PlayerAbilityManager(sim);
            unlocks = 0;
            abilities.DoubleJumpUnlocked += () => unlocks++;
            sim.stats.bytes = PlayerAbilityManager.DoubleJumpUnlockPoints;
            abilities.Tick();
            Check(PlayerAbilityManager.IsDoubleJumpUnlocked && unlocks == 1, "30 points: double jump unlocks (once)");
            Check(sim.Jump is DoubleJumpDecorator && sim.Jump.HasDoubleJump, $"30 points: jump is decorated ({sim.Jump.Describe()})");
            Check(sim.stats.bytes == 30, "unlocking leaves the points unchanged");
            Check(PressJumpInMidAir(sim), "30 points: a mid-air jump press double-jumps");
            Check(_lastJumpPointChange == _lastJumpPickups, $"using double jump costs no points (change {_lastJumpPointChange}, pickups {_lastJumpPickups})");

            // 3. more than 30 points: still available, never unlocked or wrapped twice
            sim.stats.bytes = 45;
            abilities.Tick();
            Check(unlocks == 1, "45 points: no second unlock");
            Check(sim.Jump.Describe() == "Single Jump + Double Jump", $"45 points: wrapped exactly once ({sim.Jump.Describe()})");
            Land(sim);
            Check(PressJumpInMidAir(sim), "45 points: double jump recharges on landing and works again");

            // 4. death / checkpoint retry, level restart and the next level keep it
            sim.Respawn();
            Check(PressJumpInMidAir(sim), "after dying and retrying from a checkpoint: still unlocked");
            SaveSystem.Reload();                                  // as if the game was closed and reopened
            var restart = new RunnerSim(level1, nova);
            new PlayerAbilityManager(restart);
            Check(PressJumpInMidAir(restart), "after restarting the level (and reloading the save): still unlocked");
            var next = new RunnerSim(level2, nova);
            new PlayerAbilityManager(next);
            Check(next.stats.bytes == 0 && PressJumpInMidAir(next), "Level 2 with 0 points: still unlocked");

            // 5. Kai keeps his built-in double jump and is not wrapped twice
            var kaiSim = new RunnerSim(level1, kai);
            new PlayerAbilityManager(kaiSim);
            Check(kaiSim.Jump.Describe() == "Single Jump + Double Jump", $"Kai's chain is not doubled ({kaiSim.Jump.Describe()})");

            // 6. a completely new game locks it again
            PlayerAbilityManager.ResetForNewGame();
            var fresh = new RunnerSim(level1, nova);
            new PlayerAbilityManager(fresh);
            Check(!PlayerAbilityManager.IsDoubleJumpUnlocked && !PressJumpInMidAir(fresh), "new game: double jump is locked again");
            SaveSystem.Data.doubleJumpUnlocked = true; SaveSystem.Save();
            SaveSystem.ResetAll();
            Check(!PlayerAbilityManager.IsDoubleJumpUnlocked, "Reset Save Data also locks it");

            // Kai without the unlock: his Kick Thrusters still double-jump (unchanged behaviour)
            var kaiLocked = new RunnerSim(level1, kai);
            new PlayerAbilityManager(kaiLocked);
            Check(PressJumpInMidAir(kaiLocked), "Kai still double-jumps with his built-in Kick Thrusters");
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
            // points may only change by bytes picked up in this same step, never go down for the jump itself
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
