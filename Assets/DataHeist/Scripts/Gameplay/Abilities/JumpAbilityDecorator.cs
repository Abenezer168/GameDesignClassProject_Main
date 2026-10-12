namespace DataHeist
{
    /// <summary>
    /// Decorator pattern, base decorator: wraps another <see cref="IJumpAbility"/> and forwards to it by
    /// default. Subclasses add behaviour around the wrapped ability instead of replacing it.
    /// </summary>
    public abstract class JumpAbilityDecorator : IJumpAbility
    {
        protected readonly IJumpAbility inner;

        protected JumpAbilityDecorator(IJumpAbility inner)
        {
            this.inner = inner ?? throw new System.ArgumentNullException(nameof(inner));
        }

        public virtual bool TryJump(IJumpBody body, RunnerInputFrame input) => inner.TryJump(body, input);

        public virtual bool HasDoubleJump => inner.HasDoubleJump;

        public virtual string Describe() => inner.Describe();
    }
}
