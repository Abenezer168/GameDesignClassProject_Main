namespace DataHeist
{
    /// <summary>
    /// Decorator pattern, concrete decorator: adds one jump in mid-air on top of whatever it wraps.
    /// The wrapped ability always gets the first chance, so ground jumps (with buffer and coyote time)
    /// behave exactly as before; only a fresh press while airborne becomes the extra jump. The mid-air
    /// jump recharges on landing and costs nothing (no bytes or score are spent).
    /// </summary>
    public sealed class DoubleJumpDecorator : JumpAbilityDecorator
    {
        public DoubleJumpDecorator(IJumpAbility inner) : base(inner) { }

        public override bool TryJump(IJumpBody body, RunnerInputFrame input)
        {
            if (base.TryJump(body, input)) return true;
            if (!input.jumpPressed || !body.CanAirJump) return false;
            body.AirJump();
            return true;
        }

        public override bool HasDoubleJump => true;

        public override string Describe() => base.Describe() + " + Double Jump";
    }
}
