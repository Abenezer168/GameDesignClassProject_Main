namespace DataHeist
{
    /// <summary>
    /// Decorator pattern, concrete component: the default jump every runner starts with. It jumps from the
    /// ground (honouring the jump buffer and coyote time); a press in mid-air does nothing on its own.
    /// </summary>
    public sealed class SingleJump : IJumpAbility
    {
        public bool TryJump(IJumpBody body, RunnerInputFrame input)
        {
            if (!body.CanGroundJump) return false;
            body.GroundJump();
            return true;
        }

        public bool HasDoubleJump => false;

        public string Describe() => "Single Jump";
    }
}
