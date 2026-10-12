using System;

namespace DataHeist
{
    /// <summary>
    /// Tracks which jump abilities the player has unlocked and applies them to a run.
    ///
    /// Double jump unlocks the first time the player holds <see cref="DoubleJumpUnlockPoints"/> points in a run
    /// (one point per Data Byte collected, the number on the HUD). The unlock is saved in
    /// <see cref="SaveData.doubleJumpUnlocked"/>, so it survives deaths, checkpoint retries, restarts, level
    /// changes and quitting the game. Only a new game (<see cref="ResetForNewGame"/>, which
    /// <see cref="SaveSystem.ResetAll"/> also does) locks it again.
    ///
    /// Applying it wraps the run's jump in a <see cref="DoubleJumpDecorator"/> (Decorator pattern); the
    /// movement code in <see cref="RunnerSim"/> is not changed. Unlocking never spends bytes or score.
    /// </summary>
    public sealed class PlayerAbilityManager
    {
        public const int DoubleJumpUnlockPoints = 30;

        /// <summary>Raised once, the moment double jump unlocks during this run.</summary>
        public event Action DoubleJumpUnlocked;

        readonly RunnerSim _sim;

        public PlayerAbilityManager(RunnerSim sim)
        {
            _sim = sim ?? throw new ArgumentNullException(nameof(sim));
            if (IsDoubleJumpUnlocked) ApplyDoubleJump();      // unlocked earlier: carry it into this run
        }

        /// <summary>Saved for the whole game (all levels and runners) until a new game.</summary>
        public static bool IsDoubleJumpUnlocked => SaveSystem.Data.doubleJumpUnlocked;

        /// <summary>Progress toward the unlock: Data Bytes collected in this run.</summary>
        public int Points => _sim.stats.bytes;

        /// <summary>Call after each simulation step. Unlocks double jump once the goal is reached.</summary>
        public void Tick()
        {
            if (IsDoubleJumpUnlocked || Points < DoubleJumpUnlockPoints) return;
            SaveSystem.Data.doubleJumpUnlocked = true;
            SaveSystem.Save();
            ApplyDoubleJump();
            DoubleJumpUnlocked?.Invoke();
        }

        /// <summary>Wraps the run's jump with double jump, unless it can already jump in mid-air.</summary>
        void ApplyDoubleJump()
        {
            if (!_sim.Jump.HasDoubleJump) _sim.Jump = new DoubleJumpDecorator(_sim.Jump);
        }

        /// <summary>A new game: double jump is locked again until the goal is reached.</summary>
        public static void ResetForNewGame()
        {
            SaveSystem.Data.doubleJumpUnlocked = false;
            SaveSystem.Save();
        }
    }
}
