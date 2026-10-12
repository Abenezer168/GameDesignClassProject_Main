namespace DataHeist
{
    /// <summary>
    /// The body a jump ability acts on. <see cref="RunnerSim"/> implements it, so jump abilities never touch
    /// the simulation's internals directly: they only ask "may I jump?" and say "jump now".
    /// </summary>
    public interface IJumpBody
    {
        /// <summary>A buffered jump press while on the ground (or inside coyote time).</summary>
        bool CanGroundJump { get; }

        /// <summary>Airborne, past coyote time, and the mid-air jump has not been used since landing.</summary>
        bool CanAirJump { get; }

        void GroundJump();
        void AirJump();
    }

    /// <summary>
    /// Decorator pattern, component interface: what happens when the runner presses jump.
    /// <see cref="SingleJump"/> is the concrete component; <see cref="JumpAbilityDecorator"/> subclasses
    /// such as <see cref="DoubleJumpDecorator"/> wrap it to add behaviour without changing it.
    /// Implementations hold no per-run state (that lives on the runner), so a chain can be shared
    /// between cloned simulations.
    /// </summary>
    public interface IJumpAbility
    {
        /// <summary>Called once per simulation step. Returns true if a jump happened this step.</summary>
        bool TryJump(IJumpBody body, RunnerInputFrame input);

        /// <summary>True if this ability, or any ability it wraps, can jump in mid-air.</summary>
        bool HasDoubleJump { get; }

        /// <summary>Readable description of the chain, e.g. "Single Jump + Double Jump".</summary>
        string Describe();
    }
}
