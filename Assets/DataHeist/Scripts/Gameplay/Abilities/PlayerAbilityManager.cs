using System;

namespace DataHeist
{
    /// <summary>
    /// Tracks which jump abilities the player has unlocked in the current level run and applies them.
    ///
    /// Every level run starts with double jump locked. It unlocks the moment the player holds
    /// <see cref="DoubleJumpUnlockPoints"/> points in that run (one point per coin / Data Byte, the number on
    /// the HUD). It then stays on for the rest of the run, including deaths and checkpoint retries, because
    /// those reuse the same run. Restarting the level or starting the next level creates a new run (and a
    /// new manager), so double jump is locked again. Nothing is saved.
    ///
    /// Unlocking wraps the run's jump in a <see cref="DoubleJumpDecorator"/> (Decorator pattern); the movement
    /// code in <see cref="RunnerSim"/> is not changed. Unlocking never spends coins or score.
    /// </summary>
    public sealed class PlayerAbilityManager
    {
        public const int DoubleJumpUnlockPoints = 30;

        /// <summary>Raised once per run, the moment double jump activates.</summary>
        public event Action DoubleJumpUnlocked;

        readonly RunnerSim _sim;

        public PlayerAbilityManager(RunnerSim sim)
        {
            _sim = sim ?? throw new ArgumentNullException(nameof(sim));
        }

        /// <summary>True once the 30-coin goal has been reached in this run.</summary>
        public bool IsDoubleJumpUnlocked { get; private set; }

        /// <summary>Progress toward the unlock: coins (Data Bytes) collected in this run.</summary>
        public int Points => _sim.stats.bytes;

        /// <summary>Call after each simulation step. Unlocks double jump once the goal is reached.</summary>
        public void Tick()
        {
            if (IsDoubleJumpUnlocked || Points < DoubleJumpUnlockPoints) return;
            IsDoubleJumpUnlocked = true;
            if (_sim.Jump.HasDoubleJump) return;            // Kai: Kick Thrusters is already a double jump
            _sim.Jump = new DoubleJumpDecorator(_sim.Jump);
            DoubleJumpUnlocked?.Invoke();
        }
    }
}
